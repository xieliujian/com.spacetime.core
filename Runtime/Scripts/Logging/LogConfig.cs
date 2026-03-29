namespace ST.Core.Logging
{
    /// <summary>
    /// 日志配置类
    /// </summary>
    public class LogConfig : ILogConfig
    {
        public LogLevel MinLevel { get; private set; }
        public ILogFormatter Formatter { get; private set; }
        public ILogWriter Writer { get; private set; }

        public LogConfig()
        {
            MinLevel = LogLevel.Debug;
            Formatter = new DefaultLogFormatter();
            Writer = null;
        }

        public ILogConfig SetMinLevel(LogLevel level)
        {
            MinLevel = level;
            return this;
        }

        public ILogConfig SetFormatter(ILogFormatter formatter)
        {
            Formatter = formatter;
            return this;
        }

        public ILogConfig SetWriter(ILogWriter writer)
        {
            Writer = writer;
            return this;
        }
    }
}
