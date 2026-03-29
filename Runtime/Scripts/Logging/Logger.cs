namespace ST.Core.Logging
{
    /// <summary>
    /// 日志静态门面
    /// </summary>
    public static class Logger
    {
        private static ILogManager manager;

        public static void Initialize(ILogConfig config)
        {
            if (manager == null)
            {
                manager = new LogManager();
            }
            manager.Initialize(config);
        }

        public static void SetManager(ILogManager customManager)
        {
            manager = customManager;
        }

        public static void EnableUnityLogCapture()
        {
            manager?.EnableUnityLogCapture();
        }

        public static void Log(string message)
        {
            manager?.Log(LogLevel.Info, message);
        }

        public static void LogWarning(string message)
        {
            manager?.Log(LogLevel.Warning, message);
        }

        public static void LogError(string message)
        {
            manager?.Log(LogLevel.Error, message);
        }

        public static void LogDebug(string message)
        {
            manager?.Log(LogLevel.Debug, message);
        }

        public static void Flush()
        {
            manager?.Flush();
        }

        public static void Close()
        {
            manager?.Close();
        }
    }
}

