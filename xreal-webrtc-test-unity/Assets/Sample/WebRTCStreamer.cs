using NRKernal;
using NRKernal.Record;
using UnityEngine;
using Unity.WebRTC;
using System;
using System.Collections;
using WebSocketSharp;
using System.Threading;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class WebRTCStreamer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string serverUrl = "ws://192.168.x.x:3001/";
    [SerializeField] private float recordDurationSeconds = 30.0f; // 30秒毎に録画ファイルを切り替える

    private NRVideoCapture m_VideoCapture = null;
    private WebSocket ws;
    private RTCPeerConnection localPeer;
    private VideoStreamTrack videoTrack;
    private VideoStreamHandler videoHandler;
    private string currentVideoPath;
    private Coroutine recordingCoroutine;

    private bool isProcessingVideo = false;
    private bool isTransitioning = false;

    void Start()
    {
        Debug.Log("[XREAL_WEBRTC][Init] Creating VideoStreamHandler...");
        try
        {
            videoHandler = new VideoStreamHandler(1280, 720);
            Debug.Log("[XREAL_WEBRTC][Init] VideoStreamHandler created successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"[XREAL_WEBRTC][Init] Failed to create VideoStreamHandler: {e}");
            return;
        }

        CreateVideoCapture(() =>
        {
            StartVideoCapture();
            InitializeWebRTC();
            StartCoroutine(RecordVideo());
        });
    }

    void CreateVideoCapture(Action callback)
    {
        Debug.Log("[XREAL_WEBRTC] Creating VideoCapture...");
        NRVideoCapture.CreateAsync(false, delegate (NRVideoCapture videoCapture)
        {
            Debug.Log("[XREAL_WEBRTC] VideoCapture created: " + (videoCapture != null ? "success" : "failed"));
            if (videoCapture != null)
            {
                m_VideoCapture = videoCapture;
                callback?.Invoke();
            }
            else
            {
                Debug.LogError("Failed to create VideoCapture");
            }
        });
    }

    void StartVideoCapture()
    {
        CameraParameters cameraParams = new CameraParameters()
        {
            hologramOpacity = 1.0f,
            frameRate = 30,
            cameraResolutionWidth = 1280,
            cameraResolutionHeight = 720,
            pixelFormat = CapturePixelFormat.BGRA32,
            blendMode = BlendMode.VirtualOnly,
            audioState = NRVideoCapture.AudioState.None
        };

        m_VideoCapture.StartVideoModeAsync(cameraParams, (result) =>
        {
            if (result.success)
            {
                Debug.Log("[XREAL_WEBRTC][Camera] Started video mode successfully");
                var previewTexture = m_VideoCapture.PreviewTexture;
                if (previewTexture != null)
                {
                    Debug.Log($"[XREAL_WEBRTC][Camera] PreviewTexture created: {previewTexture.width}x{previewTexture.height}");
                    CreateVideoTrack(previewTexture); // PreviewTextureをVideoStreamTrackとして使用
                }
            }
            else
            {
                Debug.LogError("[XREAL_WEBRTC][Camera] Failed to start video mode");
            }
        });
    }

    private void CreateVideoTrack(Texture texture)
    {
        try
        {
            Debug.Log("[XREAL_WEBRTC][Track] Creating new video track from texture");
            Debug.Log($"[XREAL_WEBRTC][Track] Texture info - Width: {texture.width}, Height: {texture.height}, Format: {texture.graphicsFormat}");

            if (videoTrack != null)
            {
                Debug.Log("[XREAL_WEBRTC][Track] Disposing old track");
                videoTrack.Dispose();
            }

            videoTrack = new VideoStreamTrack(texture);
            videoTrack.Enabled = true;

            Debug.Log($"[XREAL_WEBRTC][Track] New track created - Enabled: {videoTrack.Enabled}");

            if (localPeer != null)
            {
                var stream = new MediaStream();
                stream.AddTrack(videoTrack);
                localPeer.AddTrack(videoTrack, stream);
                Debug.Log("[XREAL_WEBRTC][Track] Track added to peer connection");
                StartCoroutine(CreateAndSendOffer());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[XREAL_WEBRTC][Track] Error creating video track: {e}");
        }
    }

    private void InitializeWebRTC()
    {
        Debug.Log("[XREAL_WEBRTC][Init] Starting WebRTC initialization");
        StartCoroutine(WebRTC.Update());
        ws = new WebSocket(serverUrl);
        var context = SynchronizationContext.Current;

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("[XREAL_WEBRTC][WS] WebSocket Connected");
            SetupPeerConnection();
        };
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log($"[XREAL_WEBRTC][WS] Received message: {e.Data}");
            context.Post(_ => HandleWebSocketMessage(e.Data), null);
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"[XREAL_WEBRTC][WS] WebSocket Error: {e.Message}");
        };

        ws.Connect();
    }

    private void SetupPeerConnection()
    {
        Debug.Log("[XREAL_WEBRTC][Peer] Setting up peer connection");
        var config = new RTCConfiguration
        {
            iceServers = new[] {
                new RTCIceServer {
                    urls = new[] {
                        "stun:stun.l.google.com:19302",
                    }
                }
            }
        };

        localPeer = new RTCPeerConnection(ref config);

        localPeer.OnIceCandidate = candidate =>
        {
            if (candidate == null) return;
            Debug.Log($"[XREAL_WEBRTC][ICE] Generated candidate: {candidate.Candidate}");

            var json = JsonUtility.ToJson(new SignalingMessage
            {
                type = "candidate",
                candidate = new CandidateData
                {
                    candidate = candidate.Candidate,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex ?? 0
                }
            });

            if (ws?.ReadyState == WebSocketState.Open)
            {
                Debug.Log($"[XREAL_WEBRTC][WS] Sending candidate: {json}");
                ws.Send(json);
            }
        };

        localPeer.OnConnectionStateChange = state =>
        {
            Debug.Log($"[XREAL_WEBRTC][Peer] Connection State: {state}");
        };

        localPeer.OnIceConnectionChange = state =>
        {
            Debug.Log($"[XREAL_WEBRTC][ICE] Connection State: {state}");

            if (state == RTCIceConnectionState.Failed)
            {
                Debug.LogError("[XREAL_WEBRTC][ICE] Connection Failed - Attempting reconnect");
                StartCoroutine(CreateAndSendOffer());
            }
        };

        // ビデオトラックの設定
        var transceiver = localPeer.AddTransceiver(TrackKind.Video);
        transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;

        // 既存のビデオトラックがある場合は追加
        if (videoTrack != null)
        {
            var stream = new MediaStream();
            stream.AddTrack(videoTrack);
            localPeer.AddTrack(videoTrack, stream);
            StartCoroutine(CreateAndSendOffer());
        }
    }

    private IEnumerator CreateAndSendOffer()
    {
        var op = localPeer.CreateOffer();
        yield return new WaitUntil(() => op.IsDone);

        if (!op.IsError)
        {
            var offer = op.Desc;
            var setLocalDesc = localPeer.SetLocalDescription(ref offer);
            yield return new WaitUntil(() => setLocalDesc.IsDone);

            if (!setLocalDesc.IsError)
            {
                var msg = new SignalingMessage
                {
                    type = "offer",
                    offer = new DescData
                    {
                        type = "offer",
                        sdp = offer.sdp
                    }
                };
                SendWebSocketMessage(msg);
            }
        }
    }

    private void HandleWebSocketMessage(string message)
    {
        Debug.Log($"[XREAL_WEBRTC][WS] Received: {message}");
        try
        {
            var msg = JsonUtility.FromJson<SignalingMessage>(message);
            if (msg.type == "answer" && msg.answer != null)
            {
                Debug.Log("[XREAL_WEBRTC][Signaling] Received answer");
                var answer = new RTCSessionDescription
                {
                    type = RTCSdpType.Answer,
                    sdp = msg.answer.sdp
                };
                StartCoroutine(HandleAnswer(answer));
            }
            else if (msg.type == "candidate" && msg.candidate != null)
            {
                Debug.Log($"[XREAL_WEBRTC][ICE] Received candidate: {msg.candidate.candidate}");
                var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                {
                    candidate = msg.candidate.candidate,
                    sdpMid = msg.candidate.sdpMid,
                    sdpMLineIndex = msg.candidate.sdpMLineIndex
                });

                localPeer.AddIceCandidate(candidate);
                Debug.Log("[XREAL_WEBRTC][ICE] Added ICE candidate");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[XREAL_WEBRTC][WS] Parse error: {e.Message}");
        }
    }

    private IEnumerator HandleAnswer(RTCSessionDescription answer)
    {
        var op = localPeer.SetRemoteDescription(ref answer);
        yield return new WaitUntil(() => op.IsDone);

        if (op.IsError)
        {
            Debug.LogError($"Error setting remote description: {op.Error.message}");
        }
    }

    private void SendWebSocketMessage(object data)
    {
        if (ws?.ReadyState != WebSocketState.Open)
        {
            Debug.LogError($"[XREAL_WEBRTC][WS] WebSocket not ready: {ws?.ReadyState}");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data);
            Debug.Log($"[XREAL_WEBRTC][WS] Sending: {json}");
            ws.Send(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[XREAL_WEBRTC][WS] Failed to send message: {e.Message}");
        }
    }

    [System.Serializable]
    private class SignalingMessage
    {
        public string type;
        public DescData offer;
        public DescData answer;
        public CandidateData candidate;
    }

    [System.Serializable]
    private class DescData
    {
        public string type;
        public string sdp;
    }

    [System.Serializable]
    private class CandidateData
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
    }

    private string GenerateVideoPath()
    {
        var filename = $"temp_video_{DateTime.Now.Ticks}.mp4";
        return Path.Combine(Application.temporaryCachePath, filename);
    }

    private IEnumerator RecordVideo()
    {
        while (true) // 永続的なループ
        {
            string newVideoPath = GenerateVideoPath();
            Debug.Log($"[XREAL_WEBRTC][Record] Starting new recording to: {newVideoPath}");
            bool started = false;

            try
            {
                m_VideoCapture.StartRecordingAsync(newVideoPath, (result) =>
                {
                    if (result.success)
                    {
                        started = true;
                        Debug.Log($"[XREAL_WEBRTC][Record] Started recording");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[XREAL_WEBRTC][Record] Error starting recording: {e.Message}");
                yield break;
            }

            yield return new WaitUntil(() => started);

            // 指定時間の録画
            yield return new WaitForSeconds(recordDurationSeconds);

            // 次の録画への切り替え準備
            string previousPath = currentVideoPath;
            currentVideoPath = newVideoPath;

            // 現在の録画を停止
            bool stopped = false;
            try
            {
                m_VideoCapture.StopRecordingAsync((result) =>
                {
                    if (result.success)
                    {
                        stopped = true;
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[XREAL_WEBRTC][Record] Error stopping recording: {e.Message}");
            }

            yield return new WaitUntil(() => stopped);

            // 前のファイルを削除
            if (!string.IsNullOrEmpty(previousPath))
            {
                DeleteVideoFile(previousPath);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void DeleteVideoFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[XREAL_WEBRTC][Record] Deleted processed file: {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[XREAL_WEBRTC][Record] Failed to delete file: {e.Message}");
        }
    }

    private void HandleTextureUpdated(RenderTexture texture)
    {
        if (texture == null || localPeer == null || isTransitioning) return;

        try
        {
            if (isProcessingVideo)
            {
                Debug.Log("[XREAL_WEBRTC][Track] Skipping texture update during video processing");
                return;
            }

            var senders = localPeer.GetSenders();
            var sender = senders.FirstOrDefault(s => s.Track?.Kind == TrackKind.Video);

            if (sender != null && videoTrack != null)
            {
                var newTrack = new VideoStreamTrack(texture);
                newTrack.Enabled = true;

                sender.ReplaceTrack(newTrack);
                videoTrack.Dispose();
                videoTrack = newTrack;
                Debug.Log("[XREAL_WEBRTC][Track] Track replaced successfully");
            }
            else
            {
                videoTrack?.Dispose();
                videoTrack = new VideoStreamTrack(texture);
                videoTrack.Enabled = true;

                var stream = new MediaStream();
                stream.AddTrack(videoTrack);
                localPeer.AddTrack(videoTrack, stream);
                Debug.Log("[XREAL_WEBRTC][Track] New track added");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[XREAL_WEBRTC][Track] Failed to update track: {e.Message}\n{e.StackTrace}");
        }
    }

    void OnDestroy()
    {
        try
        {
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                recordingCoroutine = null;
            }

            if (videoHandler != null)
            {
                videoHandler.UnregisterTextureCallback(HandleTextureUpdated);
                videoHandler.Stop();
                videoHandler.Release();
                videoHandler = null;
            }

            if (videoTrack != null)
            {
                videoTrack.Dispose();
                videoTrack = null;
            }

            if (localPeer != null)
            {
                localPeer.Close();
                localPeer.Dispose();
                localPeer = null;
            }

            if (ws != null)
            {
                ws.Close();
                ws = null;
            }

            if (m_VideoCapture != null)
            {
                m_VideoCapture.StopRecordingAsync((result) => { });
                m_VideoCapture.Dispose();
                m_VideoCapture = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[XREAL_WEBRTC] Error during cleanup: {e.Message}");
        }
    }
}