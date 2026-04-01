using System.IO;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志配置，支持链式调用风格进行参数配置
    /// </summary>
    public class LogConfig
    {
        /// <summary>日志文件输出路径</summary>
        string m_LogFilePath;

        /// <summary>是否为低内存设备（系统内存小于 2GB）</summary>
        bool m_IsLowMemoryDevice;

        /// <summary>触发自动刷新的缓存条目数量阈值</summary>
        int m_MaxFlushCount;

        /// <summary>日志文件最大允许大小（字节），超出后执行轮转</summary>
        long m_MaxFileSize;

        /// <summary>超出大小限制时是否保留 .bak 备份文件</summary>
        bool m_EnableBackup;

        /// <summary>
        /// 构造函数，按设备内存情况自动初始化默认配置
        /// 低内存设备（小于 2GB）使用更小的刷新阈值以减少内存占用
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
        /// 设置日志文件输出路径
        /// </summary>
        /// <param name="path">完整文件路径</param>
        /// <returns>当前实例，支持链式调用</returns>
        public LogConfig SetLogFilePath(string path)
        {
            m_LogFilePath = path;
            return this;
        }

        /// <summary>
        /// 标记当前设备是否为低内存设备，并自动调整刷新阈值
        /// </summary>
        /// <param name="isLowMemory">true 表示低内存，刷新阈值设为 20；否则设为 100</param>
        /// <returns>当前实例，支持链式调用</returns>
        public LogConfig SetLowMemoryDevice(bool isLowMemory)
        {
            m_IsLowMemoryDevice = isLowMemory;
            m_MaxFlushCount = isLowMemory ? 20 : 100;
            return this;
        }

        /// <summary>
        /// 手动指定触发刷新的缓存条目数量阈值
        /// </summary>
        /// <param name="count">缓存条目数量，达到此值时自动刷新</param>
        /// <returns>当前实例，支持链式调用</returns>
        public LogConfig SetMaxFlushCount(int count)
        {
            m_MaxFlushCount = count;
            return this;
        }

        /// <summary>
        /// 设置日志文件的最大大小，超出后执行文件轮转
        /// </summary>
        /// <param name="size">最大文件大小（字节）</param>
        /// <returns>当前实例，支持链式调用</returns>
        public LogConfig SetMaxFileSize(long size)
        {
            m_MaxFileSize = size;
            return this;
        }

        /// <summary>
        /// 设置文件超出大小限制时是否保留 .bak 备份
        /// </summary>
        /// <param name="enable">true 表示保留备份，false 表示直接删除旧文件</param>
        /// <returns>当前实例，支持链式调用</returns>
        public LogConfig SetEnableBackup(bool enable)
        {
            m_EnableBackup = enable;
            return this;
        }

        /// <summary>获取日志文件路径</summary>
        public string GetLogFilePath() => m_LogFilePath;

        /// <summary>是否为低内存设备</summary>
        public bool IsLowMemoryDevice() => m_IsLowMemoryDevice;

        /// <summary>获取最大刷新数量</summary>
        public int GetMaxFlushCount() => m_MaxFlushCount;

        /// <summary>获取最大文件大小（字节）</summary>
        public long GetMaxFileSize() => m_MaxFileSize;

        /// <summary>是否启用备份</summary>
        public bool EnableBackup() => m_EnableBackup;
    }
}
