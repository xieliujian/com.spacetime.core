namespace ST.Core.Logging
{
    /// <summary>
    /// 日志配置接口
    /// 抽象外部依赖（FilePath、DeviceModule 等）
    /// </summary>
    public interface ILogConfig
    {
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        string GetLogFilePath();

        /// <summary>
        /// 是否为低内存设备
        /// </summary>
        bool IsLowMemoryDevice();

        /// <summary>
        /// 获取最大刷新数量
        /// </summary>
        int GetMaxFlushCount();

        /// <summary>
        /// 获取最大文件大小（字节）
        /// </summary>
        long GetMaxFileSize();

        /// <summary>
        /// 是否启用备份
        /// </summary>
        bool EnableBackup();
    }
}
