using UnityEngine;
using ST.Core.Logging;

namespace ST.Core.Logging.Examples
{
    /// <summary>
    /// 日志系统使用示例
    /// </summary>
    public class LoggingExample : MonoBehaviour
    {
        void Start()
        {
            // 初始化日志系统
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

            // Unity 日志也会被捕获
            Debug.Log("Unity log message");
            Debug.LogWarning("Unity warning");
            Debug.LogError("Unity error");

            Debug.Log($"Log file path: {config.GetLogFilePath()}");
        }

        void OnApplicationQuit()
        {
            // 应用退出时刷新并关闭
            Logger.Flush();
            Logger.Close();
        }
    }
}