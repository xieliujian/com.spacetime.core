namespace ST.Core.Logging
{
    /// <summary>
    /// 日志静态门面，对外提供统一的日志调用入口
    /// Log 函数委托给 <see cref="Debugger"/> 输出，文件写入通过 <see cref="LogManager"/> 监听
    /// <c>Application.logMessageReceived</c> 回调实现
    /// </summary>
    public static class Logger
    {
        /// <summary>当前日志管理器实例，由 Initialize 赋值</summary>
        static LogManager s_Manager;

        /// <summary>
        /// 使用指定配置初始化日志系统
        /// 若管理器尚未创建则自动构造默认的 <see cref="LogManager"/>
        /// </summary>
        /// <param name="config">日志配置</param>
        public static void Initialize(LogConfig config)
        {
            if (s_Manager == null)
            {
                s_Manager = new LogManager();
            }
            s_Manager.Initialize(config);
        }

        /// <summary>
        /// 启用或禁用对 Unity <c>Application.logMessageReceived</c> 的捕获
        /// </summary>
        /// <param name="enable">true 表示启用捕获，false 表示停止捕获</param>
        public static void EnableUnityLogCapture(bool enable)
        {
            if (s_Manager == null)
                return;

            s_Manager.EnableUnityLogCapture(enable);
        }

        /// <summary>
        /// 写入 Info 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Log(string message)
        {
            Debugger.LogInfo(message);
        }

        /// <summary>
        /// 写入 Warning 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogWarning(string message)
        {
            Debugger.LogWarning(message);
        }

        /// <summary>
        /// 写入 Error 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogError(string message)
        {
            Debugger.LogError(message);
        }

        /// <summary>
        /// 写入 Debug 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogDebug(string message)
        {
            Debugger.LogDebug(message);
        }

        /// <summary>
        /// 将缓存中的日志立即刷新到文件
        /// </summary>
        public static void Flush()
        {
            if (s_Manager == null)
                return;

            s_Manager.Flush();
        }

        /// <summary>
        /// 刷新并关闭日志系统，释放文件资源
        /// </summary>
        public static void Close()
        {
            if (s_Manager == null)
                return;

            s_Manager.Close();
        }
    }
}
