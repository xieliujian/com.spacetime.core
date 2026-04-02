using System;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器，协调 <see cref="FileLogWriter"/> 和 <see cref="DefaultLogFormatter"/>，并管理 Unity 日志捕获
    /// </summary>
    public class LogManager
    {
        /// <summary>日志写入器，负责将格式化后的日志持久化</summary>
        FileLogWriter m_Writer;

        /// <summary>日志格式化器，负责将日志内容转换为字符串</summary>
        DefaultLogFormatter m_Formatter;

        /// <summary>日志配置，保存路径、文件大小等参数</summary>
        LogConfig m_Config;

        /// <summary>标记当前是否已注册 Unity 日志回调，避免重复订阅</summary>
        bool m_UnityLogCaptureEnabled;

        /// <summary>
        /// 根据配置初始化格式化器和文件写入器
        /// </summary>
        /// <param name="config">日志配置</param>
        public void Initialize(LogConfig config)
        {
            m_Config = config;
            m_Formatter = new DefaultLogFormatter();
            m_Writer = new FileLogWriter(
                config.GetLogFilePath(),
                config.GetMaxFlushCount(),
                config.GetMaxFileSize(),
                config.EnableBackup()
            );
        }

        /// <summary>
        /// 启用或禁用对 Unity <c>Application.logMessageReceived</c> 事件的监听
        /// 重复设置相同状态时不做任何处理
        /// </summary>
        /// <param name="enable">true 表示开始监听，false 表示停止监听</param>
        public void EnableUnityLogCapture(bool enable)
        {
            if (m_UnityLogCaptureEnabled == enable)
                return;

            m_UnityLogCaptureEnabled = enable;

            if (enable)
            {
                Application.logMessageReceived += OnUnityLogCallback;
            }
            else
            {
                Application.logMessageReceived -= OnUnityLogCallback;
            }
        }

        /// <summary>
        /// 将缓存中的日志立即写入文件
        /// </summary>
        public void Flush()
        {
            if (m_Writer == null)
                return;

            m_Writer.Flush();
        }

        /// <summary>
        /// 停止 Unity 日志捕获并关闭写入器，释放文件句柄
        /// </summary>
        public void Close()
        {
            EnableUnityLogCapture(false);
            if (m_Writer == null)
                return;

            m_Writer.Close();
        }

        /// <summary>
        /// Unity <c>Application.logMessageReceived</c> 回调，将 Unity 日志转发到日志系统
        /// </summary>
        /// <param name="condition">日志消息内容</param>
        /// <param name="stackTrace">堆栈跟踪</param>
        /// <param name="type">Unity 日志类型</param>
        void OnUnityLogCallback(string condition, string stackTrace, LogType type)
        {
            LogLevel level = ConvertUnityLogType(type);
            Log(level, condition, stackTrace);
        }

        /// <summary>
        /// 格式化并写入一条日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="stackTrace">堆栈跟踪（可选）</param>
        void Log(LogLevel level, string message, string stackTrace = null)
        {
            if (m_Writer == null)
                return;

            string formatted = m_Formatter.Format(level, message, stackTrace, DateTime.Now);
            m_Writer.Write(formatted);
        }

        /// <summary>
        /// 将 Unity <see cref="LogType"/> 转换为 <see cref="LogLevel"/>
        /// </summary>
        /// <param name="type">Unity 日志类型</param>
        /// <returns>对应的日志级别</returns>
        LogLevel ConvertUnityLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    return LogLevel.Error;
                case LogType.Exception:
                    return LogLevel.Exception;
                case LogType.Warning:
                    return LogLevel.Warning;
                case LogType.Assert:
                    return LogLevel.Error;
                default:
                    return LogLevel.Info;
            }
        }
    }
}
