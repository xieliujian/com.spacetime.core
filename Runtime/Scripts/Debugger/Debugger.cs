using System;
using System.Diagnostics;
using UnityEngine;


namespace ST.Core.Debugger
{
    /// <summary>
    /// 
    /// </summary>
    public class Debugger
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public delegate void OnFatalDelegate(string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public delegate void OnAssertFailDelegate(string message);

        /// <summary>
        /// 
        /// </summary>
        public static bool isLogException = true;

        /// <summary>
        /// 
        /// </summary>
        public static bool isLogError = true;

        /// <summary>
        /// 
        /// </summary>
        public static bool isLogInfo = true;

        /// <summary>
        /// 
        /// </summary>
        public static bool isLogWarning = true;

        /// <summary>
        /// 
        /// </summary>
        public static bool isLogDebug = true;

        /// <summary>
        /// 
        /// </summary>
        public static OnFatalDelegate onFatalDelegate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static OnAssertFailDelegate onAssertFailDelegate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static bool writeToConsole { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isLog"></param>
        public static void SetAllIsLog(bool isLog)
        {
            isLogException = isLog;
            isLogError = isLog;
            isLogInfo = isLog;
            isLogWarning = isLog;
            isLogDebug = isLog;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogDebugF(object message, params object[] args)
        {
            if (isLogDebug)
            {
                LogDebug(string.Format(message.ToString(), args));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogInfoF(object message, params object[] args)
        {
            if (isLogInfo)
            {
                LogInfo(string.Format(message.ToString(), args));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogWarningF(object message, params object[] args)
        {
            if (isLogWarning)
            {
                LogWarning(string.Format(message.ToString(), args));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogErrorF(object message, params object[] args)
        {
            if (isLogError)
            {
                LogError(string.Format(message.ToString(), args));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
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
        /// 
        /// </summary>
        /// <param name="message"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogFatalF(object message, params object[] args)
        {
            if (isLogException)
            {
                LogFatal(string.Format(message.ToString(), args));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="condition"></param>
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition && onAssertFailDelegate != null)
            {
                onAssertFailDelegate(string.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
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

