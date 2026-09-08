# LaunchModule 启动框架

## 目标

LaunchModule 是 com.spacetime.core 包内可复用的启动流程骨架。新游戏只需要引用该包，
在启动场景显式挂载 GameLauncher，即可获得统一的初始化、逐帧驱动和关闭生命周期。

启动入口关系如下：

~~~text
启动场景
  └─ GameLauncher（MonoBehaviour，显式挂载）
      └─ GameApplication（游戏运行入口）
          └─ LaunchModule（启动模块）
              └─ LaunchNode（节点树）
~~~

## 为什么保留 GameApplication

GameLauncher 负责适配 Unity 的 Awake、Update、LateUpdate、OnDestroy 回调，
不应该直接承担业务流程。GameApplication 是纯 C# 的生命周期边界，负责持有唯一的
LaunchModule，便于单元测试、非场景环境运行和后续替换启动入口。

因此建议保留三层职责：

- GameLauncher：只做场景挂载和 Unity 生命周期转发。
- GameApplication：只做应用级初始化、更新、关闭和模块持有。
- LaunchModule：只构建并驱动启动节点树。

## 节点模型

模块内部由节点类组成，每个具体节点单独一个 .cs 文件，位于：

~~~text
Runtime/Scripts/Launch/Nodes/
~~~

基础节点：

- LaunchNode：节点生命周期和状态机。
- SequenceNode：按顺序执行子节点。
- ParallelNode：并行执行子节点，全部成功后完成。

具体启动节点当前提供空实现，用于固定流程和接入边界。每个节点启动后立即成功，
后续再把真实异步逻辑填入对应文件：

~~~text
WaitFrame
AppInit
OpenLaunchShell
SplashGlobalResLoad
DownloadServerConfig
TableLoad
CheckDownloadServerConfigSuccess
BeforeLoginGlobalResLoad
DownloadServerDynamic
CheckDownloadServerDynamicSuccess
SDKInit
SDKInitCheck
CheckVersion
Update
ReloadRes
CompileShader
CheckSubpackUpdate
LaunchGC
OpenLoginPanel
SDKLogin
End
~~~

## 标准启动顺序

LaunchModule 默认构建以下流程：

1. 等待一帧，完成基础应用初始化。
2. 并行执行启动壳、首屏全局资源、服务器配置下载和表加载。
3. 检查服务器配置，再执行登录前资源和动态资源下载。
4. 初始化并检查 SDK，检查版本并执行更新。
5. 重载资源、编译 Shader、检查分包更新并执行启动 GC。
6. 打开登录界面，执行 SDK 登录，进入 End。

网络系统的接入边界是 DownloadServerConfig、DownloadServerDynamic、
SDKInit、SDKInitCheck 和 SDKLogin 节点。资源系统的接入边界是
SplashGlobalResLoad、BeforeLoginGlobalResLoad、ReloadRes 和 Update 节点。
这些节点可以在不改变调度器的前提下替换为实际异步实现。

## 启动场景挂载

1. 在 Unity 中创建或打开启动场景。
2. 创建一个空对象，例如 GameLauncher。
3. 将包内 Runtime/Scripts/Application/GameLauncher.cs 挂载到该对象。
4. 将该场景设置为 Build Settings 的第一个场景。
5. 运行场景；Awake 会创建 GameApplication 并启动 LaunchModule。

不需要在业务场景中手动 new GameApplication() 或 new LaunchModule()。
如果需要测试启动模块，可以在纯 C# 测试中直接创建 LaunchModule。

## 扩展异步节点

具体节点继承 LaunchNode，把异步操作状态保存在节点字段中，在完成或失败时调用：

~~~csharp
Complete();
Fail("可读的失败原因");
~~~

推荐的异步节点模式：

~~~text
OnStart  -> 发起请求/任务，只发起一次
OnUpdate -> 检查任务状态，未完成则保持 Running
完成     -> Complete()
失败     -> Fail(error)
OnStop   -> 取消请求、释放临时句柄
~~~

进度展示不应直接写入调度器。资源下载节点可以通过包内的事件、进度服务或 UI 适配器
发布当前进度，GameLauncher 只负责驱动生命周期。

## 目录约定

~~~text
Runtime/Scripts/Application/GameApplication.cs
Runtime/Scripts/Application/GameLauncher.cs
Runtime/Scripts/Launch/LaunchModule.cs
Runtime/Scripts/Launch/LaunchNodeType.cs
Runtime/Scripts/Launch/LaunchNodeState.cs
Runtime/Scripts/Launch/Nodes/*.cs
~~~

所有公共类型使用命名空间 ST.Core.Launch。测试类型使用
ST.Core.Launch.Tests，每个测试替身或测试类也保持单文件。

## 当前阶段

当前版本的重点是固定启动树、节点边界和生命周期调度。网络请求、热更新、资源下载、
进度 UI 和 SDK 逻辑保留在对应节点中实现；接入真实系统时不需要改变
GameLauncher、GameApplication 或组合节点的公共生命周期。
