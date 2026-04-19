# UI 测试系统

[← 返回 UI 管理系统](UI.md)

> 版本：1.2.0 | 命名空间：`ST.Core.Test`

---

## 一、概述

`Runtime/Scripts/Test/` 提供一套**开箱即用**的 UI 测试组件，分三个层次：

| 层次 | 组件 | 触发方式 | 用途 |
|------|------|----------|------|
| GM 指令 | `TestUIBoot` + `TestGMBoxPanel` | F1 打开面板，输入指令 | 运行时手动按需打开/关闭面板及子页面 |
| 手动调试 GUI | `UIFlowTest` | **F3** 切换 | IMGUI 悬浮窗，实时状态 + 快速操作 + 逐步执行 TC |
| 自动化测试 | `UIFlowTest` | **F2** 或 `uitest` | 自动顺序执行 7 条 TC，输出 PASS / FAIL |

---

## 二、文件结构

```
Runtime/Scripts/Test/
├── TestUIID.cs             UI ID 常量表（GMBoxPanel=1 / TestPanel=2 / TestModal=3 / TestPageA=4）
├── GMBoxPanel.cs           GM Box 的 UIManager 适配层（AbstractPanel 包装）
├── TestPanel.cs            通用测试面板（AbstractPanel，Auto 层，含子页面操作方法）
├── TestModalPanel.cs       模态测试面板（hideMask=HideAndUnInteractive，Auto 层）
├── TestPageA.cs            测试子页面 A（AbstractPage，无 Canvas，挂载于 TestPanel）
├── TestGMBoxPanel.cs       全屏 IMGUI GM 指令窗口（F1 开关）
├── TestUIBoot.cs           场景引导类（初始化 UIManager + 注册 GM 指令）
└── UIFlowTest.cs           测试运行器（F2 自动 / F3 手动调试 GUI）

Editor/Scripts/Test/
└── CreateUITestPrefabEditor.cs   一键生成全部 4 个测试 Prefab

Assets/Package/ui/uiprefab/
├── ui_panel_gm_box.prefab        GM 面板（GMBoxPanel，Top 层）
├── ui_panel_test.prefab          通用测试面板（TestPanel，Auto 层）
├── ui_panel_test_modal.prefab    模态测试面板（TestModalPanel，Auto 层）
└── ui_page_test_a.prefab         测试子页面 A（TestPageA，无 Canvas）
```

---

## 三、快速启动

### 3.1 生成全部测试 Prefab（只需做一次）

菜单栏 → **ST.Core / Test / Create All Test Prefabs**

一键在 `Assets/Package/ui/uiprefab/` 下生成以下四个 Prefab：

| Prefab | 挂载组件 | 层级 | 说明 |
|--------|----------|------|------|
| `ui_panel_gm_box.prefab` | `GMBoxPanel` | Top | 打开时同步显示 IMGUI GM 窗口 |
| `ui_panel_test.prefab` | `TestPanel` | Auto | 基础开关 / 缓存 / 计数 / 子页面测试 |
| `ui_panel_test_modal.prefab` | `TestModalPanel` | Auto | 打开后下层全隐藏且不可交互 |
| `ui_page_test_a.prefab` | `TestPageA` | — | 子页面，无 Canvas，由父面板的 Canvas 渲染 |

> 也可单独生成：**ST.Core / Test / Create ui_page_test_a Prefab** 等子菜单。

**Panel Prefab 节点结构：**

```
ui_panel_xxx
├── Background    (全屏半透明蒙版)
├── TitleBar      (顶部标题条)
│   └── TitleText (Text 标题)
├── CloseBtn      (右上角关闭按钮)
│   └── Label     (Text "×")
└── Content       (内容容器)
```

**Page Prefab 节点结构（无 Canvas）：**

```
ui_page_test_a            ← 根节点：仅 RectTransform + TestPageA
├── Background            (半透明蒙版，铺满父面板)
├── TitleBar              (顶部标题条)
│   └── TitleText         (Text "TestPageA（子页面）")
├── InfoText              (居中说明文字)
└── CloseBtn              (右上角关闭按钮，点击 DettachPage)
    └── Label             (Text "×")
```

> 所有引用（TitleText / InfoText / CloseButton）均在生成时自动绑定，无需手动连线。

