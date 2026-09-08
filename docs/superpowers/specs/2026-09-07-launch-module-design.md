# LaunchModule 启动骨架设计

## 目标

在 `com.spacetime.core` 运行时程序集中提供可由启动场景显式挂载的启动入口。入口通过
`GameLauncher → GameApplication → LaunchModule` 启动一棵节点树。首版只实现节点调度骨架，
具体业务节点执行后立即成功完成，不实现资源下载、版本检查、SDK、登录或进度界面业务。

## 范围

- `GameLauncher` 负责承接 Unity 生命周期。
- `GameApplication` 负责持有并驱动 `LaunchModule`。
- `LaunchModule` 负责构造和启动标准启动节点树。
- 每个具体节点类独立放在一个 `.cs` 文件中。
- 顺序节点、并行节点与基础节点具备可运行的最小调度能力。
- 节点失败时停止后续调度并记录错误；当前空节点默认成功。
- 所有新增 C# 文件使用 UTF-8 BOM、LingRen 命名规则、中文 XML 注释和项目日志入口。

## 不在范围内

- 热更新清单解析、文件比较、HTTP 下载、断点续传和文件校验。
- 真实的启动进度 UI、SDK 初始化、登录、表格加载和资源重载。
- Character Explorer、二维码登录、测试入口、单实例和移动平台专用分支。
- 通用依赖注入容器、反射发现节点或可视化节点编辑器。

## 文件结构

```text
Runtime/Scripts/Application/
├── GameLauncher.cs
└── GameApplication.cs

Runtime/Scripts/Launch/
├── LaunchModule.cs
├── LaunchNodeType.cs
├── LaunchNodeState.cs
└── Nodes/
    ├── LaunchNode.cs
    ├── SequenceNode.cs
    ├── ParallelNode.cs
    ├── WaitFrameNode.cs
    ├── AppInitNode.cs
    ├── OpenLaunchShellNode.cs
    ├── SplashGlobalResLoadNode.cs
    ├── DownloadServerConfigNode.cs
    ├── TableLoadNode.cs
    ├── CheckDownloadServerConfigSuccessNode.cs
    ├── BeforeLoginGlobalResLoadNode.cs
    ├── DownloadServerDynamicNode.cs
    ├── CheckDownloadServerDynamicSuccessNode.cs
    ├── SDKInitNode.cs
    ├── SDKInitCheckNode.cs
    ├── CheckVersionNode.cs
    ├── UpdateNode.cs
    ├── ReloadResNode.cs
    ├── CompileShaderNode.cs
    ├── CheckSubpackUpdateNode.cs
    ├── LaunchGCNode.cs
    ├── OpenLoginPanelNode.cs
    ├── SDKLoginNode.cs
    └── EndNode.cs

Tests/Editor/
├── com.spacetime.core.tests.asmdef
├── LaunchNodeTest.cs
└── LaunchModuleTest.cs
```

Unity 为新增目录、脚本和程序集定义生成的 `.meta` 文件必须一并纳入版本控制。

## 公共契约

所有新增运行时代码使用 `ST.Core.Launch` 命名空间。

- `GameLauncher`：`public sealed class`，继承 `MonoBehaviour`，由启动场景显式挂载。
- `GameApplication`：`public sealed class`，继承 `IManager`；无参构造。
- `LaunchModule`：`public sealed class`，继承 `IManager`；无参构造。
- `LaunchNode`：`public abstract class`；构造参数为非空节点名称。
- `SequenceNode`、`ParallelNode`：`public sealed class`；构造参数为节点名称，通过
  `Append(LaunchNode node)` 注册子节点。
- 所有具体业务节点：`public sealed class`；无参构造。

`LaunchNode` 公开只读属性 `name`、`state`、`error`、`isDone` 和 `isSucceeded`，公开方法
`Start()`、`DoUpdate()`、`Stop()`。`LaunchModule` 公开只读属性 `state`、`error`、`isDone`
和 `isSucceeded`，以及启动入口 `Launch()`。`GameApplication` 公开只读属性
`launchModule`，便于宿主读取启动状态。`launchModule` 在初始化前以及模块构造失败时返回
`null`。所有 `error` 属性在无错误时返回 `string.Empty`，生命周期内不返回 `null`。

`LaunchNodeType + LaunchModule.CreateNode` 是用户指定的参考结构：节点树按枚举描述，并通过
显式映射创建每个独立文件中的节点类。这样后续调整启动顺序只修改 `LaunchModule`，不会把节点
实例构造散落到流程定义中。`CreateNode` 是模块内部实现，不作为业务公开扩展点。

