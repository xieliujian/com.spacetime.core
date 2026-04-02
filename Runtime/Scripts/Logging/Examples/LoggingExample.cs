using System;
using UnityEngine;
using ST.Core.Logging;

namespace ST.Core.Logging.Examples
{
    /// <summary>
    /// 日志系统完整测试示例。
    /// <para>
    /// 本地测试日志文件保存路径：<c>D:\xieliujian\com.spacetime.core\Logs\Output.txt</c>
    /// </para>
    /// <para>
    /// 覆盖范围：初始化、Unity 日志捕获、各级别输出、格式化输出、Fatal 回调、Assert、Flush / Close。
    /// </para>
    /// </summary>
    public class LoggingExample : MonoBehaviour
    {
        /// <summary>
        /// 本地测试日志文件保存目录。
        /// <c>Application.dataPath</c> 指向工程 Assets 文件夹，
        /// 其父目录即工程根目录，日志保存在工程根目录下的 Logs 文件夹。
        /// </summary>
        static string k_LogDir => System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(Application.dataPath), "Logs");

        void Start()
        {
            InitLogger();
            TestAllLogLevels();
            TestFormatLog();
            TestException();
            TestFatal();
            TestAssert();
        }

        void OnApplicationQuit()
        {
            Logger.Flush();
            Logger.Close();
        }

        // ──────────────────────────────────────────
        // 初始化
        // ──────────────────────────────────────────

        static void InitLogger()
        {
            var logFilePath = System.IO.Path.Combine(k_LogDir, "Output.txt");

            var config = new LogConfig()
                .SetLogFilePath(logFilePath)
                .SetMaxFlushCount(10)
                .SetMaxFileSize(10 * 1024 * 1024)
                .SetEnableBackup(true);

            Logger.Initialize(config);

            Logger.EnableUnityLogCapture(true);

            Logger.Log($"=== Logger 初始化完成，日志路径：{logFilePath} ===");
        }

        // ──────────────────────────────────────────
        // 各级别基础输出
        // ──────────────────────────────────────────

        static void TestAllLogLevels()
        {
            Logger.Log("--- TestAllLogLevels ---");

            Logger.Log("Log (Info 快捷入口)");
            Logger.LogInfo("LogInfo：普通信息");
            Logger.LogDebug("LogDebug：调试信息");
            Logger.LogWarning("LogWarning：警告信息");
            Logger.LogError("LogError：错误信息");
        }

        // ──────────────────────────────────────────
        // 格式化输出
        // ──────────────────────────────────────────

        static void TestFormatLog()
        {
            Logger.Log("--- TestFormatLog ---");

            Logger.LogInfoF("玩家 {0} 进入房间 {1}", "Hero", 42);
            Logger.LogDebugF("坐标：({0:F2}, {1:F2})", 1.5f, 3.14f);
            Logger.LogWarningF("重试 {0}/{1}", 2, 5);
            Logger.LogErrorF("请求 {0} 失败，错误码 {1}", "/api/login", 500);
            Logger.LogFatalF("模块 {0} 崩溃，原因：{1}", "NetworkManager", "Timeout");
        }

        // ──────────────────────────────────────────
        // 异常日志
        // ──────────────────────────────────────────

        static void TestException()
        {
            Logger.Log("--- TestException ---");

            try
            {
                int[] arr = new int[3];
                int _ = arr[10];
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // ──────────────────────────────────────────
        // Fatal 日志 + 回调注册
        // ──────────────────────────────────────────

        static void TestFatal()
        {
            Logger.Log("--- TestFatal ---");

            Logger.LogFatal("严重错误：存档系统崩溃");
        }

        // ──────────────────────────────────────────
        // Assert（仅 DEBUG 模式生效）
        // ──────────────────────────────────────────

        static void TestAssert()
        {
            Logger.Log("--- TestAssert ---");

            Logger.Assert(1 + 1 == 2, "基本算术断言，应通过");
            Logger.Assert(false, "此断言故意失败，仅 DEBUG 下触发回调");
            Logger.Assert(true);
        }
    }
}
