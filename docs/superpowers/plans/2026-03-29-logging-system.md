# 日志系统实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现一个解耦、可扩展的文件日志系统，支持批量刷新、文件轮转、Unity 日志捕获

**Architecture:** 分层架构 - Logger 静态门面 → LogManager 管理器 → FileLogWriter/DefaultLogFormatter 实现层，通过接口抽象外部依赖

**Tech Stack:** C#, Unity Engine, System.IO

---

## 文件结构规划

```
Runtime/Scripts/Logging/
├── Enums/
│   └── LogLevel.cs                  // 日志级别枚举
├── Interfaces/
│   ├── ILogConfig.cs                // 配置接口
│   ├── ILogFormatter.cs             // 格式化器接口
│   ├── ILogWriter.cs                // 写入器接口
│   └── ILogManager.cs               // 管理器接口
├── DefaultLogFormatter.cs           // 默认格式化器实现
├── FileLogWriter.cs                 // 文件写入器实现
├── LogConfig.cs                     // 默认配置实现
├── LogManager.cs                    // 管理器实现
└── Logger.cs                        // 静态门面
```

**设计原则：**
- 每个文件单一职责
- 接口与实现分离
- 先定义接口，再实现具体类
- 遵循 TDD 原则

---

## Task 1: 创建日志级别枚举

**Files:**
- Create: `Runtime/Scripts/Logging/Enums/LogLevel.cs`
- Create: `Runtime/Scripts/Logging/Enums/LogLevel.cs.meta`

- [ ] **Step 1: 创建目录结构**

```bash
mkdir -p "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Runtime\Scripts\Logging\Enums"
```

- [ ] **Step 2: 创建 LogLevel.cs**

```csharp
namespace ST.Core.Logging
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Exception = 4
    }
}
```

- [ ] **Step 3: 创建 .meta 文件**

Unity 会自动生成，或手动创建 GUID

- [ ] **Step 4: 验证编译**

在 Unity Editor 中检查是否有编译错误

- [ ] **Step 5: 提交**

```bash
cd "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core"
git add Runtime/Scripts/Logging/Enums/
git commit -m "feat(logging): add LogLevel enum"
```

---

## Task 2: 创建配置接口

**Files:**
- Create: `Runtime/Scripts/Logging/Interfaces/ILogConfig.cs`

- [ ] **Step 1: 创建目录**

```bash
mkdir -p "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Runtime\Scripts\Logging\Interfaces"
```

- [ ] **Step 2: 创建 ILogConfig.cs**

```csharp
namespace ST.Core.Logging
{
    /// <summary>
    /// 日志配置接口
    /// 抽象外部依赖（FilePath、DeviceModule 等）
    /// </summary>
    public interface ILogConfig
    {
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        string GetLogFilePath();

        /// <summary>
        /// 是否为低内存设备
        /// </summary>
        bool IsLowMemoryDevice();

        /// <summary>
        /// 获取最大刷新数量
        /// </summary>
        int GetMaxFlushCount();

        /// <summary>
        /// 获取最大文件大小（字节）
        /// </summary>
        long GetMaxFileSize();

        /// <summary>
        /// 是否启用备份
        /// </summary>
        bool EnableBackup();
    }
}
```

- [ ] **Step 3: 验证编译**

- [ ] **Step 4: 提交**

```bash
git add Runtime/Scripts/Logging/Interfaces/ILogConfig.cs
git commit -m "feat(logging): add ILogConfig interface"
```

---

## Task 3: 创建格式化器和写入器接口

**Files:**
- Create: `Runtime/Scripts/Logging/Interfaces/ILogFormatter.cs`
- Create: `Runtime/Scripts/Logging/Interfaces/ILogWriter.cs`

- [ ] **Step 1: 创建 ILogFormatter.cs**

```csharp
using System;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志格式化器接口
    /// </summary>
    public interface ILogFormatter
    {
        /// <summary>
        /// 格式化日志消息
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">消息内容</param>
        /// <param name="stackTrace">堆栈跟踪（可选）</param>
        /// <param name="timestamp">时间戳</param>
        /// <returns>格式化后的日志字符串</returns>
        string Format(LogLevel level, string message, string stackTrace, DateTime timestamp);
    }
}
```

- [ ] **Step 2: 创建 ILogWriter.cs**

