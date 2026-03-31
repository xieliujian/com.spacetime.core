namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器接口，定义日志系统的核心控制能力
    /// </summary>
    public interface ILogManager
    {
        /// <summary>
        /// 使用指定配置初始化日志管理器
        /// </summary>
        /// <param name="config">日志配置</param>
        void Initialize(ILogConfig config);

        /// <summary>
        /// 写入一条指定级别的日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="stackTrace">堆栈跟踪（可选，为 null 时不附加）</param>
        void Log(LogLevel level, string message, string stackTrace = null);

        /// <summary>
        /// 启用或禁用对 Unity <c>Application.logMessageReceived</c> 的捕获
        /// </summary>
        /// <param name="enable">true 表示启用捕获，false 表示停止捕获</param>
        void EnableUnityLogCapture(bool enable);

        /// <summary>
        /// 将缓存中的日志立即刷新到持久化存储
        /// </summary>
        void Flush();

        /// <summary>
        /// 刷新并关闭日志管理器，释放所有占用的资源
        /// </summary>
        void Close();
    }
}
