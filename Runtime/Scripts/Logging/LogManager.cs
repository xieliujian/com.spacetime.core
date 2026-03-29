using System;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器
    /// 协调 Writer 和 Formatter，管理 Unity 日志捕获
    /// </summary>
    public class LogManager : ILogManager
    {
        private ILogWriter m_Writer;
        private ILogFormatter m_Formatter;
        private ILogConfig m_Config;
        private bool m_UnityLogCaptureEnabled;

        public void Initialize(ILogConfig config)
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

        public void Log(LogLevel level, string message, string stackTrace = null)
        {
            if (m_Writer == null) return;

            string formatted = m_Formatter.Format(level, message, stackTrace, DateTime.Now);
            m_Writer.Write(formatted);
        }

        public void EnableUnityLogCapture(bool enable)
        {
            if (m_UnityLogCaptureEnabled == enable) return;

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

        public void Flush()
        {
            m_Writer?.Flush();
        }

        public void Close()
        {
            EnableUnityLogCapture(false);
            m_Writer?.Close();
        }

        private void OnUnityLogCallback(string condition, string stackTrace, LogType type)
        {
            LogLevel level = ConvertUnityLogType(type);
            Log(level, condition, stackTrace);
        }

        private LogLevel ConvertUnityLogType(LogType type)
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

