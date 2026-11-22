using System;
using UnityEngine;
using ST.Core.Logging;

namespace ST.Core.Logging.Examples
{
    /// <summary>
    /// 日志系统完整测试示例，覆盖初始化、Unity 日志捕获、各级别输出、格式化输出、Fatal 回调、Assert 与 Flush / Close。
    /// <para>日志写入工程根目录下的 <c>Logs/Output.txt</c>。</para>
    /// </summary>
    public class LogExample : MonoBehaviour
    {
        /// <summary>
        /// 示例日志文件保存目录。
        /// <c>Application.dataPath</c> 指向工程 Assets 文件夹，
        /// 其父目录即工程根目录，日志保存在工程根目录下的 Logs 文件夹。
        /// </summary>
        static string k_LogDir => System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(Application.dataPath), "Logs");

        /// <summary>运行示例：初始化日志系统并依次执行各级别测试。</summary>
        void Start()
        {
            InitLogger();
            TestAllLogLevels();
            TestFormatLog();
            TestException();
            TestFatal();
            TestAssert();
        }

        /// <summary>退出前刷新并关闭日志系统，确保文件完整落盘。</summary>
        void OnApplicationQuit()
        {
            STLogger.Flush();
            STLogger.Close();
        }

        // ──────────────────────────────────────────
        // 初始化
        // ──────────────────────────────────────────

        /// <summary>创建配置、初始化 STLogger 并开启 Unity 日志捕获。</summary>
        static void InitLogger()
        {
            var logFilePath = System.IO.Path.Combine(k_LogDir, "Output.txt");

            var config = new LogConfig()
                .SetLogFilePath(logFilePath)
                .SetMaxFlushCount(10)
                .SetMaxFileSize(10 * 1024 * 1024)
                .SetEnableBackup(true);

            STLogger.Initialize(config);

            STLogger.EnableUnityLogCapture(true);

            STLogger.Log($"=== STLogger 初始化完成，日志路径：{logFilePath} ===");
        }

        // ──────────────────────────────────────────
        // 各级别基础输出
        // ──────────────────────────────────────────

        /// <summary>按级别依次输出普通日志。</summary>
        static void TestAllLogLevels()
        {
            STLogger.Log("--- TestAllLogLevels ---");

            STLogger.Log("Log (Info 快捷入口)");
            STLogger.LogInfo("LogInfo：普通信息");
            STLogger.LogDebug("LogDebug：调试信息");
            STLogger.LogWarning("LogWarning：警告信息");
            STLogger.LogError("LogError：错误信息");
        }

        // ──────────────────────────────────────────
        // 格式化输出
        // ──────────────────────────────────────────

        /// <summary>输出各格式化重载的示例。</summary>
        static void TestFormatLog()
        {
            STLogger.Log("--- TestFormatLog ---");

            STLogger.LogInfoF("玩家 {0} 进入房间 {1}", "Hero", 42);
            STLogger.LogDebugF("坐标：({0:F2}, {1:F2})", 1.5f, 3.14f);
            STLogger.LogWarningF("重试 {0}/{1}", 2, 5);
            STLogger.LogErrorF("请求 {0} 失败，错误码 {1}", "/api/login", 500);
            STLogger.LogFatalF("模块 {0} 崩溃，原因：{1}", "NetworkManager", "Timeout");
        }

        // ──────────────────────────────────────────
        // 异常日志
        // ──────────────────────────────────────────

        /// <summary>构造并记录一个数组越界异常。</summary>
        static void TestException()
        {
            STLogger.Log("--- TestException ---");

            try
            {
                int[] arr = new int[3];
                int _ = arr[10];
            }
            catch (Exception ex)
            {
                STLogger.LogException(ex);
            }
        }

        // ──────────────────────────────────────────
        // Fatal 日志 + 回调注册
        // ──────────────────────────────────────────

        /// <summary>输出 Fatal 日志，并在输出后触发业务层注册的回调。</summary>
        static void TestFatal()
        {
            STLogger.Log("--- TestFatal ---");

            STLogger.LogFatal("严重错误：存档系统崩溃");
        }

        // ──────────────────────────────────────────
        // Assert（仅 DEBUG 模式生效）
        // ──────────────────────────────────────────

        /// <summary>演示通过与失败的断言，回调只在 DEBUG 构建下触发。</summary>
        static void TestAssert()
        {
            STLogger.Log("--- TestAssert ---");

            STLogger.Assert(1 + 1 == 2, "基本算术断言，应通过");
            STLogger.Assert(false, "此断言故意失败，仅 DEBUG 下触发回调");
            STLogger.Assert(true);
        }
    }
}
