# LaunchModule 实施计划

## 1. 目标与边界

目标是在 com.spacetime.core 包中提供一套可复用的游戏启动骨架。新游戏引用包后，
只需在启动场景显式挂载 GameLauncher，即可驱动完整启动流程。

本计划只定义公共边界、节点顺序和验证方式。热更新、资源下载、服务器配置、SDK
和进度 UI 的真实业务实现由各游戏在对应节点中接入。

## 2. 分层设计

~~~text
GameLauncher（Unity 场景组件）
    ↓ Unity 生命周期转发
GameApplication（纯 C# 应用入口）
    ↓ 持有唯一模块
LaunchModule（启动流程模块）
    ↓ 构建并驱动
LaunchNode 树（Sequence / Parallel / 具体节点）
~~~

### GameLauncher

文件：Runtime/Scripts/Application/GameLauncher.cs

- 继承 MonoBehaviour。
- 在 Awake 创建并初始化 GameApplication。
- 在 Update、LateUpdate 中转发帧调用。
- 在 OnDestroy 中关闭应用。
- 不包含网络、资源、SDK 或业务判断。

### GameApplication

文件：Runtime/Scripts/Application/GameApplication.cs

- 继承包内 IManager。
- 持有唯一的 LaunchModule。
- 保证初始化、更新和关闭操作幂等。
- 允许纯 C# 测试直接创建，不依赖 Unity 场景。

### LaunchModule

文件：Runtime/Scripts/Launch/LaunchModule.cs

- 构建标准启动树。
- 通过显式 CreateNode(LaunchNodeType) 工厂创建节点。
- 驱动根节点并同步模块状态。
- 统一记录启动树失败信息。
- 不直接实现网络请求或资源加载细节。

## 3. 节点目录和单文件约束

所有节点位于 Runtime/Scripts/Launch/Nodes，每个具体节点一个 .cs 文件。

基础节点：

- LaunchNode：生命周期状态机、异常边界、成功/失败/停止转换。
- SequenceNode：按顺序执行子节点。
- ParallelNode：并行执行子节点，全部成功后完成。

具体节点：

~~~text
WaitFrameNode
AppInitNode
OpenLaunchShellNode
SplashGlobalResLoadNode
DownloadServerConfigNode
TableLoadNode
CheckDownloadServerConfigSuccessNode
BeforeLoginGlobalResLoadNode
DownloadServerDynamicNode
CheckDownloadServerDynamicSuccessNode
SDKInitNode
SDKInitCheckNode
CheckVersionNode
UpdateNode
ReloadResNode
CompileShaderNode
CheckSubpackUpdateNode
LaunchGCNode
OpenLoginPanelNode
SDKLoginNode
EndNode
~~~

当前阶段的具体节点是基础空实现：启动后立即成功；WaitFrameNode 在首个更新帧完成。
后续业务接入不得把多个节点合并到一个文件。

## 4. 节点状态契约

状态定义：Ready、Running、Succeeded、Failed、Stopped。

- Start 只允许从 Ready 调用一次。
- DoUpdate 只驱动 Running 节点。
- Stop 对 Ready 直接停止，对 Running 调用停止钩子后停止。
- 终态节点忽略重复生命周期调用。
- OnStart、OnUpdate 异常转为 Failed 并保存异常文本。
- OnStop 异常必须记录日志，但不得逃逸，节点最终仍为 Stopped。
- Fail 收到空错误文本时使用默认错误信息。

## 5. 标准启动树

根节点名称为 LaunchNode，标准顺序如下：

1. WaitFrame
2. AppInit
3. Splash 并行组
   - OpenLaunchShell
   - SplashGlobalResLoad
   - DownloadServerConfig
   - TableLoad
4. CheckDownloadServerConfigSuccess
5. BeforeLoginGlobalResLoad
6. DownloadServerDynamic
7. CheckDownloadServerDynamicSuccess
8. SDKInit
9. SDKInitCheck
10. CheckVersion
11. Update
12. ReloadRes
13. CompileShader
14. CheckSubpackUpdate
15. LaunchGC
16. OpenLoginPanel
17. SDKLogin
18. End

