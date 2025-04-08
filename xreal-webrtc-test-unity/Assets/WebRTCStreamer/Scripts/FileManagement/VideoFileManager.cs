using UnityEngine;
using System;
using System.IO;

public class VideoFileManager
{
    private readonly string tempDirectory;

    public VideoFileManager()
    {
        tempDirectory = Application.temporaryCachePath;
    }

    public string GenerateVideoPath()
    {
        var filename = $"temp_video_{DateTime.Now.Ticks}.mp4";
        return Path.Combine(tempDirectory, filename);
    }

    public void DeleteVideoFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                XrealLogger.Log($"[VideoFileManager] Deleted file: {filePath}");
            }
        }
        catch (Exception e)
        {
            XrealLogger.LogError($"[VideoFileManager] Failed to delete file: {e.Message}");
        }
    }

    public void CleanupTempFiles()
    {
        try
        {
            var tempFiles = Directory.GetFiles(tempDirectory, "temp_video_*.mp4");
            foreach (var file in tempFiles)
            {
                DeleteVideoFile(file);
            }
            XrealLogger.Log($"[VideoFileManager] Deleted {tempFiles.Length} temporary video files");
        }
        catch (Exception e)
        {
            XrealLogger.LogError($"[VideoFileManager] Failed to cleanup temp files: {e.Message}");
        }
    }
}