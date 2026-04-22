using System;
using System.Diagnostics;
using System.IO;

namespace VRC_OSC_Handy.Logger
{
    public static class DebugLogger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, $"log_{DateTime.Now:dd.MM.yyyy}.txt");
        private static readonly object _lock = new object();

        static DebugLogger()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
        }

        public static void Log(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            string errorMessage = ex != null ? $"{message}\nException: {ex}" : message;
            WriteLog("ERROR", errorMessage);
        }

        private static void WriteLog(string level, string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

            lock (_lock)
            {
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }

            Debug.WriteLine(logEntry);
        }
    }
}
