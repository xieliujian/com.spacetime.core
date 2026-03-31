namespace ST.Core.Logging
{
    /// <summary>
    /// 日志级别，按严重程度从低到高排列
    /// </summary>
    public enum LogLevel
    {
        /// <summary>调试级别，用于开发阶段的详细诊断信息</summary>
        Debug = 0,

        /// <summary>信息级别，用于记录正常的运行流程</summary>
        Info = 1,

        /// <summary>警告级别，表示潜在问题但不影响正常运行</summary>
        Warning = 2,

        /// <summary>错误级别，表示发生了可恢复的错误</summary>
        Error = 3,

        /// <summary>异常级别，对应 Unity 的 Exception 日志类型，通常伴有堆栈跟踪</summary>
        Exception = 4
    }
}
