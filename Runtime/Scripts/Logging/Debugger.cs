using System;
using System.Diagnostics;
using UnityEngine;


namespace ST.Core.Logging
{
    /// <summary>
    /// 调试日志门面：在 Unity 控制台与可选标准输出之间切换，并支持致命与断言失败回调。
    /// 使用前须调用 <see cref="Initialize"/> 完成初始化。
    /// </summary>
    internal class Debugger
    {
        /// <summary>致命错误时由业务注册的额外处理（如弹窗、上报）。</summary>
        /// <param name="message">致命信息文本</param>
        public delegate void OnFatalDelegate(string message);

        /// <summary>DEBUG 断言失败时由业务注册的处理。</summary>
        /// <param name="message">断言说明</param>
        public delegate void OnAssertFailDelegate(string message);

        /// <summary>是否输出异常日志。</summary>
        public static bool isLogException = true;

        /// <summary>是否输出 Error 级别日志。</summary>
        public static bool isLogError = true;

        /// <summary>是否输出 Info 级别日志。</summary>
        public static bool isLogInfo = true;

        /// <summary>是否输出 Warning 级别日志。</summary>
        public static bool isLogWarning = true;

        /// <summary>是否输出 Debug 级别日志。</summary>
        public static bool isLogDebug = true;

        /// <summary>致命日志附加回调，可为 null。</summary>
        public static OnFatalDelegate onFatalDelegate { get; set; }

        /// <summary>断言失败回调，可为 null。</summary>
        public static OnAssertFailDelegate onAssertFailDelegate { get; set; }

        /// <summary>为 true 时同步写入标准输出（适用于无头或服务器环境）。</summary>
        public static bool writeToConsole { get; set; }

        /// <summary>
        /// 将所有级别开关设为同一值。须在 <see cref="Initialize"/> 之后调用才生效。
        /// </summary>
        /// <param name="isLog">true 表示全部打开，false 表示全部关闭</param>
        public static void SetAllIsLog(bool isLog)
        {
            isLogException = isLog;
            isLogError = isLog;
            isLogInfo = isLog;
            isLogWarning = isLog;
            isLogDebug = isLog;
        }

        /// <summary>在 <see cref="isLogDebug"/> 为 true 时输出 Unity Debug 日志。</summary>
        /// <param name="message">任意可转字符串的对象</param>
        public static void LogDebug(object message)
        {
            if (isLogDebug)
            {
                if (writeToConsole)
                {
                    Console.WriteLine(message.ToString());
                }

                UnityEngine.Debug.Log(message);
            }
        }

        /// <summary>使用 <see cref="string.Format(string, object[])"/> 格式化后调用 <see cref="LogDebug"/>。</summary>
        public static void LogDebugF(object message, params object[] args)
        {
            if (isLogDebug)
            {
                LogDebug(string.Format(message.ToString(), args));
            }
        }

        /// <summary>在 <see cref="isLogInfo"/> 为 true 时输出 Unity 普通日志。</summary>
        public static void LogInfo(object message)
        {
            if (isLogInfo)
            {
                if (writeToConsole)
                {
                    Console.WriteLine(message.ToString());
                }

                UnityEngine.Debug.Log(message);
            }
        }

        /// <summary>格式化后调用 <see cref="LogInfo"/>。</summary>
        public static void LogInfoF(object message, params object[] args)
        {
            if (isLogInfo)
            {
                LogInfo(string.Format(message.ToString(), args));
            }
        }

        /// <summary>在 <see cref="isLogWarning"/> 为 true 时输出 Unity 警告日志。</summary>
        public static void LogWarning(object message)
        {
            if (isLogWarning)
            {
                if (writeToConsole)
                {
                    Console.WriteLine(message.ToString());
                }

                UnityEngine.Debug.LogWarning(message);
            }
        }

        /// <summary>格式化后调用 <see cref="LogWarning"/>。</summary>
        public static void LogWarningF(object message, params object[] args)
        {
            if (isLogWarning)
            {
                LogWarning(string.Format(message.ToString(), args));
            }
        }

        /// <summary>在 <see cref="isLogError"/> 为 true 时输出 Unity 错误日志。</summary>
        public static void LogError(object message)
        {
            if (isLogError)
            {
                if (writeToConsole)
                {
                    Console.WriteLine(message.ToString());
                }

                UnityEngine.Debug.LogError(message);
            }
        }

        /// <summary>格式化后调用 <see cref="LogError"/>。</summary>
        public static void LogErrorF(object message, params object[] args)
        {
            if (isLogError)
            {
                LogError(string.Format(message.ToString(), args));
            }
        }

        /// <summary>在 <see cref="isLogException"/> 为 true 时输出 Unity 异常（含堆栈）。</summary>
        public static void LogException(Exception ex)
        {
            if (isLogException)
            {
                if (writeToConsole)
                {
                    Console.WriteLine(ex.Message);
                }

                UnityEngine.Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 记录错误级别日志并可选调用 <see cref="onFatalDelegate"/>（受 <see cref="isLogException"/> 控制）。
        /// </summary>
        public static void LogFatal(object message)
        {
            if (isLogException)
            {
                if (writeToConsole)
                {
                    Console.WriteLine(message.ToString());
                }

                UnityEngine.Debug.LogError(message);

                if (onFatalDelegate != null)
                {
                    onFatalDelegate(message.ToString());
                }
            }
        }

        /// <summary>格式化后调用 <see cref="LogFatal"/>。</summary>
        public static void LogFatalF(object message, params object[] args)
        {
            if (isLogException)
            {
                LogFatal(string.Format(message.ToString(), args));
            }
        }

        /// <summary>DEBUG 下条件为 false 时触发 <see cref="onAssertFailDelegate"/>。</summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition && onAssertFailDelegate != null)
            {
                onAssertFailDelegate(string.Empty);
            }
        }

        /// <summary>DEBUG 下带说明的断言失败回调。</summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (!condition && onAssertFailDelegate != null)
            {
                if (string.IsNullOrEmpty(message))
                {
                    onAssertFailDelegate(string.Empty);
                }
                else
                {
                    onAssertFailDelegate(message);
                }
            }
        }
    }
}

