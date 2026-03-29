using System;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器
    /// </summary>
    public class LogManager : ILogManager
    {
        private ILogConfig config;
        private bool isUnityLogCaptureEnabled;

        public void Initialize(ILogConfig config)
        {
            this.config = config;
            this.isUnityLogCaptureEnabled = false;
        }

        public void Log(LogLevel level, string message)
        {
            if (config == null)
            {
                return;
            }

            if (level < config.MinLevel)
            {
                return;
            }

            string formattedMessage = config.Formatter.Format(level, message);

            if (config.Writer != null)
            {
                config.Writer.Write(formattedMessage);
            }

            Debug.Log(formattedMessage);
        }

        public void Flush()
        {
            if (config?.Writer != null)
            {
                config.Writer.Flush();
            }
        }

        public void Close()
        {
            if (isUnityLogCaptureEnabled)
            {
                Application.logMessageReceived -= OnUnityLogCallback;
                isUnityLogCaptureEnabled = false;
            }

            if (config?.Writer != null)
            {
                config.Writer.Close();
            }
        }

        public void EnableUnityLogCapture()
        {
            // TODO: Implement in Task 11
        }

        private void OnUnityLogCallback(string condition, string stackTrace, LogType type)
        {
            // TODO: Implement in Task 11
        }

        private LogLevel ConvertUnityLogType(LogType type)
        {
            // TODO: Implement in Task 11
            return LogLevel.Debug;
        }
    }
}

