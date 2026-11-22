namespace ST.Core.Logging
{
    /// <summary>
    /// 日志模块的固定文本与默认数值，集中存放级别标签、文件命名和阈值，避免散落在实现中。
    /// </summary>
    public static class LogDefine
    {
        // 变量定义
        public const string k_Debug = "Debug"; // 调试级别标签
        public const string k_Info = "Log"; // 信息级别标签，沿用 Unity 控制台命名
        public const string k_Warning = "LogWarning"; // 警告级别标签
        public const string k_Error = "LogError"; // 错误级别标签
        public const string k_Exception = "LogException"; // 异常级别标签
        public const string k_LogDirectory = "Logs"; // 默认日志目录
        public const string k_LogFileName = "Output.txt"; // 默认日志文件名
        public const string k_BackupSuffix = ".bak"; // 文件轮转备份后缀
        public const int k_LowMemoryThresholdMB = 2048; // 低内存判定阈值，单位 MB
        public const int k_LowMemoryFlushCount = 20; // 低内存设备的刷新阈值
        public const int k_NormalFlushCount = 100; // 普通设备的刷新阈值
        public const long k_DefaultMaxFileSize = 20 * 1024 * 1024; // 默认单文件上限 20MB
    }
}