```csharp
namespace ST.Core.Logging
{
    /// <summary>
    /// 日志写入器接口
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// 写入格式化后的日志
        /// </summary>
        void Write(string formattedLog);

        /// <summary>
        /// 刷新缓存到文件
        /// </summary>
        void Flush();

        /// <summary>
        /// 关闭写入器
        /// </summary>
        void Close();
    }
}
```

- [ ] **Step 3: 验证编译**

- [ ] **Step 4: 提交**

```bash
git add Runtime/Scripts/Logging/Interfaces/ILogFormatter.cs
git add Runtime/Scripts/Logging/Interfaces/ILogWriter.cs
git commit -m "feat(logging): add ILogFormatter and ILogWriter interfaces"
```

---

## Task 4: 创建管理器接口

**Files:**
- Create: `Runtime/Scripts/Logging/Interfaces/ILogManager.cs`

- [ ] **Step 1: 创建 ILogManager.cs**

```csharp
namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器接口
    /// </summary>
    public interface ILogManager
    {
        /// <summary>
        /// 初始化日志管理器
        /// </summary>
        void Initialize(ILogConfig config);

        /// <summary>
        /// 写入日志
        /// </summary>
        void Log(LogLevel level, string message, string stackTrace = null);

        /// <summary>
        /// 启用/禁用 Unity 日志捕获
        /// </summary>
        void EnableUnityLogCapture(bool enable);

        /// <summary>
        /// 刷新日志
        /// </summary>
        void Flush();

        /// <summary>
        /// 关闭日志管理器
        /// </summary>
        void Close();
    }
}
```

- [ ] **Step 2: 验证编译**

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/Interfaces/ILogManager.cs
git commit -m "feat(logging): add ILogManager interface"
```

---

## Task 5: 实现默认格式化器

**Files:**
- Create: `Runtime/Scripts/Logging/DefaultLogFormatter.cs`

- [ ] **Step 1: 创建 DefaultLogFormatter.cs**

```csharp
using System;

namespace ST.Core.Logging
{
    /// <summary>
    /// 默认日志格式化器
    /// 格式: [LogLevel][Day HH:MM:SS Millisecond]Message
    /// </summary>
    public class DefaultLogFormatter : ILogFormatter
    {
        public string Format(LogLevel level, string message, string stackTrace, DateTime timestamp)
        {
            string timeStr = $"{timestamp.Day} {timestamp.Hour:D2}:{timestamp.Minute:D2}:{timestamp.Second:D2} {timestamp.Millisecond}";
            string levelStr = GetLevelString(level);

            if (string.IsNullOrEmpty(stackTrace))
            {
                return $"[{levelStr}][{timeStr}]{message}";
            }
            else
            {
                return $"[{levelStr}][{timeStr}]{message}\n at {stackTrace}";
            }
        }

        private string GetLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "Debug";
                case LogLevel.Info:
                    return "Log";
                case LogLevel.Warning:
                    return "LogWarning";
                case LogLevel.Error:
                    return "LogError";
                case LogLevel.Exception:
                    return "LogException";
                default:
                    return "Log";
            }
        }
    }
}
```

- [ ] **Step 2: 验证编译**

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/DefaultLogFormatter.cs
git commit -m "feat(logging): implement DefaultLogFormatter"
```

---

## Task 6: 实现文件日志写入器（第1部分 - 基础结构）

**Files:**
- Create: `Runtime/Scripts/Logging/FileLogWriter.cs`

- [ ] **Step 1: 创建 FileLogWriter.cs 基础结构**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using ST.Core.Debugger;

namespace ST.Core.Logging
{
    /// <summary>
    /// 文件日志写入器
    /// 支持批量刷新、文件轮转、备份机制
    /// </summary>
    public class FileLogWriter : ILogWriter
    {
        private readonly string m_FilePath;
        private readonly int m_MaxFlushCount;
        private readonly long m_MaxFileSize;
        private readonly bool m_EnableBackup;

        private FileStream m_FileStream;
        private StreamWriter m_StreamWriter;
        private List<string> m_CacheList = new List<string>();

        public FileLogWriter(string filePath, int maxFlushCount, long maxFileSize, bool enableBackup)
        {
            m_FilePath = filePath;
            m_MaxFlushCount = maxFlushCount;
            m_MaxFileSize = maxFileSize;
            m_EnableBackup = enableBackup;

            OpenFile(false);
        }

        public void Write(string formattedLog)
        {
            // 待实现
        }

        public void Flush()
        {
            // 待实现
        }

        public void Close()
        {
            // 待实现
        }

