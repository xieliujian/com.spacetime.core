using System.IO;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志配置，保存输出路径、刷新阈值与轮转参数，支持链式设置。
    /// </summary>
    public class LogConfig
    {
        /// <summary>日志文件完整路径，默认位于 persistentDataPath/Logs/Output.txt。</summary>
        string m_LogFilePath;

        /// <summary>是否按低内存设备处理，影响默认刷新阈值。</summary>
        bool m_IsLowMemoryDevice;

        /// <summary>缓存多少条日志后自动刷新。</summary>
        int m_MaxFlushCount;

        /// <summary>单个日志文件的字节上限，超出后在下次打开时轮转。</summary>
        long m_MaxFileSize;

        /// <summary>超限时是否保留 .bak 备份。</summary>
        bool m_EnableBackup;

        /// <summary>
        /// 按设备内存情况设置默认值。
        /// <para>内存小于 2GB 时刷新阈值取 20，否则取 100；文件上限 20MB，默认保留备份。</para>
        /// </summary>
        public LogConfig()
        {
            m_LogFilePath = Path.Combine(Application.persistentDataPath, "Logs", "Output.txt");
            m_IsLowMemoryDevice = SystemInfo.systemMemorySize < 2048; // 2GB
            m_MaxFlushCount = m_IsLowMemoryDevice ? 20 : 100;
            m_MaxFileSize = 20 * 1024 * 1024; // 20MB
            m_EnableBackup = true;
        }

        /// <summary>
        /// 设置日志输出文件路径。
        /// </summary>
        /// <param name="path">完整文件路径。</param>
        /// <returns>当前实例，支持链式调用。</returns>
        public LogConfig SetLogFilePath(string path)
        {
            m_LogFilePath = path;
            return this;
        }

        /// <summary>
        /// 标记是否为低内存设备，并同步调整刷新阈值。
        /// </summary>
        /// <param name="isLowMemory">true 表示低内存，刷新阈值设为 20；否则设为 100。</param>
        /// <returns>当前实例，支持链式调用。</returns>
        public LogConfig SetLowMemoryDevice(bool isLowMemory)
        {
            m_IsLowMemoryDevice = isLowMemory;
            m_MaxFlushCount = isLowMemory ? 20 : 100;
            return this;
        }

        /// <summary>
        /// 手动覆盖自动刷新阈值。
        /// </summary>
        /// <param name="count">缓存达到该条数时自动刷新。</param>
        /// <returns>当前实例，支持链式调用。</returns>
        public LogConfig SetMaxFlushCount(int count)
        {
            m_MaxFlushCount = count;
            return this;
        }

        /// <summary>
        /// 设置单个日志文件的字节上限。
        /// </summary>
        /// <param name="size">最大文件大小（字节）。</param>
        /// <returns>当前实例，支持链式调用。</returns>
        public LogConfig SetMaxFileSize(long size)
        {
            m_MaxFileSize = size;
            return this;
        }

        /// <summary>
        /// 设置超限时是否保留 .bak 备份。
        /// </summary>
        /// <param name="enable">true 表示保留备份，false 表示直接删除旧文件。</param>
        /// <returns>当前实例，支持链式调用。</returns>
        public LogConfig SetEnableBackup(bool enable)
        {
            m_EnableBackup = enable;
            return this;
        }

        /// <summary>读取当前日志文件路径。</summary>
        public string GetLogFilePath() => m_LogFilePath;

        /// <summary>当前是否按低内存设备配置。</summary>
        public bool IsLowMemoryDevice() => m_IsLowMemoryDevice;

        /// <summary>读取自动刷新阈值。</summary>
        public int GetMaxFlushCount() => m_MaxFlushCount;

        /// <summary>读取单个日志文件的字节上限。</summary>
        public long GetMaxFileSize() => m_MaxFileSize;

        /// <summary>当前是否启用备份。</summary>
        public bool EnableBackup() => m_EnableBackup;
    }
}
