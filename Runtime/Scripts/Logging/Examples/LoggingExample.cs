using UnityEngine;
using ST.Core.Logging;

namespace ST.Core.Logging.Examples
{
    /// <summary>
    /// 日志系统使用示例
    /// 演示初始化配置、Unity 日志捕获和各级别日志写入的完整流程
    /// </summary>
    public class LoggingExample : MonoBehaviour
    {
        /// <summary>
        /// 组件启动时初始化日志系统并写入各级别示例日志
        /// </summary>
        void Start()
        {
            // 链式配置：缓存 50 条后刷新，单文件最大 10 MB
            var config = new LogConfig()
                .SetMaxFlushCount(50)
                .SetMaxFileSize(10 * 1024 * 1024);

            Logger.Initialize(config);

            // 启用 Unity 日志捕获
            Logger.EnableUnityLogCapture(true);

            // 写入不同级别的日志
            Logger.LogDebug("Debug message");
            Logger.Log("Info message");
            Logger.LogWarning("Warning message");
            Logger.LogError("Error message");

            // Unity 日志也会被捕获并写入文件
            Debug.Log("Unity log message");
            Debug.LogWarning("Unity warning");
            Debug.LogError("Unity error");

            Debug.Log($"Log file path: {config.GetLogFilePath()}");
        }

        /// <summary>
        /// 应用退出前将缓存日志刷新到文件并关闭写入器，防止日志丢失
        /// </summary>
        void OnApplicationQuit()
        {
            Logger.Flush();
            Logger.Close();
        }
    }
}
