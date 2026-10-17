using System;
using System.IO;
using UnityEngine;

// Writes the game log to <persistentDataPath>/game.log (development builds only), so logs from a
// phone can be read on the Mac:  xcrun devicectl device copy from --domain-type appDataContainer ...
public static class FileLog
{
    private static StreamWriter writer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (!Debug.isDebugBuild || Application.isEditor) return;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "game.log");
            writer = new StreamWriter(path, append: false) { AutoFlush = true };
            writer.WriteLine($"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} {Application.platform} {Application.version} ===");
            Application.logMessageReceivedThreaded += (message, stack, type) =>
            {
                lock (writer)
                {
                    writer.WriteLine($"{DateTime.Now:HH:mm:ss} [{type}] {message}");
                    if (type == LogType.Exception || type == LogType.Error) writer.WriteLine(stack);
                }
            };
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FileLog] {e.Message}");
        }
    }
}
