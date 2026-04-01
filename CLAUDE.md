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

Based on the code review skill configuration:
- **Private fields**: Use `m_` prefix with PascalCase (e.g., `m_SocketClient`, `m_Manager`)
- **Static fields**: Use `s_` prefix (e.g., `s_Instance`)
- **Properties**: Use camelCase (e.g., `onConnectEvent`)
- **Methods**: Use PascalCase (e.g., `DoInit`, `SendConnect`)
- **Namespace**: Core framework uses `ST.Core` namespace

### Access Modifiers

**Omit `private` keyword for private members** — it's the default and adds no value:

```csharp
// Correct
static ILogManager s_Manager;
ILogWriter m_Writer;
void DoSomething() { }

// Wrong — do not write explicit private
private static ILogManager s_Manager;
private ILogWriter m_Writer;
private void DoSomething() { }
```

### Null Guard Style

Null checks that immediately return must use the **two-line** form — condition on one line, `return` indented on the next, no curly braces.

**Always add a blank line after the return statement before continuing with code:**

```csharp
// Correct
if (manager == null)
    return;

manager.DoSomething();

// Wrong — missing blank line
if (manager == null)
    return;
manager.DoSomething();

// Wrong — do not use single-line form
if (manager == null) return;

// Wrong — do not use braces
if (manager == null)
{
    return;
}
manager.DoSomething();
```

### Null-Conditional Operator (`?.`) — Forbidden

Never use the `?.` null-conditional operator. Always replace it with an explicit `if` null check:

```csharp
// Correct
if (manager == null)
    return;
manager.Flush();

// Wrong — do not use
manager?.Flush();
```

## Key Dependencies

- Unity 2022.3.35f1c1
- Universal Render Pipeline (URP) 14.0.11
- TextMeshPro 3.0.6
- Unity AI Navigation 1.1.5
- Google Protocol Buffers (embedded)
- FlatBuffers (embedded)
