using System;
using System.Diagnostics;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志系统的唯一对外入口（静态门面）。
    /// <para>
    /// 调用链路：<br/>
    /// <c>Logger.LogXxx()</c> → 内部 <c>Debugger</c>（私有嵌套类）→ <c>UnityEngine.Debug.LogXxx</c><br/>
    /// → <c>Application.logMessageReceived</c> 事件 → <see cref="LogManager"/> → 写入文件
    /// </para>
    /// <para>
    /// 使用步骤：<br/>
    /// 1. 调用 <see cref="Initialize"/> 传入配置，初始化文件写入系统；<br/>
    /// 2. 调用 <see cref="EnableUnityLogCapture"/> 开启 Unity 日志捕获，日志自动落地到文件；<br/>
    /// 3. 使用 <see cref="Log"/>、<see cref="LogWarning"/>、<see cref="LogError"/> 等方法输出日志；<br/>
    /// 4. 程序退出前调用 <see cref="Close"/> 刷新并释放文件资源。
    /// </para>
    /// </summary>
    public static class Logger
    {
        /// <summary>日志管理器实例，负责文件写入，由 <see cref="Initialize"/> 赋值。</summary>
        static LogManager s_Manager;

        // ──────────────────────────────────────────
        // 初始化 / 文件系统
        // ──────────────────────────────────────────

        /// <summary>
        /// 初始化日志文件系统。
        /// <para>若管理器尚未创建则自动构造默认的 <see cref="LogManager"/>，随后使用 <paramref name="config"/> 完成配置。</para>
        /// <para>可重复调用以重新配置（如切换日志路径），不会重复创建管理器实例。</para>
        /// </summary>
        /// <param name="config">日志配置，包含路径、文件大小上限、备份策略等参数。</param>
        public static void Initialize(LogConfig config)
        {
            if (s_Manager == null)
            {
                s_Manager = new LogManager();
            }
            s_Manager.Initialize(config);
        }

        /// <summary>
        /// 将所有日志级别开关（Info / Debug / Warning / Error / Exception）统一设为同一值。
        /// <para>须在 <see cref="Initialize"/> 之后调用才能生效。</para>
        /// </summary>
        /// <param name="isLog"><c>true</c> 表示全部开启，<c>false</c> 表示全部关闭。</param>
        public static void SetAllIsLog(bool isLog)
        {
            Debugger.SetAllIsLog(isLog);
        }

        /// <summary>
        /// 启用或禁用对 Unity <c>Application.logMessageReceived</c> 事件的监听。
        /// <para>
        /// 启用后，所有经由 <c>UnityEngine.Debug</c> 输出的日志（包括本类所有 LogXxx 方法）
        /// 均会被 <see cref="LogManager"/> 捕获并写入文件。
        /// </para>
        /// <para>重复调用相同值时不做任何处理，防止重复订阅。</para>
        /// </summary>
        /// <param name="enable"><c>true</c> 表示开始监听，<c>false</c> 表示停止监听。</param>
        public static void EnableUnityLogCapture(bool enable)
        {
            if (s_Manager == null)
                return;

            s_Manager.EnableUnityLogCapture(enable);
        }

        /// <summary>
        /// 将缓冲区中尚未写入磁盘的日志立即刷新到文件。
        /// <para>在程序异常退出前主动调用可防止日志丢失。</para>
        /// </summary>
        public static void Flush()
        {
            if (s_Manager == null)
                return;

            s_Manager.Flush();
        }

        /// <summary>
        /// 停止 Unity 日志捕获，刷新剩余缓冲并关闭文件句柄，释放所有文件资源。
        /// <para>程序正常退出时应调用此方法，确保日志完整落盘。</para>
        /// </summary>
        public static void Close()
        {
            if (s_Manager == null)
                return;

            s_Manager.Close();
        }

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

        // ══════════════════════════════════════════
        // 私有嵌套实现类，仅 Logger 可访问
        // ══════════════════════════════════════════

        /// <summary>
        /// 日志输出核心实现，封装 Unity 控制台输出与级别开关逻辑。
        /// 此类为 Logger 的私有嵌套类，包内及包外任何其他代码均不可直接访问。
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
