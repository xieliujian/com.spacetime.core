# ST.Core.Logging

文件日志系统，支持批量刷新、文件轮转、Unity 日志捕获。  
所有日志调用统一通过 `Logger` 静态门面完成，内部实现对外不可见。

---

## 快速开始

### 基础使用

```csharp
using ST.Core.Logging;

// 1. 初始化文件写入系统
Logger.Initialize(new LogConfig());

// 2. 开启 Unity 日志捕获（可选，开启后日志自动写入文件）
Logger.EnableUnityLogCapture(true);

// 3. 写入日志
Logger.Log("Application started");          // Info 级别（字符串快捷入口）
Logger.LogInfo("Player loaded");            // Info 级别
Logger.LogDebug("Frame delta: " + dt);      // Debug 级别
Logger.LogWarning("Low memory detected");   // Warning 级别
Logger.LogError("Connection failed");       // Error 级别
Logger.LogFatal("Critical system failure"); // Fatal 级别（同时触发致命回调）

// 4. 程序退出时刷新并关闭
void OnApplicationQuit()
{
    Logger.Flush();
    Logger.Close();
}
```

### 格式化输出

```csharp
Logger.LogInfoF("Player {0} joined room {1}", playerName, roomId);
Logger.LogDebugF("Position: ({0:F2}, {1:F2})", x, y);
Logger.LogWarningF("Retry {0}/{1}", attempt, maxRetry);
Logger.LogErrorF("Request {0} failed with code {1}", url, code);
Logger.LogFatalF("Unhandled exception in {0}: {1}", module, ex.Message);
```

### 异常日志

```csharp
try
{
    // ...
}
catch (Exception ex)
{
    Logger.LogException(ex);  // 自动包含完整堆栈
}
```

### 断言（仅 DEBUG 模式）

```csharp
// Release 模式下调用被编译器完全消除，无运行时开销
Logger.Assert(index >= 0, "Index must be non-negative");
Logger.Assert(config != null);
```

### 日志级别开关

```csharp
// 须在 Initialize() 之后调用
Logger.SetAllIsLog(false);  // 关闭所有级别输出（如正式发布时）
Logger.SetAllIsLog(true);   // 全部重新开启
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

开启后，所有经由 `UnityEngine.Debug` 输出的日志（包括 Logger 本身的所有 LogXxx 调用）
均会被自动捕获并写入文件，无需在每处调用点额外处理。

```csharp
Logger.Initialize(new LogConfig());
Logger.EnableUnityLogCapture(true);

// 以下所有输出均自动落地到文件
Debug.Log("Unity native log");
Logger.LogError("My error");
```

### 适配现有项目

```csharp
public class ProjectLogConfig : ILogConfig
{
    public string GetLogFilePath()
        => FilePath.persistentDataPath4Temp + "Output.txt";

    public bool IsLowMemoryDevice()
        => DeviceModule.isLowMemoryDevice;

    public int GetMaxFlushCount()
        => IsLowMemoryDevice() ? 20 : 100;

    public long GetMaxFileSize()
        => 20 * 1024 * 1024;  // 20MB

    public bool EnableBackup()
        => true;
}

Logger.Initialize(new ProjectLogConfig());
```

---

## Logger API 一览

| 方法 | 级别 | 说明 |
|------|------|------|
| `Log(string)` | Info | 字符串快捷入口，等同于 LogInfo |
| `LogInfo(object)` | Info | — |
| `LogInfoF(object, params)` | Info | 格式化 |
| `LogDebug(object)` | Debug | — |
| `LogDebugF(object, params)` | Debug | 格式化 |
| `LogWarning(object)` | Warning | — |
| `LogWarningF(object, params)` | Warning | 格式化 |
| `LogError(object)` | Error | — |
| `LogErrorF(object, params)` | Error | 格式化 |
| `LogException(Exception)` | Exception | 含完整堆栈 |
| `LogFatal(object)` | Fatal | 输出后触发致命回调 |
| `LogFatalF(object, params)` | Fatal | 格式化 |
| `Assert(bool)` | — | 仅 DEBUG，失败触发回调 |
| `Assert(bool, string)` | — | 仅 DEBUG，带说明 |
| `Initialize(LogConfig)` | — | 初始化文件系统 |
| `SetAllIsLog(bool)` | — | 统一开关所有级别 |
| `EnableUnityLogCapture(bool)` | — | 开启/关闭 Unity 日志捕获 |
| `Flush()` | — | 立即刷新缓冲到文件 |
| `Close()` | — | 刷新并释放文件资源 |

---

## 核心特性

- **统一入口** - 所有日志调用通过 `Logger` 完成，内部实现不对外暴露
- **批量刷新** - 缓存日志，达到阈值后批量写入（低内存 20 条，正常 100 条）
- **文件轮转** - 文件超过 20MB 自动备份
- **Unity 日志捕获** - 可选的 Unity 日志自动捕获，经由 `Application.logMessageReceived` 落地
- **级别开关** - 每个级别独立可控，支持一键全关
- **依赖抽象** - 通过接口抽象外部依赖
- **异常安全** - I/O 失败不影响主流程

---

## 架构

```
Logger（唯一公开入口）
  ↓
内部 Debugger（私有嵌套类，包外不可见）
  ↓
UnityEngine.Debug.LogXxx
  ↓
Application.logMessageReceived 事件
  ↓（仅 EnableUnityLogCapture(true) 时）
LogManager → FileLogWriter → 文件
```

---

## 依赖

- Unity Engine（`Application`、`Debug`、`LogType`）
- `System.IO`

---

## 许可

MIT License
