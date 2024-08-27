using UnityEngine;

namespace Vibes
{
    public static class Logging
    {
        public enum LogLevel
        {
            // order matters, bottom ones will still trigger those above
            None,
            Error,
            Warning,
            All,
        }

        public static bool PauseLogging { get; set; }
        public static LogLevel DebugLogLevel { get; set; } = LogLevel.Warning;
        internal static bool LoggingAllEnabled => PauseLogging == false && DebugLogLevel == LogLevel.All;

        internal static void Log(string log, LogLevel logLevel = LogLevel.All)
        {
            if (PauseLogging) return;
            switch (logLevel)
            {
                case LogLevel.None:
                    return;
                case LogLevel.Error:
                    if (DebugLogLevel < LogLevel.Error) break;
                    Debug.LogError(log); break;
                case LogLevel.Warning:
                    if (DebugLogLevel < LogLevel.Warning) break;
                    Debug.LogWarning(log); break;
                case LogLevel.All:
                    if (DebugLogLevel < LogLevel.All) break;
                    Debug.Log(log); break;
            };
        }
    }
}
