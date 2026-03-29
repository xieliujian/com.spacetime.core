using System.IO;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 默认日志配置
    /// 支持链式配置
    /// </summary>
    public class LogConfig : ILogConfig
    {
        private string m_LogFilePath;
        private bool m_IsLowMemoryDevice;
        private int m_MaxFlushCount;
        private long m_MaxFileSize;
        private bool m_EnableBackup;

        public LogConfig()
        {
            // 默认配置
            m_LogFilePath = Path.Combine(Application.persistentDataPath, "Logs", "Output.txt");
            m_IsLowMemoryDevice = SystemInfo.systemMemorySize < 2048; // 2GB
            m_MaxFlushCount = m_IsLowMemoryDevice ? 20 : 100;
            m_MaxFileSize = 20 * 1024 * 1024; // 20MB
            m_EnableBackup = true;
        }

        // 链式配置方法
        public LogConfig SetLogFilePath(string path)
        {
            m_LogFilePath = path;
            return this;
        }

        public LogConfig SetLowMemoryDevice(bool isLowMemory)
        {
            m_IsLowMemoryDevice = isLowMemory;
            m_MaxFlushCount = isLowMemory ? 20 : 100;
            return this;
        }

        public LogConfig SetMaxFlushCount(int count)
        {
            m_MaxFlushCount = count;
            return this;
        }

        public LogConfig SetMaxFileSize(long size)
        {
            m_MaxFileSize = size;
            return this;
        }

        public LogConfig SetEnableBackup(bool enable)
        {
            m_EnableBackup = enable;
            return this;
        }

        // ILogConfig 实现
        public string GetLogFilePath() => m_LogFilePath;
        public bool IsLowMemoryDevice() => m_IsLowMemoryDevice;
        public int GetMaxFlushCount() => m_MaxFlushCount;
        public long GetMaxFileSize() => m_MaxFileSize;
        public bool EnableBackup() => m_EnableBackup;
    }
}