---

### 3.2 搭建测试场景

**Hierarchy 结构：**

```
Hierarchy
├── UIRoot              ← 挂 UIRoot.cs，配置双 Camera / Canvas / PanelRoot
├── EventSystem         ← Unity 内置（确保场景中只有一个）
└── Boot
    ├── TestUIBoot      ← 场景引导
    ├── TestGMBoxPanel  ← GM 面板（F1 开关）
    └── UIFlowTest      ← 测试运行器（F2 自动 / F3 手动 GUI）
```

**Inspector 连线：**

| 组件 | 字段 | 拖入 |
|------|------|------|
| `TestUIBoot` | `m_UIRoot` | 场景中的 `UIRoot` 组件 |
| `TestUIBoot` | `m_GMBox` | `TestGMBoxPanel` 组件 |
| `TestUIBoot` | `m_FlowTest` | `UIFlowTest` 组件（可选，连线后注册 `uitest` 指令）|
| `UIFlowTest` | `m_GMBox` | `TestGMBoxPanel` 组件（可选，留空自动查找）|

---

## 四、GM 指令测试（F1）

运行后按 **F1** 打开 IMGUI GM 面板，支持以下指令：

| 指令 | 用法示例 | 说明 |
|------|----------|------|
| `openui <uiID>` | `openui 2` | 打开指定面板 |
| `closeui <uiID>` | `closeui 2` | 关闭指定面板 |
| `closeall` | `closeall` | 关闭所有运行中的面板（DoClose + DoInit）|
| `isopen <uiID>` | `isopen 2` | 查询 IsOpened / IsVisible / IsActive |
| `listpanel` | `listpanel` | 列出已注册的 TestUIID 及层级信息 |
| `openpage` | `openpage` | 向已打开的 TestPanel 挂载子页面 A |
| `closepage` | `closepage` | 从 TestPanel 卸载子页面 A |
| `uitest` | `uitest` | 运行自动化测试（需连线 `m_FlowTest`）|
| `help` | `help` | 列出所有指令 |
| `echo <内容>` | `echo hello` | 输出文本到 GM 面板 |
| `clear` | `clear` | 清空输出区 |

键盘快捷键：`↑` / `↓` 导航历史记录，`Enter` 执行。

### TestUIID 面板 / 页面 ID 表

| ID | 常量名 | Prefab | 类型 | 层级 | 备注 |
|----|--------|--------|------|------|------|
| 1 | `GMBoxPanel` | `ui_panel_gm_box.prefab` | Panel | Top | 打开时同步显示 IMGUI GM 窗口 |
| 2 | `TestPanel` | `ui_panel_test.prefab` | Panel | Auto | 基础测试面板，可挂载子页面 |
| 3 | `TestModal` | `ui_panel_test_modal.prefab` | Panel | Auto | 模态面板，HideMask=HideAndUnInteractive |
| 4 | `TestPageA` | `ui_page_test_a.prefab` | **Page** | — | TestPanel 的子页面，无独立 Canvas |

> **Page 与 Panel 的区别**：Page 没有独立 Canvas，无法通过 `UIManager.OpenPanel` 打开，只能通过父面板的 `panelActive.AttachPage` / `DettachPage` 管理。

---

## 五、手动调试 GUI（F3）

### 5.1 开启方式

运行场景后按 **F3** 切换显示/隐藏悬浮窗；窗口可拖拽移动。

### 5.2 窗口布局

