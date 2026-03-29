# 日志系统设计文档

**日期：** 2026-03-29
**版本：** 1.0
**状态：** 设计阶段

## 1. 概述

### 1.1 目标

从现有项目（F:\proj_se\develop\client\project）中提取日志系统相关代码，整理到 com.spacetime.core 包中，创建一个解耦、可扩展的文件日志系统。

### 1.2 范围

**包含：**
- 文件日志写入器（FileLogger）
- 日志回调机制（Unity 日志捕获）
- 日志级别控制
- 批量刷新机制
- 文件轮转和备份
- 低内存设备优化

**不包含：**
- 日志上传功能
- Dump 信息收集
- 手势检测
- 调试工具集成

### 1.3 设计原则

1. **依赖抽象** - 外部依赖通过接口注入
2. **职责分离** - 单一职责，易于维护
3. **静态门面** - 便捷的静态 API
4. **可替换实现** - 支持自定义扩展
5. **异常安全** - 日志失败不影响主流程

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────┐
│         Logger (静态门面)            │
│  - Log() / LogError() / LogWarning() │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│       LogManager (管理器)            │
│  - 协调 Writer 和 Formatter          │
│  - Unity 日志捕获                    │
└──────┬──────────────┬────────────────┘
       │              │
       ▼              ▼
┌─────────────┐  ┌──────────────┐
│ ILogWriter  │  │ ILogFormatter│
└─────────────┘  └──────────────┘
       │              │
       ▼              ▼
┌─────────────┐  ┌──────────────┐
│FileLogWriter│  │DefaultLog    │
│             │  │Formatter     │
└─────────────┘  └──────────────┘
```

### 2.2 核心组件

#### 2.2.1 Logger（静态门面）

**职责：**
- 提供静态 API 便捷访问
- 管理 LogManager 实例
- 支持实现替换

**接口：**
```csharp
public static class Logger
{
    // 日志方法
    public static void Log(string message);
    public static void LogWarning(string message);
    public static void LogError(string message);
    public static void LogDebug(string message);

    // 配置方法
    public static void Initialize(ILogConfig config);
    public static void SetManager(ILogManager manager);
    public static void EnableUnityLogCapture(bool enable);

    // 生命周期
    public static void Flush();
    public static void Close();
}
```

#### 2.2.2 LogManager（管理器）

**职责：**
- 协调 Writer 和 Formatter
- 管理 Unity 日志捕获
- 日志级别转换

**关键逻辑：**
- 初始化时创建 Writer 和 Formatter
- Unity 日志回调转换为内部日志级别
- 异常处理保证稳定性

#### 2.2.3 FileLogWriter（文件写入器）

**职责：**
- 文件 I/O 操作
- 缓存管理
- 文件轮转和备份

**核心特性：**
1. **批量刷新** - 缓存日志，达到阈值后批量写入
2. **文件轮转** - 文件超过大小限制时自动处理
3. **备份机制** - 轮转时保留 .bak 文件
4. **异常处理** - I/O 失败不抛出异常

**配置参数：**
- `maxFlushCount` - 批量刷新阈值（低内存 20，正常 100）
- `maxFileSize` - 文件大小限制（默认 20MB）
- `enableBackup` - 是否启用备份（默认 true）

#### 2.2.4 DefaultLogFormatter（格式化器）

**职责：**
- 格式化日志消息
- 添加时间戳和级别标签

**格式：**
```
[LogLevel][Day HH:MM:SS Millisecond]Message
[LogLevel][Day HH:MM:SS Millisecond]Message
 at StackTrace
```

**示例：**
```
[Log][29 14:30:45 123]Application started
[LogError][29 14:30:50 456]Connection failed
 at NetworkManager.Connect() in NetworkManager.cs:42
