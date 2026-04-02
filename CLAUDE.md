# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity game project (Unity 2022.3.35f1c1) using the Universal Render Pipeline (URP 14.0.11). The core framework is organized as a custom Unity package (`com.spacetime.core`) located in `Packages/com.spacetime.core/`.

## Project Structure

### Core Package (`Packages/com.spacetime.core/`)

The main framework is structured as follows:

- **Runtime/Scripts/Core/** - Core framework classes
  - `IManager` - Base class for all manager systems (DoInit, DoUpdate, DoLateUpdate, DoClose lifecycle)
  - `GameEvent` - Event system
  - `ByteBuffer` - Binary data handling
  - `CommonDefine` - Common constants and definitions
  - `IMainThreadTask` - Main thread task interface

- **Runtime/Scripts/Logging/** - Logging system (`ST.Core.Logging` namespace)
  - `Logger` - **唯一对外入口**（静态门面），所有日志调用均通过此类
  - `LogManager` - 内部日志管理器，协调文件写入与 Unity 日志捕获
  - `FileLogWriter` - 文件写入器（缓存 + 批量刷新）
  - `DefaultLogFormatter` - 日志格式化器
  - `LogConfig` - 日志配置（链式调用风格）
  - `LogLevel` - 日志级别枚举（Debug / Info / Warning / Error / Exception）
  - **注意**：`Debugger` 类已作为 `Logger` 的私有嵌套类存在，外部不可直接访问

- **Runtime/Scripts/Network/** - Network communication layer
  - `NetManager` - Singleton network manager, handles socket connections and message queuing
  - `SocketClient` - TCP socket client implementation
  - `MsgDispatcher` - Message dispatcher supporting both Protobuf and FlatBuffers
  - `ProtobufProcFun` / `FlatBufferProcFun` - Message processing function interfaces

- **Runtime/Scripts/Middleware/** - Third-party libraries
  - `Protobuf/` - Google Protocol Buffers implementation
  - `FlatBuffers/` - FlatBuffers serialization library

- **Runtime/Scripts/Msg/** - Message definitions
  - `pbs/` - Protobuf message definitions

- **Runtime/Scripts/Table/** - Data table system
  - `TableLoader` - Table loading system
  - `StblReader` - Binary table reader
  - `DataStreamReader` - Data stream reader

- **Runtime/Scripts/Manager/** - Game managers
  - `MainThreadTask` - Main thread task execution

- **Editor/Scripts/** - Unity Editor tools
  - `ShaderVariant/` - Shader variant collection tools
  - `Utils/` - Editor utilities

### Assembly Structure

The project uses multiple C# assemblies:
- `com.spacetime.core.runtime` - Runtime core framework
- `com.spacetime.core.editor` - Editor tools
- `com.spacetime.core.shaders` - Shader code
- `Assembly-CSharp` - Main game scripts

## Architecture Patterns

### Manager System
All managers inherit from `IManager` base class and follow a consistent lifecycle:
1. `DoInit()` - Initialize the manager
2. `DoUpdate()` - Called every frame
3. `DoLateUpdate()` - Called after Update
4. `DoClose()` - Cleanup and shutdown

Managers use singleton pattern (accessed via static `S` property).

### Network Architecture
- **Message Flow**: SocketClient → NetManager (event queue) → MsgDispatcher → Message handlers
- **Serialization**: Supports both Protobuf and FlatBuffers (configurable via `IMsgType`)
- **Thread Safety**: Network messages are queued and processed on the main thread

### Logging Architecture

调用链路：`Logger.LogXxx()` → 内部私有 `Debugger` 嵌套类 → `UnityEngine.Debug.LogXxx` → `Application.logMessageReceived` → `LogManager` → 写入文件

**初始化顺序（必须按序）：**
```csharp
var config = new LogConfig();            // 1. 构造配置（链式可选调整）
Logger.Initialize(config);              // 2. 初始化文件系统
Logger.SetAllIsLog(true);               // 3. 设置日志级别开关
Logger.EnableUnityLogCapture(true);     // 4. 开启 Unity 日志捕获
```

**日志调用规范：**
- 业务代码**只使用** `Logger`，禁止直接调用 `UnityEngine.Debug` 或旧的 `Debugger` 类
- 提供 `Log` / `LogInfo` / `LogDebug` / `LogWarning` / `LogError` / `LogException` / `LogFatal`
- 格式化版本加 `F` 后缀：`LogDebugF` / `LogInfoF` / `LogWarningF` / `LogErrorF` / `LogFatalF`
- 程序退出前必须调用 `Logger.Close()` 确保日志落盘

### Event System
Uses `GameEvent` class for decoupled communication between systems.

## Development Commands

### Building
Open the project in Unity Editor 2022.3.35f1c1. The project uses Visual Studio solution files:
- `com.spacetime.core.sln` - Main solution file

### Code Location
- Game logic scripts: `Assets/Scripts/`
- Core framework: `Packages/com.spacetime.core/Runtime/Scripts/`
- Editor tools: `Packages/com.spacetime.core/Editor/Scripts/`

## Custom Skills

This project has several custom Claude Code skills configured in `.claude/skills/`:

- **lr_code_review** - Unity C# code review with project-specific naming conventions (camelCase properties, m_ prefix for private fields)
- **lr_lua_hotfix** - XLua hotfix generation tool for generating Lua patches from SVN changes
- **feature-scaffold** - Feature scaffolding generator
- **scaffold-generator** - General scaffold code generator
- **ui-script-generator** - UI script generator
- **lr_code_debug** - Bug troubleshooting skill

## Coding Conventions

### Naming

| 类别 | 规则 | 示例 |
|------|------|------|
| 私有实例字段 | `m_` 前缀 + PascalCase | `m_SocketClient`, `m_Writer` |
| 私有/内部静态字段 | `s_` 前缀 + PascalCase | `s_Instance`, `s_Manager` |
| 属性 | camelCase | `onConnectEvent`, `flatBufferBuilder` |
| 公共方法 | PascalCase | `DoInit`, `SendConnect`, `EnableUnityLogCapture` |
| 命名空间 | 核心框架 `ST.Core`；子模块追加后缀 | `ST.Core.Network`, `ST.Core.Logging`, `ST.Core.Table` |

### Access Modifiers

**Omit `private` keyword for private members** — it's the default and adds no value:

```csharp
// Correct
static LogManager s_Manager;
FileLogWriter m_Writer;
void DoSomething() { }

// Wrong — do not write explicit private
private static LogManager s_Manager;
private FileLogWriter m_Writer;
private void DoSomething() { }
```

### Null Guard Style

Null checks that immediately return must use the **two-line** form — condition on one line, `return` indented on the next, no curly braces.

**Always add a blank line after the return statement before continuing with code:**

```csharp
// Correct
if (m_Manager == null)
    return;

m_Manager.DoSomething();

// Wrong — missing blank line
if (m_Manager == null)
    return;
m_Manager.DoSomething();

// Wrong — do not use single-line form
if (m_Manager == null) return;

// Wrong — do not use braces
if (m_Manager == null)
{
    return;
}
m_Manager.DoSomething();
```

### Null-Conditional Operator (`?.`) — Forbidden

Never use the `?.` null-conditional operator. Always replace it with an explicit `if` null check:

```csharp
// Correct
if (m_Manager == null)
    return;
m_Manager.Flush();

// Wrong — do not use
m_Manager?.Flush();
```

### XML Documentation Comments

所有 `public` 及内部关键成员必须有 XML 文档注释，使用**中文**描述：

```csharp
/// <summary>
/// 根据配置初始化格式化器和文件写入器
/// </summary>
/// <param name="config">日志配置</param>
public void Initialize(LogConfig config) { ... }
```

单行字段注释使用行内形式：
```csharp
/// <summary>日志写入器，负责将格式化后的日志持久化</summary>
FileLogWriter m_Writer;
```

### Logging — 禁止直接使用 `UnityEngine.Debug`

业务代码统一通过 `Logger` 静态类输出日志：

```csharp
// Correct
Logger.Log("info message");
Logger.LogWarning("warning");
Logger.LogError("error");
Logger.LogDebug("debug");
Logger.LogException(ex);
Logger.LogFatal("fatal");
Logger.LogDebugF("value = {0}", value);   // 格式化版本

// Wrong — 禁止直接调用
UnityEngine.Debug.Log("...");
UnityEngine.Debug.LogError("...");
```

### Section Dividers（代码段分割）

在较长文件中用统一的注释分隔逻辑段：

```csharp
// ──────────────────────────────────────────
// 初始化 / 文件系统
// ──────────────────────────────────────────
```

```csharp
// ══════════════════════════════════════════
// 私有嵌套实现类，仅外层类可访问
// ══════════════════════════════════════════
```

## Key Dependencies

- Unity 2022.3.35f1c1
- Universal Render Pipeline (URP) 14.0.11
- TextMeshPro 3.0.6
- Unity AI Navigation 1.1.5
- Google Protocol Buffers (embedded)
- FlatBuffers (embedded)
