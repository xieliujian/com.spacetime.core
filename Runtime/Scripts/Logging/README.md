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
