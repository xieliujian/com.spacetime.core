namespace ST.Core.Logging
{
    /// <summary>
    /// 日志静态门面，对外提供统一的日志调用入口
    /// 内部委托给 <see cref="ILogManager"/> 实现，支持运行时替换管理器
    /// </summary>
    public static class Logger
    {
        /// <summary>当前日志管理器实例，由 Initialize 或 SetManager 赋值</summary>
        private static ILogManager manager;

        /// <summary>
        /// 使用指定配置初始化日志系统
        /// 若管理器尚未创建则自动构造默认的 <see cref="LogManager"/>
        /// </summary>
        /// <param name="config">日志配置</param>
        public static void Initialize(ILogConfig config)
        {
            if (manager == null)
            {
                manager = new LogManager();
            }
            manager.Initialize(config);
        }

        /// <summary>
        /// 替换当前日志管理器，用于单元测试或自定义实现
        /// </summary>
        /// <param name="customManager">自定义日志管理器</param>
        public static void SetManager(ILogManager customManager)
        {
            manager = customManager;
        }

        /// <summary>
        /// 启用或禁用对 Unity <c>Application.logMessageReceived</c> 的捕获
        /// </summary>
        /// <param name="enable">true 表示启用捕获，false 表示停止捕获</param>
        public static void EnableUnityLogCapture(bool enable)
        {
            if (manager == null)
            {
                return;
            }
            manager.EnableUnityLogCapture(enable);
        }

        /// <summary>
        /// 写入 Info 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Log(string message)
        {
            if (manager == null)
            {
                return;
            }
            manager.Log(LogLevel.Info, message);
        }

        /// <summary>
        /// 写入 Warning 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogWarning(string message)
        {
            if (manager == null)
            {
                return;
            }
            manager.Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// 写入 Error 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogError(string message)
        {
            if (manager == null)
            {
                return;
            }
            manager.Log(LogLevel.Error, message);
        }

        /// <summary>
        /// 写入 Debug 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void LogDebug(string message)
        {
            if (manager == null)
            {
                return;
            }
            manager.Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// 将缓存中的日志立即刷新到文件
        /// </summary>
        public static void Flush()
        {
            if (manager == null)
            {
                return;
            }
            manager.Flush();
        }

        /// <summary>
        /// 刷新并关闭日志系统，释放文件资源
        /// </summary>
        public static void Close()
        {
            if (manager == null)
            {
                return;
            }
            manager.Close();
        }
    }
}