```

#### 2.2.5 LogConfig（配置类）

**职责：**
- 提供默认配置
- 支持链式配置
- 抽象外部依赖

**依赖抽象：**
- `GetLogFilePath()` - 替代 `FilePath.persistentDataPath4Temp`
- `IsLowMemoryDevice()` - 替代 `DeviceModule.isLowMemoryDevice`

## 3. 接口设计

### 3.1 ILogManager

```csharp
public interface ILogManager
{
    void Initialize(ILogConfig config);
    void Log(LogLevel level, string message, string stackTrace = null);
    void EnableUnityLogCapture(bool enable);
    void Flush();
    void Close();
}
```

### 3.2 ILogWriter

```csharp
public interface ILogWriter
{
    void Write(string formattedLog);
    void Flush();
    void Close();
}
```

### 3.3 ILogFormatter

```csharp
public interface ILogFormatter
{
    string Format(LogLevel level, string message, string stackTrace, DateTime timestamp);
}
```

### 3.4 ILogConfig

```csharp
public interface ILogConfig
{
    string GetLogFilePath();
    bool IsLowMemoryDevice();
    int GetMaxFlushCount();
    long GetMaxFileSize();
    bool EnableBackup();
}
```

## 4. 数据流

### 4.1 日志写入流程

```
1. Logger.Log("message")
   ↓
2. LogManager.Log(LogLevel.Info, "message")
   ↓
3. DefaultLogFormatter.Format(...)
   ↓
4. FileLogWriter.Write(formattedLog)
   ↓
5. 缓存到 List<string>
   ↓
6. 达到阈值 → Flush() → 写入文件
```

### 4.2 Unity 日志捕获流程

```
1. Debug.Log("message")
   ↓
2. Application.logMessageReceived 回调
   ↓
3. LogManager.OnUnityLogCallback(...)
   ↓
4. 转换 LogType → LogLevel
   ↓
5. LogManager.Log(level, message, stackTrace)
   ↓
6. 正常日志写入流程
```

### 4.3 文件轮转流程

```
1. FileLogWriter 初始化
   ↓
2. 检查文件是否存在
   ↓
3. 文件大小 > maxFileSize?
   ├─ Yes → 备份文件 (.bak)
   └─ No  → 继续使用
   ↓
4. 打开文件流
```

## 5. 使用场景

### 5.1 基础使用

```csharp
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

### 5.2 自定义配置

```csharp
var config = new LogConfig()
    .SetLogFilePath("/custom/path/log.txt")
    .SetMaxFlushCount(50)
    .SetMaxFileSize(10 * 1024 * 1024)  // 10MB
    .SetEnableBackup(true);

Logger.Initialize(config);
```

### 5.3 Unity 日志捕获

```csharp
// 启用 Unity 日志捕获
Logger.Initialize(new LogConfig());
Logger.EnableUnityLogCapture(true);

// 所有 Unity 日志自动写入文件
Debug.Log("This will be captured");
Debug.LogError("This error will be captured");
```

### 5.4 适配现有项目

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

### 5.5 自定义扩展

```csharp
// 自定义写入器（例如：网络日志）
public class NetworkLogWriter : ILogWriter
{
    public void Write(string formattedLog)
    {
        // 发送到服务器
    }
    
    public void Flush() { }
    public void Close() { }
}

// 自定义管理器
public class CustomLogManager : ILogManager
{
    private ILogWriter m_FileWriter;
    private ILogWriter m_NetworkWriter;
    
    public void Initialize(ILogConfig config)
    {
        m_FileWriter = new FileLogWriter(...);
        m_NetworkWriter = new NetworkLogWriter();
    }
    
    public void Log(LogLevel level, string message, string stackTrace = null)
    {
        // 同时写入文件和网络
        string formatted = Format(level, message, stackTrace);
        m_FileWriter.Write(formatted);
        m_NetworkWriter.Write(formatted);
    }
    
    // 其他方法实现...
}

// 替换默认实现
Logger.SetManager(new CustomLogManager());
```

## 6. 文件结构

```
com.spacetime.core/
└── Runtime/
    └── Scripts/
        └── Logging/
            ├── Logger.cs                    // 静态门面
            ├── LogManager.cs                // 管理器实现
            ├── FileLogWriter.cs             // 文件写入器
            ├── DefaultLogFormatter.cs       // 默认格式化器
            ├── LogConfig.cs                 // 默认配置
            ├── Interfaces/
            │   ├── ILogManager.cs
            │   ├── ILogWriter.cs
            │   ├── ILogFormatter.cs
            │   └── ILogConfig.cs
            └── Enums/
                └── LogLevel.cs
```

## 7. 性能考虑

### 7.1 批量刷新

**问题：** 频繁的文件 I/O 影响性能

**解决：**
- 缓存日志到内存 List
- 达到阈值后批量写入
- 低内存设备降低阈值（20 vs 100）