```
┌──────────── UIFlowTest 手动调试 ──────────────────────────┐
│                                  [▶ 自动全跑 F2]  [✕ 关闭]│
├───────────────────────────────────────────────────────────┤
│ ■ 面板状态                                                 │
│   ID   名称          IsOpen   Visible   Active            │
│    1   GMBoxPanel      ○        ○         ○              │
│    2   TestPanel       ●        ●         ●              │
│    3   TestModal       ○        ○         ○              │
│    4   TestPageA       ●        (Page)                   │
├───────────────────────────────────────────────────────────┤
│ ■ 快速操作                                                 │
│  [Open 1][Close 1]  [Open 2][Close 2]  [Open 3][Close 3]  │
│  [Close All]                                              │
│  Page A（需 TestPanel 已打开）：                           │
│  [Attach PageA]  [Detach PageA]                           │
├───────────────────────────────────────────────────────────┤
│ ■ 分步执行测试用例                                         │
│  [TC01][TC02][TC03][TC04][TC05][TC06][TC07]     [重置]    │
│  ─────────────────────────────────────────────────────    │
│  TC07 子页面 Attach/Detach  [3/7 步]                       │
│  ✓  1. 清理：确保 TestPanel 已打开      IsOpened=true      │
│  ✓  2. 等待 TestPanel 加载完成          FindPanel≠null     │
│  ►  3. AttachPage(TestPageA)           IsPageOpen=true [▶]│
│     4. 等待 TestPageA 加载完成          IsPageOpen=true    │
│     5. DettachPage(TestPageA)          IsPageOpen=false   │
│     6. 再次 Attach（验证缓存复用）       IsPageOpen=true    │
│     7. 清理：关闭 TestPanel             IsOpened=false     │
├───────────────────────────────────────────────────────────┤
│ ■ 操作日志                                       [清空]   │
│  [操作] AttachPage TestPageA                              │
│  [操作] DettachPage TestPageA                             │
└───────────────────────────────────────────────────────────┘
```

### 5.3 各区域说明

**面板状态区**

每帧实时刷新，绿点（`●`）= true，灰圈（`○`）= false。  
第 4 行显示 `TestPageA` 的挂载状态（仅 IsOpen 列，Page 无 Visible / Active 概念）。

**快速操作区**

- 面板行：直接调用 `UIManager.S.OpenPanel` / `ClosePanel`。
- **Page 行**：
  - `[Attach PageA]`：调用 `TestPanel.OpenPageA()`，向已打开的 TestPanel 动态挂载子页面 A。
  - `[Detach PageA]`：调用 `TestPanel.ClosePageA()`，卸载子页面 A（根据 `cacheCount` 决定是否缓存 GameObject）。
  - TestPanel 未打开时点击会在日志区输出错误提示。

**分步执行区**

| 元素 | 行为 |
|------|------|
| `[TC01]…[TC07]` | 点击切换 TC，不重置已完成步骤（可继续上次进度）|
| `[重置]` | 将当前 TC 所有步骤回到 Pending |
| 步骤状态图标 | `✓` PASS（绿）/ `✗` FAIL（红）/ `⏳` 等待加载（黄）/ `►` 当前步（蓝）|
| `[▶ 执行]` | 仅在当前待执行步骤旁出现；点击后运行 Action + Check |
| `[重试]` | 失败步骤旁出现，可单独重跑该步 |
| 全部通过 | 所有步骤完成后显示 `✓ 全部通过` 横幅 |

> 涉及异步加载的步骤（标记 `waitLoad`）在执行后自动轮询 `check()` 条件，最长等待 **5 秒**；超时则标记 FAIL。

---

## 六、自动化测试（F2）

### 6.1 触发方式

| 方式 | 操作 |
|------|------|
| 快捷键 | 运行后按 **F2** |
| GUI 按钮 | 手动调试窗口右上角 `[▶ 自动全跑 F2]` |
| GM 指令 | `uitest`（需 `m_FlowTest` 已连线）|
| 代码调用 | `m_FlowTest.RunTests()` |

### 6.2 测试用例列表

| 编号 | 名称 | 验证点 |
|------|------|--------|
| TC01 | 基础开关流程 | `OpenPanel` → `IsOpened=true` → 加载完成 → `FindPanel≠null` → `ClosePanel` → `IsOpened=false` |
| TC02 | 单例防重 | 同一 uiID 连续打开两次，返回相同 `panelID` |
| TC03 | 缓存复用 | 关闭后（进缓存）再次打开，复用同一个 `GameObject` 实例 |
| TC04 | OnOpen 计数累加 | 每次 `OpenPanel` 都触发 `OnOpen`，`openCount` 正确递增 |
| TC05 | DoClose 全关 | `UIManager.DoClose()` 后所有面板 `IsOpened=false` |
| TC06 | 查询 API 准确性 | `IsOpened` / `IsPanelVisible` / `IsPanelActive` / `SetPanelVisible` 返回值符合预期 |
| **TC07** | **子页面 Attach/Detach** | `AttachPage(TestPageA)` → `IsPageOpen=true` → `DettachPage` → `IsPageOpen=false` → 再次 Attach（缓存复用）|