        private void OpenFile(bool append)
        {
            // 待实现
        }

        private void BackupFile()
        {
            // 待实现
        }
    }
}
```

- [ ] **Step 2: 验证编译**

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/FileLogWriter.cs
git commit -m "feat(logging): add FileLogWriter basic structure"
```

---

## Task 7: 实现文件日志写入器（第2部分 - 文件操作）

**Files:**
- Modify: `Runtime/Scripts/Logging/FileLogWriter.cs`

- [ ] **Step 1: 实现 OpenFile 方法**

```csharp
private void OpenFile(bool append)
{
    try
    {
        // 检查文件大小，超过限制则清空或备份
        if (File.Exists(m_FilePath))
        {
            FileInfo fileInfo = new FileInfo(m_FilePath);
            if (fileInfo.Length > m_MaxFileSize)
            {
                if (m_EnableBackup)
                {
                    BackupFile();
                }
                else
                {
                    File.Delete(m_FilePath);
                }
            }
        }

        // 创建目录
        string directory = Path.GetDirectoryName(m_FilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        m_FileStream = File.Open(m_FilePath, FileMode.OpenOrCreate);

        if (append)
        {
            m_FileStream.Seek(0, SeekOrigin.End);
        }
        else
        {
            m_FileStream.SetLength(0);
        }

        m_StreamWriter = new StreamWriter(m_FileStream);
    }
    catch (Exception ex)
    {
        Debugger.Debugger.LogError($"[FileLogWriter] Open file failed: {m_FilePath}, Error: {ex.Message}");
    }
}
```

- [ ] **Step 2: 实现 BackupFile 方法**

```csharp
private void BackupFile()
{
    try
    {
        string backupPath = m_FilePath + ".bak";
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
        File.Move(m_FilePath, backupPath);
    }
    catch (Exception ex)
    {
        Debugger.Debugger.LogError($"[FileLogWriter] Backup file failed: {ex.Message}");
    }
}
```

- [ ] **Step 3: 验证编译**

- [ ] **Step 4: 提交**

```bash
git add Runtime/Scripts/Logging/FileLogWriter.cs
git commit -m "feat(logging): implement file operations in FileLogWriter"
```

---

## Task 8: 实现文件日志写入器（第3部分 - 写入和刷新）

**Files:**
- Modify: `Runtime/Scripts/Logging/FileLogWriter.cs`

- [ ] **Step 1: 实现 Write 方法**

```csharp
public void Write(string formattedLog)
{
    if (m_StreamWriter == null) return;

    m_CacheList.Add(formattedLog);

    if (m_CacheList.Count >= m_MaxFlushCount)
    {
        Flush();
    }
}
```

- [ ] **Step 2: 实现 Flush 方法**

```csharp
public void Flush()
{
    if (m_StreamWriter == null || m_CacheList.Count == 0) return;

    try
    {
        foreach (string log in m_CacheList)
        {
            m_StreamWriter.WriteLine(log);
        }
        m_CacheList.Clear();
        m_StreamWriter.Flush();
    }
    catch (Exception ex)
    {
        Debugger.Debugger.LogError($"[FileLogWriter] Flush failed: {ex.Message}");
    }
}
```

- [ ] **Step 3: 实现 Close 方法**

```csharp
public void Close()
{
    Flush();

    if (m_StreamWriter != null)
    {
        m_StreamWriter.Close();
        m_StreamWriter = null;
    }

    if (m_FileStream != null)
    {
        m_FileStream.Close();
        m_FileStream = null;
    }
}
```

- [ ] **Step 4: 验证编译**

- [ ] **Step 5: 提交**

```bash
git add Runtime/Scripts/Logging/FileLogWriter.cs
git commit -m "feat(logging): implement write, flush and close in FileLogWriter"
```

---

## Task 9: 实现默认配置类

**Files:**
- Create: `Runtime/Scripts/Logging/LogConfig.cs`

- [ ] **Step 1: 创建 LogConfig.cs**

