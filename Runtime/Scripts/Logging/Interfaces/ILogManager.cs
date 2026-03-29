namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器接口
    /// </summary>
    public interface ILogManager
    {
        /// <summary>
        /// 初始化日志管理器
        /// </summary>
        void Initialize(ILogConfig config);

        /// <summary>
        /// 写入日志
        /// </summary>
        void Log(LogLevel level, string message, string stackTrace = null);

        /// <summary>
        /// 启用/禁用 Unity 日志捕获
        /// </summary>
        void EnableUnityLogCapture(bool enable);

        /// <summary>
        /// 刷新日志
        /// </summary>
        void Flush();

        /// <summary>
        /// 关闭日志管理器
        /// </summary>
        void Close();
    }
}
