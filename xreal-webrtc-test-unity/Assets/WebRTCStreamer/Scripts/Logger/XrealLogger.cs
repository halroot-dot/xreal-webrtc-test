using UnityEngine;
using System.Runtime.CompilerServices;

public static class XrealLogger
{
    private const string PREFIX = "[XREAL_WEBRTC]";

    public static void Log(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
    {
        var tag = GetTag(sourceFilePath);
        Debug.Log($"{PREFIX}[{tag}] {message}");
    }

    public static void LogWarning(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
    {
        var tag = GetTag(sourceFilePath);
        Debug.LogWarning($"{PREFIX}[{tag}] {message}");
    }

    public static void LogError(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
    {
        var tag = GetTag(sourceFilePath);
        Debug.LogError($"{PREFIX}[{tag}] {message}");
    }

    private static string GetTag(string sourceFilePath)
    {
        try
        {
            // ファイルパスから最後のファイル名を取得
            var fileName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            return fileName;
        }
        catch
        {
            return "Unknown";
        }
    }
}