## 组件设计

### GameLauncher

`GameLauncher` 是启动场景中显式挂载的 `MonoBehaviour`。`Awake` 创建并初始化
`GameApplication`，`Update` 和 `LateUpdate` 转发帧生命周期，`OnDestroy` 关闭应用。
它不创建具体节点、不保存业务模块列表，也不包含热更新逻辑。

重复初始化或销毁后收到帧回调时直接忽略，保证 Unity 生命周期边界安全。

### GameApplication

`GameApplication` 是纯 C# 生命周期协调器，内部只持有一个 `LaunchModule`。它公开：

- `DoInit()`：创建 `LaunchModule`，依次调用它的 `DoInit()` 和 `Launch()`。
- `DoUpdate()`：驱动 `LaunchModule`。
- `DoLateUpdate()`：预留滞后帧驱动。
- `DoClose()`：委托 `LaunchModule.DoClose()` 停止节点树，但保留模块引用供状态查询。

同一实例只允许初始化一次。`DoInit()` 在已初始化或已关闭时直接返回。`LaunchModule` 自己
捕获节点树构造和启动异常并暴露失败状态，因此 `GameApplication` 不重复捕获或记录同一错误。
关闭操作可重复调用。初始化前或关闭后的 `DoUpdate()`、`DoLateUpdate()` 均直接返回。

### LaunchNode

`LaunchNode` 是所有节点的抽象基类，包含节点名称、状态和错误信息。状态为：

- `Ready`：尚未启动。
- `Running`：正在执行。
- `Succeeded`：成功完成。
- `Failed`：失败完成。
- `Stopped`：被外部终止。

公共生命周期为 `Start()`、`DoUpdate()` 和 `Stop()`。派生类通过 `OnStart()`、`OnUpdate()`、
`OnStop()` 实现行为，并通过 `Complete()` 或 `Fail(string error)` 结束。空业务节点仅在
`OnStart()` 中调用 `Complete()`。

状态转换规则：

| 当前状态 | `Start()` | `DoUpdate()` | `Stop()` |
|---|---|---|---|
| `Ready` | 进入 `Running` 并调用 `OnStart()` | 忽略 | 进入 `Stopped`，不调用 `OnStop()` |
| `Running` | 忽略 | 调用 `OnUpdate()` | 调用 `OnStop()` 后进入 `Stopped` |
| `Succeeded` | 忽略 | 忽略 | 忽略 |
| `Failed` | 忽略 | 忽略 | 忽略 |
| `Stopped` | 忽略 | 忽略 | 忽略 |

`OnStart()` 或 `OnUpdate()` 抛出的异常由基类捕获，节点进入 `Failed`，`error` 保存
`Exception.ToString()`。`OnStop()` 抛出的异常通过 `Logger.LogException` 记录，节点仍进入
`Stopped`，保证关闭流程不会中断。失败节点不能重试；重试需创建新的节点树。

`SequenceNode.Append()` 与 `ParallelNode.Append()` 收到 `null` 时抛出
`ArgumentNullException`；节点开始运行后不再允许追加子节点，并抛出 `InvalidOperationException`。

### SequenceNode

按追加顺序逐个启动子节点。当前子节点成功后，在同一帧继续启动下一个节点；子节点失败时，
顺序节点立即失败并保留子节点错误。所有子节点成功后顺序节点成功。

### ParallelNode

启动时按追加顺序启动子节点。若某个子节点在 `Start()` 中同步失败，不再启动它后面的节点，
并停止此前已启动且仍在运行的节点。每帧只驱动仍在运行的子节点；任一子节点异步失败时，
并行节点失败并停止其他运行节点；全部成功时并行节点成功。空并行节点直接成功。

### LaunchModule

`LaunchModule` 使用显式 `switch` 将 `LaunchNodeType` 映射到具体节点，不使用反射。标准流程为：

```text
WaitFrame
→ AppInit
→ Parallel Splash
   ├── OpenLaunchShell
   ├── SplashGlobalResLoad
   ├── DownloadServerConfig
   └── TableLoad
→ CheckDownloadServerConfigSuccess
→ BeforeLoginGlobalResLoad
→ DownloadServerDynamic
→ CheckDownloadServerDynamicSuccess
→ SDKInit
→ SDKInitCheck
→ CheckVersion
→ Update
→ ReloadRes
→ CompileShader
→ CheckSubpackUpdate
→ LaunchGC
→ OpenLoginPanel
→ SDKLogin
→ End
```