```csharp
using System.IO;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 默认日志配置
    /// 支持链式配置
    /// </summary>
    public class LogConfig : ILogConfig
    {
        private string m_LogFilePath;
        private bool m_IsLowMemoryDevice;
        private int m_MaxFlushCount;
        private long m_MaxFileSize;
        private bool m_EnableBackup;

        public LogConfig()
        {
            // 默认配置
            m_LogFilePath = Path.Combine(Application.persistentDataPath, "Logs", "Output.txt");
            m_IsLowMemoryDevice = SystemInfo.systemMemorySize < 2048; // 2GB
            m_MaxFlushCount = m_IsLowMemoryDevice ? 20 : 100;
            m_MaxFileSize = 20 * 1024 * 1024; // 20MB
            m_EnableBackup = true;
        }

        // 链式配置方法
        public LogConfig SetLogFilePath(string path)
        {
            m_LogFilePath = path;
            return this;
        }

        public LogConfig SetLowMemoryDevice(bool isLowMemory)
        {
            m_IsLowMemoryDevice = isLowMemory;
            m_MaxFlushCount = isLowMemory ? 20 : 100;
            return this;
        }

        public LogConfig SetMaxFlushCount(int count)
        {
            m_MaxFlushCount = count;
            return this;
        }

        public LogConfig SetMaxFileSize(long size)
        {
            m_MaxFileSize = size;
            return this;
        }

        public LogConfig SetEnableBackup(bool enable)
        {
            m_EnableBackup = enable;
            return this;
        }

        // ILogConfig 实现
        public string GetLogFilePath() => m_LogFilePath;
        public bool IsLowMemoryDevice() => m_IsLowMemoryDevice;
        public int GetMaxFlushCount() => m_MaxFlushCount;
        public long GetMaxFileSize() => m_MaxFileSize;
        public bool EnableBackup() => m_EnableBackup;
    }
}
```

- [ ] **Step 2: 验证编译**

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/LogConfig.cs
git commit -m "feat(logging): implement LogConfig with fluent API"
```

---

## Task 10: 实现日志管理器（第1部分 - 基础结构）

**Files:**
- Create: `Runtime/Scripts/Logging/LogManager.cs`

- [ ] **Step 1: 创建 LogManager.cs 基础结构**

```csharp
using System;
using UnityEngine;

namespace ST.Core.Logging
{
    /// <summary>
    /// 日志管理器
    /// 协调 Writer 和 Formatter，管理 Unity 日志捕获
    /// </summary>
    public class LogManager : ILogManager
    {
        private ILogWriter m_Writer;
        private ILogFormatter m_Formatter;
        private ILogConfig m_Config;
        private bool m_UnityLogCaptureEnabled;

        public void Initialize(ILogConfig config)
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

        public void Log(LogLevel level, string message, string stackTrace = null)
        {
            if (m_Writer == null) return;

            string formatted = m_Formatter.Format(level, message, stackTrace, DateTime.Now);
            m_Writer.Write(formatted);
        }

        public void EnableUnityLogCapture(bool enable)
        {
            // 待实现
        }

        public void Flush()
        {
            m_Writer?.Flush();
        }

        public void Close()
        {
            EnableUnityLogCapture(false);
            m_Writer?.Close();
        }

        private void OnUnityLogCallback(string condition, string stackTrace, LogType type)
        {
            // 待实现
        }

        private LogLevel ConvertUnityLogType(LogType type)
        {
            // 待实现
            return LogLevel.Info;
        }
    }
}
```

- [ ] **Step 2: 验证编译**

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/LogManager.cs
git commit -m "feat(logging): add LogManager basic structure"
```

---

## Task 11: 实现日志管理器（第2部分 - Unity 日志捕获）

**Files:**
- Modify: `Runtime/Scripts/Logging/LogManager.cs`

- [ ] **Step 1: 实现 EnableUnityLogCapture 方法**

```csharp
public void EnableUnityLogCapture(bool enable)
{
    if (m_UnityLogCaptureEnabled == enable) return;

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
```

- [ ] **Step 2: 实现 OnUnityLogCallback 方法**

```csharp
private void OnUnityLogCallback(string condition, string stackTrace, LogType type)
{
    LogLevel level = ConvertUnityLogType(type);
    Log(level, condition, stackTrace);
}
```

- [ ] **Step 3: 实现 ConvertUnityLogType 方法**

```csharp
private LogLevel ConvertUnityLogType(LogType type)
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
```

- [ ] **Step 4: 验证编译**

- [ ] **Step 5: 提交**

```bash
git add Runtime/Scripts/Logging/LogManager.cs
git commit -m "feat(logging): implement Unity log capture in LogManager"
```

---

## Task 12: 实现静态门面

**Files:**
- Create: `Runtime/Scripts/Logging/Logger.cs`

- [ ] **Step 1: 创建 Logger.cs**

