using System;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器，负责把格式化后的日志写入文件，并承接 Unity 控制台日志。
    /// <para>由 <see cref="STLogger.Initialize"/> 创建，业务代码通常不直接实例化。</para>
    /// </summary>
    public class LogManager
    {
        /// <summary>日志写入器；初始化失败时为 null，此时所有写入会被丢弃。</summary>
        FileLogWriter m_Writer;

        /// <summary>日志格式化器，把级别、消息和堆栈拼成一行文本。</summary>
        DefaultLogFormatter m_Formatter;

        /// <summary>初始化时传入的配置，保存路径与轮转参数。</summary>
        LogConfig m_Config;

        /// <summary>Unity 日志回调是否已订阅，用于避免重复订阅或重复取消。</summary>
        bool m_UnityLogCaptureEnabled;

        /// <summary>
        /// 根据配置创建格式化器和文件写入器。
        /// <para>重复调用会用新的写入器覆盖旧实例，旧文件句柄不会被关闭。</para>
        /// </summary>
        /// <param name="config">日志配置，不可为 null。</param>
        public void Initialize(LogConfig config)
        {
            m_Config = config;
            m_Formatter = new DefaultLogFormatter();
            m_Writer = new FileLogWriter(
                config.GetLogFilePath(),
                config.GetMaxFlushCount(),
                config.GetMaxFileSize(),
                config.EnableBackup()
            );
        }

        /// <summary>
        /// 订阅或取消 Unity 控制台日志回调。
        /// <para>状态未变化时不做任何处理，避免重复订阅。</para>
        /// </summary>
        /// <param name="enable">true 表示开始监听，false 表示停止监听。</param>
        public void EnableUnityLogCapture(bool enable)
        {
            if (m_UnityLogCaptureEnabled == enable)
                return;

            m_UnityLogCaptureEnabled = enable;

            if (enable)
            {
                Application.logMessageReceived += OnUnityLogCallback;
            }
            else
            {
                Application.logMessageReceived -= OnUnityLogCallback;
            }
        }

        /// <summary>
        /// 把写入器缓存中的日志立即落盘。
        /// </summary>
        public void Flush()
        {
            if (m_Writer == null)
                return;

            m_Writer.Flush();
        }

        /// <summary>
        /// 取消 Unity 日志订阅，刷新缓存并关闭文件句柄。
        /// <para>重复调用是安全的。</para>
        /// </summary>
        public void Close()
        {
            EnableUnityLogCapture(false);
            if (m_Writer == null)
                return;

            m_Writer.Close();
        }

        /// <summary>
        /// Unity 控制台日志回调，把日志内容和堆栈转交写入器。
        /// </summary>
        /// <param name="condition">日志消息内容。</param>
        /// <param name="stackTrace">Unity 生成的堆栈跟踪。</param>
        /// <param name="type">Unity 日志类型，决定写入的级别。</param>
        void OnUnityLogCallback(string condition, string stackTrace, LogType type)
        {
            LogLevel level = ConvertUnityLogType(type);
            Log(level, condition, stackTrace);
        }

        /// <summary>
        /// 格式化一条日志并交给写入器；写入器为空时直接丢弃。
        /// </summary>
        /// <param name="level">日志级别。</param>
        /// <param name="message">日志消息。</param>
        /// <param name="stackTrace">堆栈跟踪，可为 null。</param>
        void Log(LogLevel level, string message, string stackTrace = null)
        {
            if (m_Writer == null)
                return;

            string formatted = m_Formatter.Format(level, message, stackTrace, DateTime.Now);
            m_Writer.Write(formatted);
        }

        /// <summary>
        /// 把 Unity 日志类型映射到内部级别；Assert 按错误处理，未知类型按 Info 处理。
        /// </summary>
        /// <param name="type">Unity 日志类型。</param>
        /// <returns>对应的内部日志级别。</returns>
        LogLevel ConvertUnityLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    return LogLevel.Error;
                case LogType.Exception:
                    return LogLevel.Exception;
                case LogType.Warning:
                    return LogLevel.Warning;
                case LogType.Assert:
                    return LogLevel.Error;
                default:
                    return LogLevel.Info;
            }
        }
    }
}
