# 管理器系统

[← 返回主页](../../README.md)

## 概述

统一的逻辑模块生命周期与模块查找框架。

## 核心类型

### ILogicModule
所有逻辑模块的基类，提供标准生命周期：
- `DoInit()` - 初始化，按注册顺序调用
- `DoUpdate()` - 每帧更新，只对重写了该方法的模块调用
- `DoLateUpdate()` - 延迟更新，只对重写了该方法的模块调用
- `DoClose()` - 清理关闭，按注册逆序调用

注册后基类会注入 `m_Kernel`，模块内可直接通过它查找其它模块。

### IKernel / Kernel
模块查找入口，提供 `GetModule<T>()`、`GetModule(Type)`、`GetModule(string)` 与 `GetAllModule()`。

~~~csharp
IKernel kernel = GameWorld.gameKernel;
PostEffectsModule module = kernel.GetModule<PostEffectsModule>();
~~~

### GameWorld
持有模块表并按注册顺序初始化与驱动全部模块。模块通过 `GameLauncher.CollectBusinessModules` 注册：

~~~csharp
protected override void CollectBusinessModules(List<ILogicModule> modules)
{
    modules.Add(new LoginModule());
}
~~~

模块之间按注册顺序初始化，需要先就绪的模块要排在前面。

---
[← 返回主页](../../README.md)
