using UnityEngine;
using NRKernal.Record;
using Unity.WebRTC;
using System;
using System.Collections;

public class VideoStreamManager
{
    private NRVideoCapture videoCapture;
    private VideoStreamTrack videoTrack;
    private VideoStreamHandler videoHandler;
    private readonly WebRTCConfiguration config;
    private Camera editorCamera;
    private RenderTexture editorRenderTexture;
    private Texture activeTexture;
    private VideoFileManager fileManager;

    public VideoStreamManager(WebRTCConfiguration config, Camera editorCamera = null, VideoFileManager fileManager = null)
    {
        this.config = config;
        this.editorCamera = editorCamera;
        this.fileManager = fileManager;
    }

    public void Initialize(Action onInitialized)
    {
#if UNITY_EDITOR
        InitializeEditorCamera(onInitialized);
#else
        InitializeNRVideoCapture(onInitialized);
#endif
    }

    private void InitializeEditorCamera(Action onInitialized)
    {
        if (editorCamera == null)
        {
            editorCamera = Camera.main;
        }
        if (editorCamera == null)
        {
            XrealLogger.LogError("[VideoStreamManager][Editor] No camera found!");
            return;
        }

        editorRenderTexture = new RenderTexture(config.VideoWidth, config.VideoHeight, 0, RenderTextureFormat.BGRA32);
        editorRenderTexture.Create();
        editorCamera.targetTexture = editorRenderTexture;
        activeTexture = editorRenderTexture;

        CreateVideoTrack(activeTexture);
        onInitialized?.Invoke();
    }

    private void InitializeNRVideoCapture(Action onInitialized)
    {
        try
        {
            videoHandler = new VideoStreamHandler(config.VideoWidth, config.VideoHeight);
            NRVideoCapture.CreateAsync(false, delegate (NRVideoCapture videoCapture)
            {
                if (videoCapture != null)
                {
                    this.videoCapture = videoCapture;
                    StartVideoCapture(onInitialized);
                }
                else
                {
                    XrealLogger.LogError("[VideoStreamManager] Failed to create VideoCapture");
                }
            });
        }
        catch (Exception e)
        {
            XrealLogger.LogError($"[VideoStreamManager] Failed to initialize: {e}");
        }
    }

    private void StartVideoCapture(Action onStarted)
    {
        CameraParameters cameraParams = new CameraParameters()
        {
            hologramOpacity = 1.0f,
            frameRate = config.FrameRate,
            cameraResolutionWidth = config.VideoWidth,
            cameraResolutionHeight = config.VideoHeight,
            pixelFormat = CapturePixelFormat.BGRA32,
            blendMode = BlendMode.VirtualOnly,
            audioState = NRVideoCapture.AudioState.None
        };

        videoCapture.StartVideoModeAsync(cameraParams, (result) =>
        {
            if (result.success)
            {
                XrealLogger.Log("[VideoStreamManager] Started video mode successfully");
                activeTexture = videoCapture.PreviewTexture;
                if (activeTexture != null)
                {
                    CreateVideoTrack(activeTexture);
                    onStarted?.Invoke();
                }
            }
            else
            {
                XrealLogger.LogError("[VideoStreamManager] Failed to start video mode");
            }
        });
    }

    public VideoStreamTrack GetVideoTrack()
    {
        if (videoTrack == null && activeTexture != null)
        {
            CreateVideoTrack(activeTexture);
        }
        return videoTrack;
    }

    private void CreateVideoTrack(Texture texture)
    {
        try
        {
            XrealLogger.Log("[VideoStreamManager] Creating new video track from texture");
            if (videoTrack != null)
            {
                videoTrack.Dispose();
            }

            if (texture == null)
            {
                XrealLogger.LogError("[VideoStreamManager] Cannot create video track: texture is null");
                return;
            }

            videoTrack = new VideoStreamTrack(texture);
            videoTrack.Enabled = true;
        }
        catch (Exception e)
        {
            XrealLogger.LogError($"[VideoStreamManager] Error creating video track: {e}");
            videoTrack = null;
        }
    }

    public IEnumerator RecordVideo()
    {
        string currentVideoPath = null;
        while (true) // 永続的なループ
        {
            string newVideoPath = fileManager.GenerateVideoPath();
            Debug.Log($"[XREAL_WEBRTC][Record] Starting new recording to: {newVideoPath}");
            bool started = false;

            try
            {
                videoCapture.StartRecordingAsync(newVideoPath, (result) =>
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
            yield return new WaitForSeconds(config.RecordDurationSeconds);

            // 次の録画への切り替え準備
            string previousPath = currentVideoPath;
            currentVideoPath = newVideoPath;

            // 現在の録画を停止
            bool stopped = false;
            try
            {
                videoCapture.StopRecordingAsync((result) =>
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
                fileManager.DeleteVideoFile(previousPath);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    public void Cleanup()
    {
#if UNITY_EDITOR
        if (editorRenderTexture != null)
        {
            editorRenderTexture.Release();
            UnityEngine.Object.Destroy(editorRenderTexture);
        }
#else
        if (videoCapture != null)
        {
            videoCapture.StopRecordingAsync((result) => { });
            videoCapture.Dispose();
            videoCapture = null;
        }

        if (videoHandler != null)
        {
            videoHandler.Stop();
            videoHandler.Release();
            videoHandler = null;
        }
#endif

        if (videoTrack != null)
        {
            videoTrack.Dispose();
            videoTrack = null;
        }
    }
}