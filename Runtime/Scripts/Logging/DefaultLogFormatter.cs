using System;

namespace ST.Core.Logging
{
    /// <summary>
    /// 默认日志格式化器
    /// 格式: [LogLevel][Day HH:MM:SS Millisecond]Message
    /// </summary>
    public class DefaultLogFormatter : ILogFormatter
    {
        public string Format(LogLevel level, string message)
        {
            DateTime now = DateTime.Now;
            string timestamp = $"{now.Day:D2} {now:HH:mm:ss} {now.Millisecond:D3}";
            return $"[{level}][{timestamp}]{message}";
        }
    }
}
