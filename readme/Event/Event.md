# 事件系统

[← 返回主页](../../README.md)

## 概述

解耦的事件通信机制，用于系统间消息传递。核心实现位于 `Runtime/Scripts/Core/Delegate/`。

## 核心类

### RunEvent

事件类，提供事件注册、触发和注销功能，配合 `Run` 委托使用：

```csharp
RunEvent onLoaded = new RunEvent();
onLoaded.AddListener(OnLoaded);
onLoaded.Invoke();
onLoaded.RemoveListener(OnLoaded);
```

- 同一回调重复注册会自动去重。
- 触发时单个回调抛出异常会被 `ERunner` 捕获并记录，后续回调继续执行。
- `RunEvent<T0>` 到 `RunEvent<T0, T1, T2, T3>` 提供最多四个参数的泛型版本。

### 相关类型

| 类型 | 用途 |
|------|------|
| `Run` / `Run<T...>` | 无返回值回调委托，替代 `Action` |
| `RFunc<TResult>` | 带返回值回调委托，替代 `Func` |
| `ERunner.Run` | 安全执行 `Run`，捕获并记录异常 |
| `EFuncer.Run` | 安全执行 `RFunc`，异常时返回默认值 |

---
[← 返回主页](../../README.md)
