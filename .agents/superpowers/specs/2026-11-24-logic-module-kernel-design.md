# 逻辑模块内核（Kernel / ILogicModule）改造设计

- 日期 2026-11-24
- 状态 已实现，待 Unity 编辑器内验证
- 目标包 com.spacetime.core
- 参考 F:/proj_se/develop/client/SoulEngine

## 1. 背景

旧工程用 `IKernel` 作为模块查找入口，业务侧先拿到内核，再按类型取模块。

```csharp
m_Kernel = GameWorld.gameKernel;
m_PostEffectsModule = m_Kernel.GetModule<PostEffectsModule>();
```

调用位置见 `project/Assets/Script/Main/LingRenWarp/LingRenWarpSoulEngine.cs:1003`。

新框架 `com.spacetime.core` 原来只有 `IManager` 基类和 `GameWorld` 的模块列表，没有类型查找能力，业务侧拿不到已初始化的模块。这次把旧工程的模块容器与查找契约搬进核心包。

## 2. 旧实现核对

### 2.1 对象职责

| 类型 | 职责 | 位置 |
| --- | --- | --- |
| `ILogicModule` | 模块基类，持有 `protected IKernel m_Kernel`，生命周期为 `OnInit/OnUpdate/OnPreLateUpdate/OnLateUpdate/OnClose/OnReset/OnBackground/OnForeground` | `SoulEngine/ILogicModule.cs:5` |
| `TLogicModule<T>` | 带 `public static T S = new T()` 的单例模块基类 | `SoulEngine/TLogicModule.cs` |
| `IKernel` | 模块查找与世界接口 | `SoulEngine/IKernel.cs:149` |
| `Kernel` | `IKernel` 实现，`GetModule` 重载转发给 `LogicModuleSet` | `SoulEngine/Kernel.cs:1520` |
| `LogicModuleSet` | 真正的注册表，维护列表、类型字典、名称字典与逐帧子列表 | `SoulEngine/LogicModuleSet.cs:37` |
| `GameWorld` | 持有 `Kernel` 与 `LogicModuleSet`，负责启动和生命周期驱动 | `SoulEngine/GameWorld.cs:55` |

### 2.2 注册与查找的关键行为

1. 模块由 `GameLauncher` 构造后以数组传入 `GameWorld.Start`，数组顺序就是初始化顺序（`project/Assets/Script/Main/GameLauncher.cs:248`）。
2. `LogicModuleSet.AddModule` 调用 `module.SetKernel(m_Kernel)`，模块由此拿到内核。
3. 类型字典按运行时类型索引，`GetModule<T>` 只命中原类型，派生模块不会被基类查询命中。
4. 名称字典用 `module.ToString()` 作键，默认值是完整类型名。
5. 注册时用反射判断 `OnUpdate` / `OnPreLateUpdate` / `OnLateUpdate` 是否被重写，只有重写过的模块进入逐帧列表。
6. 生命周期逐模块 try/catch，单个模块异常不会中断其它模块。

## 3. 改造后的新结构

| 文件 | 内容 |
| --- | --- |
| `Runtime/Scripts/Core/Manager/ILogicModule.cs` | 模块基类，注入 `m_Kernel`，四个虚生命周期方法 |
| `Runtime/Scripts/Core/Manager/TLogicModule.cs` | `S` 单例模块基类 |
| `Runtime/Scripts/Core/Kernel/IKernel.cs` | 模块查找接口 |
| `Runtime/Scripts/Application/Kernel.cs` | 最小内核实现，转发给 `GameWorld` |
| `Runtime/Scripts/Application/GameWorld_Define.cs` | 变量定义，集中内核、模块表、逐帧列表与运行状态 |
| `Runtime/Scripts/Application/GameWorld.cs` | 构造、初始化、帧驱动、关闭 |
| `Runtime/Scripts/Application/GameWorld_Module.cs` | 注册、查找、遍历、异常隔离 |

## 4. 关键决策

### 4.1 Kernel 只实现查找层

旧 `IKernel` 包含两百多个业务方法，覆盖登录、寻路、世界对象、命令、日志、时间与网络统计。本次只实现模块机制必需的部分。

- `GetModule<T>()`
- `GetModule(Type)`
- `GetModule(string)`
- `GetAllModule()`

业务接口随对应模块迁移时再加进 `IKernel` 与 `Kernel`，核心包不需要提前依赖网络层与世界层。

### 4.2 保留 Do* 命名

新框架现有模块、演示脚本和测试都直接调用 `DoInit/DoUpdate/DoLateUpdate/DoClose`，基类沿用这四个名字并改成 `virtual`。没有重写的生命周期不会进入逐帧列表。

迁移旧模块时按下列方式改名。

| 旧写法 | 新写法 |
| --- | --- |
| `protected override void OnInit()` | `public override void DoInit()` |
| `protected override void OnUpdate()` | `public override void DoUpdate()` |
| `protected override void OnLateUpdate()` | `public override void DoLateUpdate()` |
| `protected override void OnClose()` | `public override void DoClose()` |

### 4.3 静态 gameKernel

`GameWorld.gameKernel` 与旧工程同名同语义，返回最后创建的游戏世界内核。多个世界并存时以最后创建的为准。

### 4.4 名称查找兼容

名称字典同时登记 `Type.Name` 与 `Type.FullName`，兼容旧代码里用完整类型名查找的写法。

## 5. 业务接入

注册模块。

```csharp
protected override void CollectBusinessModules(List<ILogicModule> modules)
{
    modules.Add(new PostEffectsModule());
    modules.Add(new AudioModule());
}
```

获取模块。

```csharp
IKernel kernel = GameWorld.gameKernel;
PostEffectsModule postEffectsModule = kernel.GetModule<PostEffectsModule>();
```

模块内查找其它模块。

```csharp
public class CameraModule : ILogicModule
{
    PostEffectsModule m_PostEffectsModule;

    public override void DoInit()
    {
        m_PostEffectsModule = m_Kernel.GetModule<PostEffectsModule>();
    }
}
```

被依赖的模块必须排在前面，因为 `DoInit` 按注册顺序执行。

## 6. 验证

- 用 Unity 2022.3.35f1 自带 Roslyn 编译 `Runtime/Scripts` 全部源码，0 错误。
- `Tests/Editor/GameWorldTest.cs` 新增两个用例，覆盖模块查找、内核注入与未注册查找返回 null。
- Unity Test Runner 需要在编辑器内执行，本次未运行。

## 7. 非目标

- 不迁移 `IKernel` 的业务方法。
- 不迁移 `LogicEvent` / `AddClassHook` / `AddCommandHook` 等模块事件系统。
- 不迁移 `LogicCreatorSet`、`SceneFactory`、`ServerData` 等世界层对象。
- 不迁移 `OnReset` / `OnBackground` / `OnForeground` / Lua 生命周期。