### 6.3 Console 输出示例

```
╔══════════════════════════════╗
║  UIFlowTest  开始             ║
╚══════════════════════════════╝
── TC01 基础开关流程
  [PASS] TC01_基础开关
── TC02 单例防重
  [PASS] TC02_单例防重
── TC03 缓存复用
  [PASS] TC03_缓存复用
── TC04 OnOpen 计数累加
  [PASS] TC04_OpenCount累加
── TC05 DoClose 全关
  [PASS] TC05_DoClose全关
── TC06 查询 API 准确性
  [PASS] TC06_查询API
── TC07 子页面 Attach/Detach
  [PASS] TC07_子页面
──────────────────────────────
结果：7 / 7 通过
══════════════════════════════
```

失败时额外输出：
```
  ✗ Attach 后 IsPageOpen=true
  [FAIL] TC07_子页面
```

---

## 七、核心组件说明

### 7.1 TestUIID

```csharp
// Runtime/Scripts/Test/TestUIID.cs
public static class TestUIID
{
    public const int GMBoxPanel = 1;   // ui_panel_gm_box.prefab       (Panel / Top)
    public const int TestPanel  = 2;   // ui_panel_test.prefab          (Panel / Auto)
    public const int TestModal  = 3;   // ui_panel_test_modal.prefab    (Panel / Auto, HideMask)
    public const int TestPageA  = 4;   // ui_page_test_a.prefab         (Page，无 Canvas)
}
```

---

### 7.2 GMBoxPanel

**文件**：`Runtime/Scripts/Test/GMBoxPanel.cs`

`ui_panel_gm_box.prefab` 的 AbstractPanel 适配层。

- 注册于 `TestUIID.GMBoxPanel = 1`，`openui 1` 可通过 UIManager 加载
- `OnOpen` 调用 `TestGMBoxPanel.Show()`，`OnClose` 调用 `TestGMBoxPanel.Hide()`
- 实际 GM 指令输入/输出由场景中的 `TestGMBoxPanel`（IMGUI）负责

---

### 7.3 TestPanel

**文件**：`Runtime/Scripts/Test/TestPanel.cs`

| 成员 | 说明 |
|------|------|
| `Text m_TitleText` | 打开时更新为 `"TestPanel #N"` |
| `Button m_CloseButton` | 点击调用 `UIManager.S.ClosePanel` |
| `int openCount` | 只读，记录被打开的累计次数（供 TC04 断言）|
| `void OpenPageA(params object[] args)` | 调用 `panelActive.AttachPage(TestUIID.TestPageA, args)` |
| `void ClosePageA(bool useCache = false)` | 调用 `panelActive.DettachPage(TestUIID.TestPageA, useCache)` |
| `bool IsPageAOpened()` | 返回 `panelActive.IsPageOpened(TestUIID.TestPageA)` |

生命周期日志：
```
[TestPanel] OnCreate
[TestPanel] OnOpen  第 1 次  args=(none)
[TestPanel] OnVisibleChanged  isVisible=true
[TestPanel] OnClose
```

---

### 7.4 TestModalPanel

**文件**：`Runtime/Scripts/Test/TestModalPanel.cs`

- `hideMask = PanelHideMask.HideAndUnInteractive`
- 打开后下层所有面板**不可见且不可交互**，关闭后自动恢复
- 面板中央显示 `HideMask` 说明文字，便于直观验证遮蔽效果
- 供 `openui 3` 手动验证 HideMask 机制

---

### 7.5 TestPageA

**文件**：`Runtime/Scripts/Test/TestPageA.cs`

测试子页面，演示 `AbstractPage` 动态挂载/卸载的完整生命周期。

| 成员 | 说明 |
|------|------|
| `Text m_TitleText` | 打开时更新为 `"TestPageA #N"` |
| `Text m_InfoText` | 显示打开次数、传入参数、uiID |
| `Button m_CloseButton` | 点击调用 `panelActive.DettachPage(uiID)` |
| `int openCount` | 只读，记录被打开的累计次数 |

**与 Panel 的关键区别：**

