namespace ST.Core.Logging
{
    /// <summary>
    /// 日志级别，按严重程度从低到高排列；与 Unity <see cref="UnityEngine.LogType"/> 的映射见
    /// <see cref="LogManager"/> 中 Unity 控制台日志回调的处理。
    /// </summary>
    public enum LogLevel
    {
        Debug = 0, // 调试级别，用于开发阶段的详细诊断信息
        Info = 1, // 信息级别，用于记录正常的运行流程
        Warning = 2, // 警告级别，表示潜在问题但不影响正常运行
        Error = 3, // 错误级别，表示发生了可恢复的错误
        Exception = 4, // 异常级别，对应 Unity 的 Exception 日志类型，通常伴有堆栈跟踪
    }
}