WaitFrame 保证启动流程至少跨越一个 Unity 更新帧；其余占位节点当前同步完成。

## 6. 异步业务接入顺序

每个异步节点遵循统一模式：

~~~text
OnStart  -> 发起一次请求或任务
OnUpdate -> 检查任务状态
Running  -> 任务未完成，保持运行
Complete -> 成功结束
Fail     -> 保存可读错误并结束
OnStop   -> 取消请求、释放临时资源
~~~

接入顺序建议：

1. 先接入 DownloadServerConfig，建立服务器配置和错误重试策略。
2. 接入 SplashGlobalResLoad、TableLoad，提供首屏所需资源。
3. 接入 DownloadServerDynamic 和 BeforeLoginGlobalResLoad，完成热更新资源准备。
4. 接入 CheckVersion、Update、ReloadRes，完成版本更新和资源切换。
5. 接入 SDKInit、SDKInitCheck、SDKLogin。
6. 最后接入 OpenLoginPanel、进度展示和登录后跳转。

网络层和资源层通过节点接入，不修改 GameLauncher 或组合节点的生命周期代码。

## 7. 进度展示方案

进度状态由资源/网络节点产生，由独立进度服务或事件适配器发布。UI 只订阅进度，
不反向控制节点树。

建议进度数据至少包含：

- 当前阶段名称。
- 阶段内完成比例。
- 总体完成比例。
- 当前速度或剩余时间（如果底层可提供）。
- 可展示给用户的错误信息。

节点完成或失败后应停止继续发布旧进度，避免 UI 显示过期状态。

## 8. 启动场景接入

1. 创建启动场景。
2. 创建空对象并命名为 GameLauncher。
3. 挂载 Runtime/Scripts/Application/GameLauncher.cs。
4. 将启动场景设置为 Build Settings 第一个场景。
5. 运行场景，确认 Awake 创建应用并进入 LaunchModule。

业务场景不需要手动 new GameApplication 或 new LaunchModule。

## 9. 测试计划

测试程序集：Tests/Editor/com.spacetime.core.tests.asmdef

测试覆盖：

- LaunchNodeTest：构造校验、状态转换、幂等、异常和停止。
- SequenceNodeTest：顺序启动、同步推进、等待和失败停止。
- ParallelNodeTest：启动顺序、并行等待、同步失败和停止传播。
- BootstrapLaunchNodeTest：首批节点名称与立即成功。
- RemainingLaunchNodeTest：其余节点一类一测。
- LaunchModuleTest：工厂映射、根树顺序和模块终态。
- GameApplicationTest：应用生命周期与 GameLauncher 类型边界。

每次执行 Unity 测试时，结果目录放在项目外部；提交前要求测试退出码为 0、失败数为 0。

## 10. 验收清单

- [ ] 新游戏只引用包并挂载 GameLauncher 即可启动。
- [ ] GameApplication 和 LaunchModule 均可脱离场景进行单元测试。
- [ ] 每个具体节点类均为独立文件。
- [ ] 标准启动树顺序与本计划一致。
- [ ] 组合节点可处理同步完成、异步等待、失败和停止。
- [ ] 网络、热更新、资源下载、SDK 和进度展示均有明确节点接入点。
- [ ] 所有新增 C# 文件为 UTF-8 BOM，并通过 LingRen/GC 规范检查。
- [ ] Unity 编辑器无脚本编译错误，EditMode 测试全部通过。

## 11. 后续实施批次

### 批次一：基础调度器

实现 LaunchNodeState、LaunchNode、SequenceNode、ParallelNode，先完成纯 C# 状态机测试。

### 批次二：标准节点与模块

按节点单文件创建占位实现，加入 LaunchNodeType、LaunchModule 和标准树测试。

### 批次三：应用入口

加入 GameApplication、场景挂载组件 GameLauncher，验证 Unity 生命周期转发。

### 批次四：真实异步业务

按第 6 节顺序把网络、热更新、资源、SDK 和进度服务接入具体节点，
保持公共调度器接口不变。