| | Panel | Page |
|-|-------|------|
| 基类 | `AbstractPanel` | `AbstractPage` |
| 需要 Canvas | 是（`[RequireComponent]`）| **否** |
| 打开方式 | `UIManager.OpenPanel(uiID)` | `panelActive.AttachPage(pageID)` |
| 关闭方式 | `UIManager.ClosePanel(uiID)` | `panelActive.DettachPage(pageID)` |
| 挂载位置 | `PanelRoot` 子节点 | **父面板的 Transform 子节点** |
| sortingOrder | 由 UIManager 统一管理 | 继承父面板 Canvas |

生命周期日志：
```
[TestPageA] OnCreate
[TestPageA] OnOpen  第 1 次  args=[via GUI]
[TestPageA] OnClose
[TestPageA] OnDispose
```

---

### 7.6 TestUIBoot

**文件**：`Runtime/Scripts/Test/TestUIBoot.cs`

按序初始化：`AsyncTaskManager` → `ResourceLoad` → `UIManager` → `UIDataTable` 注册 → GM 指令注册。

```csharp
public class TestUIBoot : MonoBehaviour
{
    public UIRoot         m_UIRoot;     // 必填
    public TestGMBoxPanel m_GMBox;      // 必填
    public UIFlowTest     m_FlowTest;   // 可选，连线后注册 uitest 指令
}
```

---

### 7.7 UIFlowTest

**文件**：`Runtime/Scripts/Test/UIFlowTest.cs`

```csharp
public class UIFlowTest : MonoBehaviour
{
    public TestGMBoxPanel m_GMBox;  // 可选，留空时自动 FindObjectOfType

    public void RunTests();         // 外部触发自动测试（7 条 TC）
}
```

| 快捷键 | 功能 |
|--------|------|
| **F2** | 自动顺序运行全部 TC，结果输出到 Console + GM 面板 |
| **F3** | 切换手动调试 GUI 悬浮窗（可拖拽）|

---

## 八、扩展指南

### 8.1 添加新测试面板（Panel）

**Step 1** — `TestUIID.cs` 追加常量：
```csharp
public const int MyPanel = 5;
```

**Step 2** — `TestUIBoot.RegisterTestPanels()` 追加注册：
```csharp
UIDataTable.Register(new UIData
{
    uiID        = TestUIID.MyPanel,
    name        = "MyPanel",
    path        = "ui/uiprefab/",
    filename    = "ui_panel_my",
    suffix      = ".prefab",
    sortLayer   = PanelSortLayer.Auto,
    cacheCount  = 1,
    isSingleton = true,
});
```

**Step 3** — 在 `CreateUITestPrefabEditor.cs` 中添加对应的 `BuildMyPrefab()` 方法，并在 `CreateAllPrefabs()` 中调用 `SavePrefab("ui_panel_my", BuildMyPrefab())`。

**Step 4** — 菜单 **ST.Core / Test / Create All Test Prefabs** 重新生成。

---

### 8.2 添加新测试子页面（Page）

**Step 1** — 创建继承 `AbstractPage` 的脚本（无需 Canvas）：
```csharp
public class MyPage : AbstractPage
{
    protected override void OnOpen(object[] args) { ... }
    protected override void OnClose() { ... }
}
```

**Step 2** — `TestUIID.cs` 追加常量：
```csharp
public const int MyPage = 5;
```

**Step 3** — `TestUIBoot.RegisterTestPanels()` 注册（`isSingleton = false`，允许多实例挂载）：
```csharp
UIDataTable.Register(new UIData
{
    uiID        = TestUIID.MyPage,
    name        = "MyPage",
    path        = "ui/uiprefab/",
    filename    = "ui_page_my",
    suffix      = ".prefab",
    sortLayer   = PanelSortLayer.Auto,
    cacheCount  = 1,
    isSingleton = false,   // Page 通常允许多实例
});
```

**Step 4** — 在父面板中调用：
```csharp
// 挂载（异步加载）
panelActive.AttachPage(TestUIID.MyPage, arg1, arg2);

// 卸载（true = 走缓存，false = 销毁）
panelActive.DettachPage(TestUIID.MyPage, useCache: true);

// 查询
bool opened = panelActive.IsPageOpened(TestUIID.MyPage);
```

