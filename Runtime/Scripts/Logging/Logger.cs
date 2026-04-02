using System;
using System.Diagnostics;

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

        // ──────────────────────────────────────────
        // 初始化 / 文件系统
        // ──────────────────────────────────────────

        /// <summary>
        /// 使用指定配置初始化日志系统
        /// 若管理器尚未创建则自动构造默认的 <see cref="LogManager"/>
        /// </summary>
        public static void Initialize(LogConfig config)
        {
            if (s_Manager == null)
            {
                s_Manager = new LogManager();
            }
            s_Manager.Initialize(config);
        }

        /// <summary>
        /// 将所有级别开关设为同一值，须在 <see cref="Initialize"/> 之后调用才生效
        /// </summary>
        public static void SetAllIsLog(bool isLog)
        {
            Debugger.SetAllIsLog(isLog);
        }

        /// <summary>
        /// 启用或禁用对 Unity <c>Application.logMessageReceived</c> 的捕获
        /// </summary>
        public static void EnableUnityLogCapture(bool enable)
        {
            if (s_Manager == null)
                return;

            s_Manager.EnableUnityLogCapture(enable);
        }

        /// <summary>将缓存中的日志立即刷新到文件</summary>
        public static void Flush()
        {
            if (s_Manager == null)
                return;

            s_Manager.Flush();
        }

        /// <summary>刷新并关闭日志系统，释放文件资源</summary>
        public static void Close()
        {
            if (s_Manager == null)
                return;

            s_Manager.Close();
        }

        // ──────────────────────────────────────────
        // Info
        // ──────────────────────────────────────────

        /// <summary>写入 Info 级别日志（字符串快捷入口）</summary>
        public static void Log(string message)
        {
            Debugger.LogInfo(message);
        }

        /// <summary>写入 Info 级别日志</summary>
        public static void LogInfo(object message)
        {
            Debugger.LogInfo(message);
        }

        /// <summary>格式化后写入 Info 级别日志</summary>
        public static void LogInfoF(object message, params object[] args)
        {
            Debugger.LogInfoF(message, args);
        }

        // ──────────────────────────────────────────
        // Debug
        // ──────────────────────────────────────────

        /// <summary>写入 Debug 级别日志</summary>
        public static void LogDebug(object message)
        {
            Debugger.LogDebug(message);
        }

        /// <summary>格式化后写入 Debug 级别日志</summary>
        public static void LogDebugF(object message, params object[] args)
        {
            Debugger.LogDebugF(message, args);
        }

        // ──────────────────────────────────────────
        // Warning
        // ──────────────────────────────────────────

        /// <summary>写入 Warning 级别日志</summary>
        public static void LogWarning(object message)
        {
            Debugger.LogWarning(message);
        }

        /// <summary>格式化后写入 Warning 级别日志</summary>
        public static void LogWarningF(object message, params object[] args)
        {
            Debugger.LogWarningF(message, args);
        }

        // ──────────────────────────────────────────
        // Error
        // ──────────────────────────────────────────

        /// <summary>写入 Error 级别日志</summary>
        public static void LogError(object message)
        {
            Debugger.LogError(message);
        }

        /// <summary>格式化后写入 Error 级别日志</summary>
        public static void LogErrorF(object message, params object[] args)
        {
            Debugger.LogErrorF(message, args);
        }

        // ──────────────────────────────────────────
        // Exception / Fatal
        // ──────────────────────────────────────────

        /// <summary>写入异常日志（含堆栈）</summary>
        public static void LogException(Exception ex)
        {
            Debugger.LogException(ex);
        }

        /// <summary>写入 Fatal 级别日志并触发 <see cref="Debugger.onFatalDelegate"/></summary>
        public static void LogFatal(object message)
        {
            Debugger.LogFatal(message);
        }

        /// <summary>格式化后写入 Fatal 级别日志</summary>
        public static void LogFatalF(object message, params object[] args)
        {
            Debugger.LogFatalF(message, args);
        }

        // ──────────────────────────────────────────
        // Assert
        // ──────────────────────────────────────────

        /// <summary>DEBUG 下条件为 false 时触发 <see cref="Debugger.onAssertFailDelegate"/></summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            Debugger.Assert(condition);
        }

        /// <summary>DEBUG 下带说明的断言失败回调</summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            Debugger.Assert(condition, message);
        }
    }
}
