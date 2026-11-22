using System;
using System.Diagnostics;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志入口的级别输出部分，按 Info、Debug、Warning、Error、Exception、Fatal 分组提供写日志方法。
    /// </summary>
    public static partial class STLogger
    {
        // ──────────────────────────────────────────
        // Info
        // ──────────────────────────────────────────

        /// <summary>
        /// 写入 Info 级别日志（字符串快捷入口，等同于 <see cref="LogInfo(object)"/>）。
        /// <para>受内部 <c>isLogInfo</c> 开关控制，关闭时静默丢弃。</para>
        /// </summary>
        /// <param name="message">日志消息。</param>
        public static void Log(string message)
        {
            Debugger.LogInfo(message);
        }

        /// <summary>
        /// 写入 Info 级别日志。
        /// <para>受内部 <c>isLogInfo</c> 开关控制，关闭时静默丢弃。</para>
        /// </summary>
        /// <param name="message">任意可转为字符串的对象。</param>
        public static void LogInfo(object message)
        {
            Debugger.LogInfo(message);
        }

        /// <summary>
        /// 使用 <see cref="string.Format(string, object[])"/> 格式化后写入 Info 级别日志。
        /// </summary>
        /// <param name="message">格式字符串。</param>
        /// <param name="args">格式参数。</param>
        public static void LogInfoF(object message, params object[] args)
        {
            Debugger.LogInfoF(message, args);
        }

        // ──────────────────────────────────────────
        // Debug
        // ──────────────────────────────────────────

        /// <summary>
        /// 写入 Debug 级别日志。
        /// <para>受内部 <c>isLogDebug</c> 开关控制，关闭时静默丢弃。</para>
        /// </summary>
        /// <param name="message">任意可转为字符串的对象。</param>
        public static void LogDebug(object message)
        {
            Debugger.LogDebug(message);
        }

        /// <summary>
        /// 使用 <see cref="string.Format(string, object[])"/> 格式化后写入 Debug 级别日志。
        /// </summary>
        /// <param name="message">格式字符串。</param>
        /// <param name="args">格式参数。</param>
        public static void LogDebugF(object message, params object[] args)
        {
            Debugger.LogDebugF(message, args);
        }

        // ──────────────────────────────────────────
        // Warning
        // ──────────────────────────────────────────

        /// <summary>
        /// 写入 Warning 级别日志，对应 <c>UnityEngine.Debug.LogWarning</c>。
        /// <para>受内部 <c>isLogWarning</c> 开关控制，关闭时静默丢弃。</para>
        /// </summary>
        /// <param name="message">任意可转为字符串的对象。</param>
        public static void LogWarning(object message)
        {
            Debugger.LogWarning(message);
        }

        /// <summary>
        /// 使用 <see cref="string.Format(string, object[])"/> 格式化后写入 Warning 级别日志。
        /// </summary>
        /// <param name="message">格式字符串。</param>
        /// <param name="args">格式参数。</param>
        public static void LogWarningF(object message, params object[] args)
        {
            Debugger.LogWarningF(message, args);
        }

        // ──────────────────────────────────────────
        // Error
        // ──────────────────────────────────────────

        /// <summary>
        /// 写入 Error 级别日志，对应 <c>UnityEngine.Debug.LogError</c>。
        /// <para>受内部 <c>isLogError</c> 开关控制，关闭时静默丢弃。</para>
        /// </summary>
        /// <param name="message">任意可转为字符串的对象。</param>
        public static void LogError(object message)
        {
            Debugger.LogError(message);
        }

        /// <summary>
        /// 使用 <see cref="string.Format(string, object[])"/> 格式化后写入 Error 级别日志。
        /// </summary>
        /// <param name="message">格式字符串。</param>
        /// <param name="args">格式参数。</param>
        public static void LogErrorF(object message, params object[] args)
        {
            Debugger.LogErrorF(message, args);
        }

        // ──────────────────────────────────────────
        // Exception / Fatal
        // ──────────────────────────────────────────

        /// <summary>
        /// 写入异常日志，对应 <c>UnityEngine.Debug.LogException</c>，自动包含完整堆栈。
        /// <para>受内部 <c>isLogException</c> 开关控制，关闭时静默丢弃。</para>
        /// </summary>
        /// <param name="ex">需要记录的异常对象。</param>
        public static void LogException(Exception ex)
        {
            Debugger.LogException(ex);
        }

        /// <summary>
        /// 写入 Fatal 级别日志。
        /// <para>内部调用 <c>UnityEngine.Debug.LogError</c> 输出，并在输出后触发业务层注册的致命错误回调（如弹窗、上报等）。</para>
        /// <para>受内部 <c>isLogException</c> 开关控制，关闭时静默丢弃。</para>
        /// </summary>
        /// <param name="message">任意可转为字符串的对象。</param>
        public static void LogFatal(object message)
        {
            Debugger.LogFatal(message);
        }

        /// <summary>
        /// 使用 <see cref="string.Format(string, object[])"/> 格式化后写入 Fatal 级别日志。
        /// </summary>
        /// <param name="message">格式字符串。</param>
        /// <param name="args">格式参数。</param>
        public static void LogFatalF(object message, params object[] args)
        {
            Debugger.LogFatalF(message, args);
        }

        // ──────────────────────────────────────────
        // Assert
        // ──────────────────────────────────────────

        /// <summary>
        /// 仅在 DEBUG 编译符号存在时有效。
        /// 当 <paramref name="condition"/> 为 <c>false</c> 时触发业务层注册的断言失败回调。
        /// Release 模式下此方法调用会被编译器完全消除，无任何运行时开销。
        /// </summary>
        /// <param name="condition">断言条件，为 <c>false</c> 时触发回调。</param>
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            Debugger.Assert(condition);
        }

        /// <summary>
        /// 仅在 DEBUG 编译符号存在时有效。
        /// 当 <paramref name="condition"/> 为 <c>false</c> 时，携带说明信息触发断言失败回调。
        /// Release 模式下此方法调用会被编译器完全消除，无任何运行时开销。
        /// </summary>
        /// <param name="condition">断言条件，为 <c>false</c> 时触发回调。</param>
        /// <param name="message">断言失败时传入回调的说明文本。</param>
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            Debugger.Assert(condition, message);
        }
    }
}