---

### 8.3 添加自动化 TC

在 `RunAllTests()` 末尾追加调用，并实现协程：

```csharp
yield return TC08_MyCustomCase();

IEnumerator TC08_MyCustomCase()
{
    bool passed = true;
    Print("── TC08 自定义测试");

    yield return EnsureClosed();

    UIManager.S.OpenPanel(TestUIID.TestPanel);
    yield return WaitForPanel<TestPanel>(k_LoadTimeout);

    Check(UIManager.S.IsOpened(TestUIID.TestPanel), "已打开", ref passed);

    yield return EnsureClosed();
    ReportTC("TC08_自定义测试", passed);
}
```

---

### 8.4 添加手动 TC（逐步执行）

在 `InitManualTCs()` 的数组中追加 `BuildManualTC08()`：

```csharp
ManualTC BuildManualTC08()
{
    return new ManualTC("TC08 自定义", "TC08", new Step[]
    {
        new Step("清理：关闭 TestPanel",
            "IsOpened=false",
            () => { if (UIManager.S.IsOpened(TestUIID.TestPanel)) UIManager.S.ClosePanel(TestUIID.TestPanel); },
            () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

        new Step("OpenPanel(2)",
            "IsOpened=true",
            () => UIManager.S.OpenPanel(TestUIID.TestPanel),
            () => UIManager.S.IsOpened(TestUIID.TestPanel)),

        new Step("等待加载完成",
            "FindPanel≠null",
            null,
            () => UIManager.S.FindPanel<TestPanel>() != null,
            waitLoad: true),

        // 在此添加更多步骤...
    });
}
```

> `waitLoad: true` 表示该步骤执行后会自动轮询 `check()` 直到返回 `true` 或超时（默认 5 秒）。

---

### 8.5 注册自定义 GM 指令

```csharp
var gm = FindObjectOfType<TestGMBoxPanel>();
gm.RegisterCommand("myCmd", (args) =>
{
    // args[0] = 指令名，args[1..] = 参数
    gm.AppendOutput("[myCmd] 执行完成");
}, "我的自定义指令");
```

---

## 九、注意事项

1. **EventSystem 唯一**：确保场景中只有一个 `EventSystem`，否则 uGUI 事件处理异常。
2. **Panel 根节点必须有 Canvas**：`AbstractPanel` 通过 `[RequireComponent(typeof(Canvas))]` 强制要求，编辑器工具已自动添加。
3. **Page 根节点不需要 Canvas**：`AbstractPage` 无 `RequireComponent` 限制；Page 由父面板的 Canvas 渲染，添加 Canvas 反而会导致层级混乱。
4. **UI Layer**：所有测试 Prefab 的 GameObject 层级均设为 `UI`（Layer 5），`UICamera` 默认只渲染该层。
5. **CanvasScaler 不加在 Panel Prefab 上**：Panel 作为嵌套 Canvas 挂入 `PanelRoot` 时，`CanvasScaler(ScaleWithScreenSize)` 会以父容器初始尺寸（可能为 0）计算缩放，导致 `scale = 0`。全局缩放由 `UIRoot.RootCanvas` 的 `CanvasScaler` 统一管理。
6. **异步加载超时**：`WaitForPanel` / `WaitForPage` 与手动步骤均默认等待 **5 秒**；低性能机器可修改 `UIFlowTest.k_LoadTimeout`。
7. **openCount 跨测试累积**：缓存复用时 `openCount` 不归零，TC04 通过记录初始值再比对增量来断言，而非直接比对绝对值。
8. **TC05 调用 DoClose**：会清空所有运行中的面板（但不清除 `UIDataTable` 静态注册），后续 TC 可正常继续。
9. **手动 GUI 与 UGUI 层级**：IMGUI 默认渲染在 UGUI 上方，手动 GUI 窗口不受 UIManager 层级管理影响。
10. **GMBoxPanel 与 TestGMBoxPanel 关系**：`GMBoxPanel`（UGUI）是 UIManager 适配层，实际 GM 功能由场景中的 `TestGMBoxPanel`（IMGUI）提供，两者通过 `Show()` / `Hide()` 接口联动。

---

[← 返回 UI 管理系统](UI.md)
