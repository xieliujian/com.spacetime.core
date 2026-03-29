using System;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志格式化器接口
    /// </summary>
    public interface ILogFormatter
    {
        /// <summary>
        /// 格式化日志消息
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">消息内容</param>
        /// <param name="stackTrace">堆栈跟踪（可选）</param>
        /// <param name="timestamp">时间戳</param>
        /// <returns>格式化后的日志字符串</returns>
        string Format(LogLevel level, string message, string stackTrace, DateTime timestamp);
    }
}