`WaitFrameNode` 是唯一不立即完成的首版节点：启动后的下一次 `DoUpdate()` 才成功，用于证明节点树
确实由 Unity 帧循环驱动。其他具体节点均为空实现并立即成功。

`LaunchModule.DoInit()` 是为遵循现有 `IManager` 契约提供的无操作初始化入口；它不创建节点树。
`GameApplication` 固定先调用 `DoInit()`，再调用 `Launch()`。`DoLateUpdate()` 同样是
`IManager` 所要求的无操作入口。

`LaunchModule.Launch()` 只在首次调用时构造并启动节点树，重复调用直接忽略。`DoUpdate()` 在
根节点运行时驱动它；启动前、完成后或关闭后调用均忽略。`DoClose()` 停止运行中的根节点并
可重复调用。节点基类只保存错误，最终由
`LaunchModule` 在首次观察到根节点失败时通过非可变参数的 `Logger.LogError` 统一记录一次，
避免重复日志，也避免在 `DoUpdate()` 中产生 `params object[]` 分配。

模块状态使用 `LaunchNodeState`，转换规则如下：

| 当前状态 | `Launch()` | `DoUpdate()` | `DoClose()` |
|---|---|---|---|
| `Ready` | 构造并启动根节点，进入 `Running` 或同步完成状态 | 忽略 | 进入 `Stopped` |
| `Running` | 忽略 | 驱动根节点并同步其最终状态 | 停止根节点并进入 `Stopped` |
| `Succeeded` | 忽略 | 忽略 | 保留 `Succeeded` |
| `Failed` | 忽略 | 忽略 | 保留 `Failed` |
| `Stopped` | 忽略 | 忽略 | 忽略 |

`Launch()` 捕获节点树构造异常，将模块置为 `Failed`，保存非空错误文本并通过
`Logger.LogException` 记录。根节点未创建时模块直接报告失败，不进入帧驱动。私有
`CreateNode` 仅接收标准树中列出的有效枚举值；无效枚举不属于公开输入契约。

## 数据流与所有权

- `GameLauncher` 独占 `GameApplication`。
- `GameApplication` 独占 `LaunchModule`。
- `LaunchModule` 独占根 `SequenceNode`。
- 组合节点独占其子节点列表。
- `DoClose()` 从根节点向下停止运行中的节点；保留根引用用于读取最终状态，不依赖静态单例。

节点仅在启动时分配，帧更新期间不创建列表、不使用 LINQ、不构造日志字符串，避免持续 GC。

## 错误处理

- 重复启动节点不会重新执行。
- 节点异常由节点基类捕获，转换为 `Failed` 状态并保存异常消息。
- 组合节点传播第一个失败原因并停止剩余运行节点。
- `GameLauncher` 销毁时无论启动是否完成都安全停止节点树。

## 测试

- 顺序节点按追加顺序执行并完成。
- 顺序节点遇到失败后不启动后续节点。
- 并行节点在全部子节点完成后成功。
- 并行节点传播失败并停止其余节点。
- 并行节点遇到同步失败时不启动后续节点。
- 组合节点拒绝空子节点和启动后的追加操作。
- `WaitFrameNode` 不在 `Start()` 中完成，而在一次 `DoUpdate()` 后完成。
- `LaunchModule` 在调用 `Launch()` 后进入运行状态，并在帧更新后完成标准空节点链。
- 节点回调异常被转换为失败状态，停止回调异常不打断关闭。
- 重复 `Start()`、重复 `Launch()` 和重复 `DoInit()` 不重复执行。
- 初始化前、完成后及关闭后的帧更新不改变状态。
- 运行中关闭会停止根节点，重复关闭不抛异常。
- `GameApplication` 初始化后能驱动启动模块，重复关闭不抛异常。

测试使用 Unity EditMode：`Tests/Editor/com.spacetime.core.tests.asmdef` 设置
`includePlatforms: ["Editor"]`、`optionalUnityReferences: ["TestAssemblies"]`，并引用
`com.spacetime.core.runtime`。测试不进入 PlayMode，不依赖场景或真实网络。

## 完成标准

- 所列类均独立成文件并纳入 `com.spacetime.core.runtime` 程序集。
- Unity EditMode 测试程序集能够编译并执行节点调度测试。
- 新增测试全部通过。
- 新增目录、脚本、测试及程序集定义均有对应 `.meta` 文件。
- 新增 C# 文件均为 UTF-8 BOM。
- 新增代码通过 LingRen C# 代码规范与 `csharp_gc.md` 检查。
