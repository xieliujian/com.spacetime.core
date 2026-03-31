using System;

namespace ST.Core.Logging
{
    /// <summary>
    /// 默认日志格式化器，实现 <see cref="ILogFormatter"/>
    /// 输出格式：[LogLevel][Day HH:MM:SS Millisecond]Message
    /// </summary>
    public class DefaultLogFormatter : ILogFormatter
    {
        /// <summary>
        /// 将日志内容格式化为可读字符串
        /// 若包含堆栈跟踪，则在消息后另起一行追加
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="stackTrace">堆栈跟踪（为 null 或空字符串时不附加）</param>
        /// <param name="timestamp">记录时间</param>
        /// <returns>格式化后的日志字符串</returns>
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

        /// <summary>
        /// 将 <see cref="LogLevel"/> 枚举转换为日志文件中显示的字符串标签
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <returns>对应的字符串标签</returns>
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
