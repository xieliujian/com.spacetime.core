using System;

namespace ST.Core.Logging
{
    /// <summary>
    /// 默认日志格式化器
    /// 格式: [LogLevel][Day HH:MM:SS Millisecond]Message
    /// </summary>
    public class DefaultLogFormatter : ILogFormatter
    {
        public string Format(LogLevel level, string message, string stackTrace, DateTime timestamp)
        {
            string timeStr = $"{timestamp.Day} {timestamp.Hour:D2}:{timestamp.Minute:D2}:{timestamp.Second:D2} {timestamp.Millisecond}";
            string levelStr = GetLevelString(level);

            if (string.IsNullOrEmpty(stackTrace))
            {
                return $"[{levelStr}][{timeStr}]{message}";
            }
            else
            {
                return $"[{levelStr}][{timeStr}]{message}\n at {stackTrace}";
            }
        }

        private string GetLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "Debug";
                case LogLevel.Info:
                    return "Log";
                case LogLevel.Warning:
                    return "LogWarning";
                case LogLevel.Error:
                    return "LogError";
                case LogLevel.Exception:
                    return "LogException";
                default:
                    return "Log";
            }
        }
    }
}
