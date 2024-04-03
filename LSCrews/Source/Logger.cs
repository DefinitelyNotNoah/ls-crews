using System;
using System.IO;

namespace LSCrews.Source;

public static class Logger
{
    private const string LogFile = "log.txt";
    
    private static readonly string LogPath = StorageManager.MainDirectory + LogFile;

    public static void Log(object message)
    {
        File.AppendAllText(LogPath,
            DateTime.Now + " : " + message + Environment.NewLine);
    }
}