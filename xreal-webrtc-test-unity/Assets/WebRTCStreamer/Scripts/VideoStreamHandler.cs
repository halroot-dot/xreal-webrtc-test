using UnityEngine;
using System;
using System.IO;

public class VideoStreamHandler
{
    private class SurfaceTextureListener : AndroidJavaProxy
    {
        private Action onFrameAvailableCallback;

        public SurfaceTextureListener(Action callback) : base("android.graphics.SurfaceTexture$OnFrameAvailableListener")
        {
            onFrameAvailableCallback = callback;
        }

        // SurfaceTexture$OnFrameAvailableListenerのインターフェースメソッドを実装
        public void onFrameAvailable(AndroidJavaObject surfaceTexture)
        {
            onFrameAvailableCallback?.Invoke();
        }
    }

    private class MediaPlayerListener : AndroidJavaProxy
    {
        private Action onPreparedCallback;

        public MediaPlayerListener(Action callback) : base("android.media.MediaPlayer$OnPreparedListener")
        {
            onPreparedCallback = callback;
        }

        // MediaPlayer$OnPreparedListenerのインターフェースメソッドを実装
        public void onPrepared(AndroidJavaObject mp)
        {
            onPreparedCallback?.Invoke();
        }
    }

    private AndroidJavaObject mediaPlayer;
    private AndroidJavaObject surfaceTexture;
    private RenderTexture targetTexture;
    private bool isPlaying = false;
    private event Action<RenderTexture> OnTextureUpdated;
    private bool isTextureReady = false;
    private bool isMediaPlayerPrepared = false;
    private bool isPendingPlay = false;
    private readonly object mediaPlayerLock = new object();
    private bool isDisposed = false;

    public VideoStreamHandler(int width = 1280, int height = 720)
    {
        XrealLogger.Log($"[Handler] Creating VideoStreamHandler {width}x{height}");

        try
        {
            // RenderTextureの作成
            targetTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            if (!targetTexture.Create())
            {
                throw new Exception("Failed to create RenderTexture");
            }
            XrealLogger.Log($"[Handler] RenderTexture created: {targetTexture.width}x{targetTexture.height}");

            if (Application.platform == RuntimePlatform.Android)
            {
                lock (mediaPlayerLock)
                {
                    InitializeAndroidMediaPlayer(width, height);
                }
            }
        }
        catch (Exception e)
        {
            XrealLogger.LogError($"[Handler] Initialization error: {e.Message}\n{e.StackTrace}");
            Release(); // エラー時のクリーンアップ
        }
    }

    private void InitializeAndroidMediaPlayer(int width, int height)
    {
        // MediaPlayerの作成
        mediaPlayer = new AndroidJavaObject("android.media.MediaPlayer");

        // SurfaceTextureの作成とリスナーの設定
        surfaceTexture = new AndroidJavaObject("android.graphics.SurfaceTexture", (int)targetTexture.GetNativeTexturePtr());
        surfaceTexture.Call("setDefaultBufferSize", width, height);

        var frameListener = new SurfaceTextureListener(() =>
        {
            isTextureReady = true;
            OnTextureUpdate();
        });
        surfaceTexture.Call("setOnFrameAvailableListener", frameListener);

        // Surfaceの作成とMediaPlayerへの設定
        using (var surface = new AndroidJavaObject("android.view.Surface", surfaceTexture))
        {
            mediaPlayer.Call("setSurface", surface);
        }

        // MediaPlayerの準備完了リスナーの設定
        var preparedListener = new MediaPlayerListener(() =>
        {
            isTextureReady = true;
            OnTextureUpdate();
        });
        mediaPlayer.Call("setOnPreparedListener", preparedListener);

        XrealLogger.Log("[Handler] MediaPlayer initialized successfully");
    }

    private void OnTextureUpdate()
    {
        if (isDisposed || !isTextureReady) return;

        try
        {
            lock (mediaPlayerLock)
            {
                if (targetTexture != null)
                {
                    surfaceTexture?.Call("updateTexImage");
                    OnTextureUpdated?.Invoke(targetTexture);
                }
            }
        }
        catch (Exception e)
        {
            XrealLogger.LogError($"[Handler] Texture update error: {e.Message}");
        }
    }

    public void LoadVideo(string path)
    {
        if (isDisposed) return;

        lock (mediaPlayerLock)
        {
            try
            {
                if (mediaPlayer != null)
                {
                    Stop();
                    isMediaPlayerPrepared = false;
                    mediaPlayer.Call("reset");
                    mediaPlayer.Call("setDataSource", path);

                    // 準備完了時のコールバックを設定
                    mediaPlayer.Call("setOnPreparedListener", new MediaPlayerListener(() =>
                    {
                        isMediaPlayerPrepared = true;
                        if (isPendingPlay)
                        {
                            Play();
                        }
                    }));

                    mediaPlayer.Call("prepare");
                    mediaPlayer.Call("setLooping", true);
                    XrealLogger.Log($"[Handler] Video loaded: {path}");
                }
            }
            catch (Exception e)
            {
                XrealLogger.LogError($"[Handler] Failed to load video: {e.Message}");
            }
        }
    }

    public void Play()
    {
        if (mediaPlayer != null)
        {
            if (isMediaPlayerPrepared)
            {
                try
                {
                    mediaPlayer.Call("start");
                    isPlaying = true;
                    isPendingPlay = false;
                    XrealLogger.Log("[Handler] MediaPlayer started");
                }
                catch (Exception e)
                {
                    XrealLogger.LogError($"[Handler] Failed to start MediaPlayer: {e.Message}");
                }
            }
            else
            {
                isPendingPlay = true;
                XrealLogger.Log("[Handler] Play pending until prepared");
            }
        }
    }

    public void Stop()
    {
        if (mediaPlayer != null && isPlaying)
        {
            try
            {
                mediaPlayer.Call("stop");
                isPlaying = false;
                isPendingPlay = false;
                XrealLogger.Log("[Handler] MediaPlayer stopped");
            }
            catch (Exception e)
            {
                XrealLogger.LogError($"[Handler] Failed to stop MediaPlayer: {e.Message}");
            }
        }
    }

    public void Release()
    {
        if (isDisposed) return;
        isDisposed = true;

        lock (mediaPlayerLock)
        {
            if (mediaPlayer != null)
            {
                try
                {
                    if (isPlaying)
                    {
                        Stop();
                    }
                    mediaPlayer.Call("release");
                }
                catch (Exception e)
                {
                    XrealLogger.LogError($"[Handler] Error releasing MediaPlayer: {e.Message}");
                }
                finally
                {
                    mediaPlayer = null;
                    isMediaPlayerPrepared = false;
                    isPendingPlay = false;
                }
            }

            if (surfaceTexture != null)
            {
                try
                {
                    surfaceTexture.Call("release");
                }
                catch (Exception e)
                {
                    XrealLogger.LogError($"[Handler] Error releasing SurfaceTexture: {e.Message}");
                }
                finally
                {
                    surfaceTexture = null;
                }
            }

            if (targetTexture != null)
            {
                targetTexture.Release();
                UnityEngine.Object.Destroy(targetTexture);
                targetTexture = null;
            }

            isPlaying = false;
            isTextureReady = false;
        }
    }

    public RenderTexture GetTexture()
    {
        return targetTexture;
    }

    public void RegisterTextureCallback(Action<RenderTexture> callback)
    {
        OnTextureUpdated += callback;
        if (isTextureReady)
        {
            callback?.Invoke(targetTexture);
        }
    }

    public void UnregisterTextureCallback(Action<RenderTexture> callback)
    {
        OnTextureUpdated -= callback;
    }
}