```csharp
namespace ST.Core.Logging
{
    /// <summary>
    /// 日志系统静态门面
    /// 提供便捷的静态 API
    /// </summary>
    public static class Logger
    {
        private static ILogManager s_Manager = new LogManager();

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        public static void Initialize(ILogConfig config)
        {
            s_Manager.Initialize(config);
        }

        /// <summary>
        /// 设置自定义日志管理器
        /// </summary>
        public static void SetManager(ILogManager manager)
        {
            if (manager != null)
            {
                s_Manager = manager;
            }
        }

        /// <summary>
        /// 启用/禁用 Unity 日志捕获
        /// </summary>
        public static void EnableUnityLogCapture(bool enable)
        {
            s_Manager.EnableUnityLogCapture(enable);
        }

        /// <summary>
        /// 写入 Info 级别日志
        /// </summary>
        public static void Log(string message)
        {
            s_Manager.Log(LogLevel.Info, message);
        }

        /// <summary>
        /// 写入 Warning 级别日志
        /// </summary>
        public static void LogWarning(string message)
        {
            s_Manager.Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// 写入 Error 级别日志
        /// </summary>
        public static void LogError(string message)
        {
            s_Manager.Log(LogLevel.Error, message);
        }

        /// <summary>
        /// 写入 Debug 级别日志
        /// </summary>
        public static void LogDebug(string message)
        {
            s_Manager.Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// 刷新日志缓存
        /// </summary>
        public static void Flush()
        {
            s_Manager.Flush();
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Close()
        {
            s_Manager.Close();
        }
    }
}
```

- [ ] **Step 2: 验证编译**

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/Logger.cs
git commit -m "feat(logging): implement Logger static facade"
```

---

## Task 13: 创建使用示例和 README

**Files:**
- Create: `Runtime/Scripts/Logging/README.md`

- [ ] **Step 1: 创建 README.md**

```markdown
# ST.Core.Logging

文件日志系统，支持批量刷新、文件轮转、Unity 日志捕获。

## 快速开始

### 基础使用

```csharp
using ST.Core.Logging;

// 初始化（使用默认配置）
Logger.Initialize(new LogConfig());

// 写入日志
Logger.Log("Application started");
Logger.LogWarning("Low memory detected");
Logger.LogError("Connection failed");

// 应用退出时
void OnApplicationQuit()
{
    Logger.Flush();
    Logger.Close();
}
```

### 自定义配置

```csharp
var config = new LogConfig()
    .SetLogFilePath("/custom/path/log.txt")
    .SetMaxFlushCount(50)
    .SetMaxFileSize(10 * 1024 * 1024)  // 10MB
    .SetEnableBackup(true);

Logger.Initialize(config);
```

### Unity 日志捕获

```csharp
// 启用 Unity 日志捕获
Logger.Initialize(new LogConfig());
Logger.EnableUnityLogCapture(true);

// 所有 Unity 日志自动写入文件
Debug.Log("This will be captured");
Debug.LogError("This error will be captured");
```

### 适配现有项目

```csharp
// 实现 ILogConfig 适配现有依赖
public class ProjectLogConfig : ILogConfig
{
    public string GetLogFilePath()
    {
        return FilePath.persistentDataPath4Temp + "Output.txt";
    }
    
    public bool IsLowMemoryDevice()
    {
        return DeviceModule.isLowMemoryDevice;
    }
    
    public int GetMaxFlushCount()
    {
        return IsLowMemoryDevice() ? 20 : 100;
    }
    
    public long GetMaxFileSize()
    {
        return 20 * 1024 * 1024;  // 20MB
    }
    
    public bool EnableBackup()
    {
        return true;
    }
}

// 使用
Logger.Initialize(new ProjectLogConfig());
```

## 核心特性

- **批量刷新** - 缓存日志，达到阈值后批量写入（低内存 20 条，正常 100 条）
- **文件轮转** - 文件超过 20MB 自动备份
- **Unity 日志捕获** - 可选的 Unity 日志自动捕获
- **依赖抽象** - 通过接口抽象外部依赖
- **异常安全** - I/O 失败不影响主流程

## 架构

```
Logger (静态门面)
  ↓
LogManager (管理器)
  ↓
FileLogWriter + DefaultLogFormatter
```

## 扩展

### 自定义写入器

```csharp
public class ConsoleLogWriter : ILogWriter
{
    public void Write(string formattedLog)
    {
        Console.WriteLine(formattedLog);
    }
    
    public void Flush() { }
    public void Close() { }
}
```

### 自定义管理器