### 7.2 文件大小控制

**问题：** 日志文件无限增长

**解决：**
- 检查文件大小（默认 20MB）
- 超过限制时备份并重新创建
- 只保留最近一次备份

### 7.3 异常处理

**问题：** I/O 异常影响主流程

**解决：**
- 所有 I/O 操作包裹 try-catch
- 失败时记录到 Debugger
- 不抛出异常，保证主流程稳定

## 8. 测试策略

### 8.1 单元测试

**测试点：**
- LogFormatter 格式化正确性
- FileLogWriter 缓存和刷新逻辑
- 文件轮转和备份机制
- Unity 日志类型转换

**Mock 对象：**
- ILogConfig - 提供测试配置
- ILogWriter - 验证写入调用

### 8.2 集成测试

**测试场景：**
1. 完整日志写入流程
2. Unity 日志捕获
3. 文件大小超限处理
4. 低内存设备优化
5. 异常情况处理

### 8.3 性能测试

**测试指标：**
- 1000 条日志写入耗时
- 内存占用（缓存大小）
- 文件 I/O 次数

## 9. 迁移计划

### 9.1 从现有项目提取

**源文件：**
- `F:\proj_se\develop\client\project\Assets\Script\LogicModule\UpdateModule\FileLogger.cs`
- `F:\proj_se\develop\client\project\Assets\Script\LogicModule\DebugModule\DebugModule.cs`（部分）

**提取内容：**
- FileLogger 类的核心逻辑
- DebugModule 中的 OnLogCallback 方法
- 日志格式化逻辑

### 9.2 重构步骤

1. 创建接口定义
2. 实现 FileLogWriter（基于原 FileLogger）
3. 实现 DefaultLogFormatter
4. 实现 LogManager
5. 实现 Logger 静态门面
6. 实现 LogConfig
7. 编写单元测试
8. 编写使用示例

### 9.3 兼容性

**命名空间：** `ST.Core.Logging`

**依赖：**
- Unity Engine（Application、Debug、LogType）
- ST.Core.Debugger（Debugger 类）
- System.IO（文件操作）

## 10. 未来扩展

### 10.1 可能的扩展点

1. **多输出目标**
   - 控制台输出
   - 网络日志服务器
   - 数据库存储

2. **日志过滤**
   - 按级别过滤
   - 按标签过滤
   - 正则表达式过滤

3. **异步写入**
   - 后台线程写入
   - 避免阻塞主线程

4. **日志压缩**
   - 自动压缩旧日志
   - 节省磁盘空间

5. **结构化日志**
   - JSON 格式
   - 支持字段查询

### 10.2 扩展示例

```csharp
// 扩展：添加控制台输出
public class ConsoleLogWriter : ILogWriter
{
    public void Write(string formattedLog)
    {
        Console.WriteLine(formattedLog);
    }
    
    public void Flush() { }
    public void Close() { }
}

// 扩展：组合多个写入器
public class CompositeLogWriter : ILogWriter
{
    private List<ILogWriter> m_Writers = new List<ILogWriter>();
    
    public void AddWriter(ILogWriter writer)
    {
        m_Writers.Add(writer);
    }
    
    public void Write(string formattedLog)
    {
        foreach (var writer in m_Writers)
        {
            writer.Write(formattedLog);
        }
    }
    
    public void Flush()
    {
        foreach (var writer in m_Writers)
        {
            writer.Flush();
        }
    }
    
    public void Close()
    {
        foreach (var writer in m_Writers)
        {
            writer.Close();
        }
    }
}
```

## 11. 总结

### 11.1 设计优势

1. **解耦** - 接口抽象外部依赖
2. **可扩展** - 支持自定义 Writer、Formatter、Manager
3. **易用** - 静态门面提供便捷 API
4. **稳定** - 异常安全处理
5. **高效** - 批量刷新优化性能

### 11.2 核心价值

- 提供独立的日志系统包
- 可在多个项目中复用
- 易于测试和维护
- 支持灵活配置和扩展

### 11.3 下一步

1. 编写实现计划
2. 实现核心类
3. 编写单元测试
4. 编写使用文档
5. 集成到现有项目验证

---

**文档版本：** 1.0
**最后更新：** 2026-03-29
**作者：** Claude Code
