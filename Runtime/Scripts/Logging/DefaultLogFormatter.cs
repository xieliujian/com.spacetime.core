using System;

namespace ST.Core.Logging
{
    /// <summary>
    /// 默认日志格式化器，输出 <c>[级别][日 时:分:秒 毫秒]消息</c>，有堆栈时换行追加。
    /// </summary>
    public class DefaultLogFormatter
    {
        /// <summary>
        /// 按固定格式拼出一行日志；堆栈为空时不追加。
        /// </summary>
        /// <param name="level">日志级别。</param>
        /// <param name="message">日志消息。</param>
        /// <param name="stackTrace">堆栈跟踪，为 null 或空字符串时不附加。</param>
        /// <param name="timestamp">记录时间。</param>
        /// <returns>格式化后的日志文本。</returns>
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
        /// 把内部级别转换为日志文件中的标签，标签沿用 Unity 控制台的习惯名称。
        /// </summary>
        /// <param name="level">日志级别。</param>
        /// <returns>对应的字符串标签。</returns>
        string GetLevelString(LogLevel level)
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