```csharp
public class CustomLogManager : ILogManager
{
    // 实现自定义逻辑
}

Logger.SetManager(new CustomLogManager());
```

## 依赖

- Unity Engine (Application, Debug, LogType)
- ST.Core.Debugger
- System.IO

## 许可

MIT License
```

- [ ] **Step 2: 验证文档格式**

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/README.md
git commit -m "docs(logging): add README with usage examples"
```

---

## Task 14: 创建 Assembly Definition

**Files:**
- Create: `Runtime/Scripts/Logging/ST.Core.Logging.asmdef`

- [ ] **Step 1: 创建 asmdef 文件**

```json
{
    "name": "ST.Core.Logging",
    "rootNamespace": "ST.Core.Logging",
    "references": [
        "ST.Core.Debugger"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: 在 Unity Editor 中验证**

检查 Assembly Definition 是否正确加载

- [ ] **Step 3: 提交**

```bash
git add Runtime/Scripts/Logging/ST.Core.Logging.asmdef
git commit -m "feat(logging): add assembly definition"
```

---

## Task 15: 集成测试

**Files:**
- Create: `Runtime/Scripts/Logging/Examples/LoggingExample.cs`

- [ ] **Step 1: 创建示例目录**

```bash
mkdir -p "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Runtime\Scripts\Logging\Examples"
```

- [ ] **Step 2: 创建 LoggingExample.cs**

```csharp
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
```

- [ ] **Step 3: 在 Unity 中测试**

1. 创建空场景
2. 添加 GameObject 并挂载 LoggingExample 脚本
3. 运行场景
4. 检查日志文件是否生成
5. 验证日志内容格式正确

- [ ] **Step 4: 验证功能**

检查点：
- [ ] 日志文件正确创建
- [ ] 日志格式正确
- [ ] Unity 日志被捕获
- [ ] 批量刷新工作正常
- [ ] 应用退出时正确关闭

- [ ] **Step 5: 提交**

```bash
git add Runtime/Scripts/Logging/Examples/
git commit -m "test(logging): add integration test example"
```

---

## Task 16: 最终验证和文档更新

**Files:**
- Modify: `docs/superpowers/specs/2026-03-29-logging-system-design.md`

- [ ] **Step 1: 运行完整测试**

1. 清理 Unity 项目
2. 重新编译
3. 运行示例场景
4. 验证所有功能

- [ ] **Step 2: 更新设计文档状态**

将设计文档状态从"设计阶段"更新为"已实现"

- [ ] **Step 3: 创建 CHANGELOG**

记录实现的功能和版本信息

- [ ] **Step 4: 最终提交**

```bash
git add .
git commit -m "feat(logging): complete logging system implementation

- Implement layered architecture with interface abstraction
- Support file rotation, backup, and Unity log capture
- Add configurable batch flush and low memory optimization
- Include usage examples and documentation"
```

---

## 验收标准

### 功能验收

- [ ] 所有接口定义完整
- [ ] FileLogWriter 支持批量刷新
- [ ] FileLogWriter 支持文件轮转和备份
- [ ] LogManager 支持 Unity 日志捕获
- [ ] Logger 静态门面工作正常
- [ ] LogConfig 支持链式配置

### 质量验收

- [ ] 代码编译无错误
- [ ] 代码编译无警告
- [ ] 所有公共 API 有 XML 注释
- [ ] 异常处理完善
- [ ] 示例代码可运行

### 文档验收

- [ ] README 完整清晰
- [ ] 使用示例充足
- [ ] 设计文档更新
- [ ] 代码注释完整

---

## 注意事项

1. **命名空间统一** - 所有类使用 `ST.Core.Logging` 命名空间
2. **异常处理** - 所有 I/O 操作必须包裹 try-catch
3. **资源释放** - 确保 FileStream 和 StreamWriter 正确关闭
4. **线程安全** - 当前实现不考虑多线程，Unity 主线程使用
5. **性能优化** - 批量刷新减少 I/O 次数
6. **向后兼容** - 保持接口稳定，实现可替换

---

## 后续优化（可选）

1. **单元测试** - 添加 NUnit 测试
2. **性能测试** - 测试 1000 条日志写入耗时
3. **异步写入** - 后台线程写入避免阻塞
4. **日志过滤** - 按级别或标签过滤
5. **多输出目标** - 同时输出到文件和控制台

---

**计划版本：** 1.0
**创建日期：** 2026-03-29
**预计工时：** 4-6 小时

