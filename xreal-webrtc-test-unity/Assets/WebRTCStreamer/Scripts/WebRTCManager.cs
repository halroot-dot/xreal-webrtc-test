using UnityEngine;
using Unity.WebRTC;
using System.Collections;
using System.Linq;

public class WebRTCManager : MonoBehaviour
{
    [SerializeField] private Camera editorCamera;
    [SerializeField] private WebRTCConfiguration configuration;

    private RTCPeerConnection localPeer;
    private SignalingManager signalingManager;
    private VideoStreamManager videoStreamManager;
    private VideoFileManager fileManager;
    private Coroutine recordingCoroutine;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        fileManager = new VideoFileManager();
        fileManager.CleanupTempFiles();

        videoStreamManager = new VideoStreamManager(configuration, editorCamera, fileManager);
        signalingManager = new SignalingManager(HandleSignalingMessage);

        StartCoroutine(WebRTC.Update());
        videoStreamManager.Initialize(() =>
        {
            SetupPeerConnection();
            signalingManager.Connect(configuration.GetFullServerUrl());
#if !UNITY_EDITOR
            StartCoroutine(videoStreamManager.RecordVideo());
#endif
        });
    }

    private void SetupPeerConnection()
    {
        var config = new RTCConfiguration
        {
            iceServers = configuration.IceServers.Select(url => new RTCIceServer { urls = new[] { url } }).ToArray()
        };

        localPeer = new RTCPeerConnection(ref config);

        localPeer.OnIceCandidate = candidate =>
        {
            if (candidate == null) return;

            var message = new SignalingMessage
            {
                type = "candidate",
                candidate = new CandidateData
                {
                    candidate = candidate.Candidate,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex ?? 0
                }
            };

            signalingManager.SendMessage(message);
        };

        localPeer.OnConnectionStateChange = state =>
        {
            XrealLogger.Log($"Connection State: {state}");
        };

        localPeer.OnIceConnectionChange = state =>
        {
            XrealLogger.Log($"ICE Connection State: {state}");
            if (state == RTCIceConnectionState.Failed)
            {
                StartCoroutine(CreateAndSendOffer());
            }
        };

        var transceiver = localPeer.AddTransceiver(TrackKind.Video);
        transceiver.Direction = RTCRtpTransceiverDirection.SendOnly;

        var videoTrack = videoStreamManager.GetVideoTrack();
        if (videoTrack != null)
        {
            var stream = new MediaStream();
            stream.AddTrack(videoTrack);
            localPeer.AddTrack(videoTrack, stream);
            StartCoroutine(CreateAndSendOffer());
        }
        else
        {
            XrealLogger.LogError("Failed to get video track");
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
                signalingManager.SendMessage(msg);
            }
        }
    }

    private void HandleSignalingMessage(SignalingMessage message)
    {
        if (message.type == "answer" && message.answer != null)
        {
            var answer = new RTCSessionDescription
            {
                type = RTCSdpType.Answer,
                sdp = message.answer.sdp
            };
            StartCoroutine(HandleAnswer(answer));
        }
        else if (message.type == "candidate" && message.candidate != null)
        {
            var candidate = new RTCIceCandidate(new RTCIceCandidateInit
            {
                candidate = message.candidate.candidate,
                sdpMid = message.candidate.sdpMid,
                sdpMLineIndex = message.candidate.sdpMLineIndex
            });
            localPeer.AddIceCandidate(candidate);
        }
    }

    private IEnumerator HandleAnswer(RTCSessionDescription answer)
    {
        var op = localPeer.SetRemoteDescription(ref answer);
        yield return new WaitUntil(() => op.IsDone);

        if (op.IsError)
        {
            XrealLogger.LogError($"Error setting remote description: {op.Error.message}");
        }
    }


    public void OnDestroy()
    {
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }

        videoStreamManager?.Cleanup();
        signalingManager?.Disconnect();

        if (localPeer != null)
        {
            localPeer.Close();
            localPeer.Dispose();
        }

        // fileManager?.CleanupTempFiles();
    }
}