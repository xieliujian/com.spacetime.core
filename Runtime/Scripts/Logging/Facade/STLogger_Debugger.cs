using System;
using System.Diagnostics;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志入口的内部实现部分，封装级别开关与 Unity 控制台输出。
    /// </summary>
    public static partial class STLogger
    {
        // ══════════════════════════════════════════
        // 私有嵌套实现类，仅 STLogger 可访问
        // ══════════════════════════════════════════

        /// <summary>
        /// 日志输出核心实现，封装 Unity 控制台输出与级别开关逻辑。
        /// 此类为 STLogger 的私有嵌套类，包内及包外任何其他代码均不可直接访问。
        /// </summary>
        static class Debugger
        {
            /// <summary>Fatal 日志输出后由业务层注册的额外处理委托（如弹窗、崩溃上报）。</summary>
            /// <param name="message">致命信息文本。</param>
            public delegate void OnFatalDelegate(string message);

            /// <summary>DEBUG 断言失败时由业务层注册的处理委托。</summary>
            /// <param name="message">断言说明，可为空字符串。</param>
            public delegate void OnAssertFailDelegate(string message);

            /// <summary>是否输出 Exception / Fatal 级别日志，默认 <c>true</c>。</summary>
            public static bool isLogException = true;

            /// <summary>是否输出 Error 级别日志，默认 <c>true</c>。</summary>
            public static bool isLogError = true;

            /// <summary>是否输出 Info 级别日志，默认 <c>true</c>。</summary>
            public static bool isLogInfo = true;

            /// <summary>是否输出 Warning 级别日志，默认 <c>true</c>。</summary>
            public static bool isLogWarning = true;

            /// <summary>是否输出 Debug 级别日志，默认 <c>true</c>。</summary>
            public static bool isLogDebug = true;

            /// <summary>Fatal 日志输出后触发的回调，可为 <c>null</c>。</summary>
            public static OnFatalDelegate onFatalDelegate { get; set; }

            /// <summary>断言失败时触发的回调，可为 <c>null</c>。</summary>
            public static OnAssertFailDelegate onAssertFailDelegate { get; set; }

            /// <summary>
            /// 为 <c>true</c> 时同步将日志写入标准输出（<c>Console.WriteLine</c>），
            /// 适用于无头模式或服务器环境。默认 <c>false</c>。
            /// </summary>
            public static bool writeToConsole { get; set; }

            /// <summary>将所有级别开关统一设为 <paramref name="isLog"/>。</summary>
            public static void SetAllIsLog(bool isLog)
            {
                isLogException = isLog;
                isLogError = isLog;
                isLogInfo = isLog;
                isLogWarning = isLog;
                isLogDebug = isLog;
            }

            /// <summary>在 <see cref="isLogDebug"/> 为 <c>true</c> 时输出 Debug 日志。</summary>
            public static void LogDebug(object message)
            {
                if (!isLogDebug)
                    return;

                if (writeToConsole)
                    Console.WriteLine(message.ToString());

                UnityEngine.Debug.Log(message);
            }

            /// <summary>格式化后调用 <see cref="LogDebug"/>。</summary>
            public static void LogDebugF(object message, params object[] args)
            {
                if (isLogDebug)
                    LogDebug(string.Format(message.ToString(), args));
            }

            /// <summary>在 <see cref="isLogInfo"/> 为 <c>true</c> 时输出 Info 日志。</summary>
            public static void LogInfo(object message)
            {
                if (!isLogInfo)
                    return;

                if (writeToConsole)
                    Console.WriteLine(message.ToString());

                UnityEngine.Debug.Log(message);
            }

            /// <summary>格式化后调用 <see cref="LogInfo"/>。</summary>
            public static void LogInfoF(object message, params object[] args)
            {
                if (isLogInfo)
                    LogInfo(string.Format(message.ToString(), args));
            }

            /// <summary>在 <see cref="isLogWarning"/> 为 <c>true</c> 时输出 Warning 日志。</summary>
            public static void LogWarning(object message)
            {
                if (!isLogWarning)
                    return;

                if (writeToConsole)
                    Console.WriteLine(message.ToString());

                UnityEngine.Debug.LogWarning(message);
            }

            /// <summary>格式化后调用 <see cref="LogWarning"/>。</summary>
            public static void LogWarningF(object message, params object[] args)
            {
                if (isLogWarning)
                    LogWarning(string.Format(message.ToString(), args));
            }

            /// <summary>在 <see cref="isLogError"/> 为 <c>true</c> 时输出 Error 日志。</summary>
            public static void LogError(object message)
            {
                if (!isLogError)
                    return;

                if (writeToConsole)
                    Console.WriteLine(message.ToString());

                UnityEngine.Debug.LogError(message);
            }

            /// <summary>格式化后调用 <see cref="LogError"/>。</summary>
            public static void LogErrorF(object message, params object[] args)
            {
                if (isLogError)
                    LogError(string.Format(message.ToString(), args));
            }

            /// <summary>
            /// 在 <see cref="isLogException"/> 为 <c>true</c> 时输出异常日志（含完整堆栈）。
            /// </summary>
            public static void LogException(Exception ex)
            {
                if (!isLogException)
                    return;

                if (writeToConsole)
                    Console.WriteLine(ex.Message);

                UnityEngine.Debug.LogException(ex);
            }

            /// <summary>
            /// 在 <see cref="isLogException"/> 为 <c>true</c> 时输出 Fatal 日志，
            /// 并在输出后调用 <see cref="onFatalDelegate"/>（若已注册）。
            /// </summary>
            public static void LogFatal(object message)
            {
                if (!isLogException)
                    return;

                if (writeToConsole)
                    Console.WriteLine(message.ToString());

                UnityEngine.Debug.LogError(message);
                onFatalDelegate?.Invoke(message.ToString());
            }

            /// <summary>格式化后调用 <see cref="LogFatal"/>。</summary>
            public static void LogFatalF(object message, params object[] args)
            {
                if (isLogException)
                    LogFatal(string.Format(message.ToString(), args));
            }

            /// <summary>
            /// 仅 DEBUG 模式有效。<paramref name="condition"/> 为 <c>false</c> 时
            /// 调用 <see cref="onAssertFailDelegate"/>（若已注册），传入空字符串。
            /// </summary>
            [Conditional("DEBUG")]
            public static void Assert(bool condition)
            {
                if (!condition)
                    onAssertFailDelegate?.Invoke(string.Empty);
            }

            /// <summary>
            /// 仅 DEBUG 模式有效。<paramref name="condition"/> 为 <c>false</c> 时
            /// 调用 <see cref="onAssertFailDelegate"/>（若已注册），传入 <paramref name="message"/>。
            /// </summary>
            [Conditional("DEBUG")]
            public static void Assert(bool condition, string message)
            {
                if (!condition)
                    onAssertFailDelegate?.Invoke(string.IsNullOrEmpty(message) ? string.Empty : message);
            }
        }
    }
}