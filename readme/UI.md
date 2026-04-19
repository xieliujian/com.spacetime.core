# UI 管理系统

[← 返回主页](../README.md)

> 版本：1.0.0 | 命名空间：`ST.Core.UI`

---

## 一、系统概述

**UIManager** 是 `com.spacetime.core` 提供的 UI 管理系统，负责所有面板（Panel）和页面（Page）的生命周期管理、层级排序、资源加载、可见性控制和交互管理。

| 特性 | 说明 |
|------|------|
| 单例管理 | 继承 `IManager`，全局唯一实例 `UIManager.S` |
| 面板缓存 | 支持面板关闭后缓存，避免重复加载 |
| 层级管理 | 三层排序（Bottom / Auto / Top），自动分配 `sortingOrder` |
| 页面挂载 | 支持动态挂载/卸载子页面，内部页面自动发现 |
| 可见性控制 | 支持 HideMask 隐藏下层面板，优化渲染 |
| 资源加载 | 依赖注入 `BaseResourceLoad`，支持异步加载与缓存复用 |
| UIID 类型 | 框架层使用 `int`，上层工程定义 `enum UIID : int` |

---

## 二、文件结构

```
Runtime/Scripts/UI/
├── UIDefine.cs                  枚举：PanelSortLayer / PanelHideMask / PanelCloseTween
├── UIManager.cs                 总控（IManager，单例 S）
├── Data/
│   ├── UIData.cs                面板/页面静态配置数据
│   ├── UIDataTable.cs           配置注册表（静态）
│   └── UIPanelActive.cs         面板实例生命周期容器
└── UGUI/
    ├── UIRoot.cs                UI 根节点（双摄像机/Canvas）
    ├── AbstractPage.cs          页面基类（MonoBehaviour）
    └── AbstractPanel.cs         面板基类（继承 AbstractPage，需要 Canvas）
```

---

## 三、快速上手

### 3.1 初始化

```csharp
// 1. 注册面板配置（启动时执行一次）
UIDataTable.Register(new UIData
{
    uiID       = (int)UIID.BagPanel,
    name       = "BagPanel",
    path       = "PanelUI/",
    filename   = "BagPanel",
    sortLayer  = PanelSortLayer.Auto,
    cacheCount = 1,
});

// 2. 创建并初始化 UIManager
var uiManager = new UIManager();
uiManager.Setup(BaseResourceLoad.instance, uiRoot);  // 注入依赖
uiManager.DoInit();
```

### 3.2 打开 / 关闭面板

```csharp
// 打开面板（可传参数）
UIManager.S.OpenPanel((int)UIID.BagPanel);
UIManager.S.OpenPanel((int)UIID.ShopPanel, itemID, tabIndex);

// 强制指定层级打开
UIManager.S.OpenTopPanel((int)UIID.LoadingPanel);
UIManager.S.OpenBottomPanel((int)UIID.MainBgPanel);

// 关闭面板
UIManager.S.ClosePanel((int)UIID.BagPanel);

// 关闭特定实例（非单例场景）
int panelID = UIManager.S.OpenPanel((int)UIID.TipsPanel);
UIManager.S.ClosePanelByPanelID(panelID);
```

### 3.3 查询状态

```csharp
bool opened  = UIManager.S.IsOpened((int)UIID.BagPanel);
bool visible = UIManager.S.IsPanelVisible((int)UIID.BagPanel);
bool active  = UIManager.S.IsPanelActive((int)UIID.BagPanel);  // 打开且可见

AbstractPanel panel = UIManager.S.FindPanel((int)UIID.BagPanel);
BagPanel bagPanel   = UIManager.S.FindPanel<BagPanel>();
```

### 3.4 实现自定义面板

```csharp
// Prefab 上挂载此组件，Prefab 必须有 Canvas 组件
public class BagPanel : AbstractPanel
{
    // 覆盖：遮挡下层面板（减少 Overdraw）
    public override PanelHideMask hideMask => PanelHideMask.Hide;

    protected override void OnCreate()
    {
        // 仅执行一次：获取组件引用、注册按钮事件
    }

    protected override void OnOpen(object[] args)
    {
        // 每次打开时：处理参数、刷新数据
        panelActive.AttachPage((int)UIID.BagEquipPage);
    }

    protected override void OnClose()
    {
        // 每次关闭时：重置 UI 状态
    }

    protected override void OnDispose()
    {
        // 仅执行一次：释放资源、取消订阅事件
    }

    protected override void OnVisibleChanged(bool isVisible)
    {
        // 可见性变化时：控制动画/渲染开关
    }
}
```

---

## 四、核心类说明

### 4.1 UIManager

**文件**：`Runtime/Scripts/UI/UIManager.cs`

**职责**：UI 系统总控制器，管理所有面板的打开/关闭/排序。

```csharp
public class UIManager : IManager
{
    public static UIManager S { get; }

    // 依赖注入（必须在 DoInit 之前调用）
    public void Setup(BaseResourceLoad resourceLoad, UIRoot uiRoot);

    // 打开面板
    public int OpenPanel(int uiID, params object[] args);
    public int OpenTopPanel(int uiID, params object[] args);
    public int OpenBottomPanel(int uiID, params object[] args);

    // 关闭面板
    public void ClosePanel(int uiID);
    public void ClosePanelByPanelID(int panelID);

    // 查询
    public AbstractPanel FindPanel(int uiID);
    public T             FindPanel<T>() where T : AbstractPanel;
    public bool IsOpened(int uiID);
    public bool IsPanelActive(int uiID);
    public bool IsPanelVisible(int uiID);
    public bool IsPageOpen(int uiID, int pageID);

    // 手动控制
    public void SetPanelVisible(int uiID, bool visible);
    public void SetPanelInteract(int uiID, bool interact);
}
```

---

### 4.2 UIPanelActive

**文件**：`Runtime/Scripts/UI/Data/UIPanelActive.cs`

**职责**：管理单个面板实例的完整生命周期，包括异步加载、页面挂载、可见性/交互状态。

```csharp
public class UIPanelActive
{
    public int            panelID    { get; }   // 唯一实例 ID（自增）
    public int            uiID       { get; }   // 面板类型 ID
    public AbstractPanel  panel      { get; }   // MonoBehaviour 实例（加载中为 null）
    public PanelSortLayer sortLayer  { get; }
    public int            sortIndex  { get; }
    public bool           isVisible  { get; set; }
    public bool           isInteract { get; set; }
    public bool           isLoading  { get; }
    public bool           isReady    { get; }

    // 子页面管理
    public void AttachPage(int pageID, params object[] args);
    public void DettachPage(int pageID, bool useCache = false);
    public bool IsPageOpened(int pageID);
}
```

---

### 4.3 AbstractPanel / AbstractPage

**文件**：`Runtime/Scripts/UI/UGUI/`

| 特性 | AbstractPanel | AbstractPage |
|------|:---:|:---:|
| 必须有 Canvas | ✓ | ✗ |
| 可独立打开 | ✓ | ✗（需挂载在 Panel 下） |
| 支持 sortLayer | ✓ | ✗ |
| 支持 hideMask | ✓ | ✗ |
| 支持关闭动画 | ✓ | ✓ |
| 支持 OnCreate / OnOpen / OnClose / OnDispose | ✓ | ✓ |

**AbstractPanel 可重写属性：**

```csharp
public virtual int           sortIndex  => 0;
public virtual PanelHideMask hideMask   => PanelHideMask.None;
public virtual PanelCloseTween closeTween => PanelCloseTween.None;
```

---

### 4.4 UIData / UIDataTable

**文件**：`Runtime/Scripts/UI/Data/`

```csharp
public class UIData
{
    public int            uiID;          // 面板类型整型 ID
    public string         name;          // 调试名称
    public string         path;          // Prefab 目录（如 "PanelUI/"）
    public string         filename;      // Prefab 文件名（不含扩展名）
    public string         suffix;        // 扩展名，默认 ".prefab"
    public Type           type;          // AbstractPanel 或 AbstractPage 的具体类型
    public PanelSortLayer sortLayer;     // 排序层级
    public int            cacheCount;    // 缓存数量（0 = 不缓存）
    public bool           isSingleton;   // 是否单例（默认 true）
}

public static class UIDataTable
{
    public static void   Register(UIData data);
    public static UIData GetData(int uiID);
    public static void   Clear();
}
```

---

### 4.5 UIRoot

**文件**：`Runtime/Scripts/UI/UGUI/UIRoot.cs`

**职责**：场景中的 UI 根节点，挂载双摄像机/Canvas 引用。

```csharp
public class UIRoot : MonoBehaviour
{
    public Camera        m_UICamera;          // 普通 UI 摄像机（受后期特效）
    public Canvas        m_RootCanvas;
    public RectTransform m_PanelRoot;

    public Camera        m_TopUICamera;       // 顶层摄像机（不受后期特效）
    public Canvas        m_TopRootCanvas;
    public RectTransform m_TopPanelRoot;

    public const int DEFAULT_UI_WIDTH  = 1334;
    public const int DEFAULT_UI_HEIGHT = 750;
}
```

---

## 五、生命周期

### 5.1 面板生命周期

```
OnCreate()          ← 仅一次，初始化组件引用
  ↓
OnOpen(args)        ← 每次打开
  ↓
OnVisibleChanged()  ← 可见性变化时（可多次）
  ↓
OnClose()           ← 每次关闭
  ↓
OnDispose()         ← 仅一次（销毁前），释放资源
```

### 5.2 页面挂载生命周期

```
AttachPage(pageID)
  ↓ 查找缓存 → 有则复用 / 无则异步加载
  ↓ Instantiate Prefab
  ↓ OnCreate()
  ↓ OnOpen(args)
  ↓ PlayOpenAnimation()（可选）

DettachPage(pageID)
  ↓ PlayCloseAnimation()（可选）
  ↓ OnClose()
  ↓ OnDispose() 或 SetActive(false) 入缓存
```

---

## 六、层级管理

### 6.1 PanelSortLayer

```csharp
public enum PanelSortLayer : byte
{
    Bottom = 0,   // 底层（主界面背景）
    Auto   = 1,   // 普通叠加（大部分面板）
    Top    = 2,   // 顶层（Loading、引导）
}
```

### 6.2 sortingOrder 分配规则

```
Bottom 层：sortingOrder = 0   + 层内索引
Auto   层：sortingOrder = 100 + 层内索引
Top    层：sortingOrder = 200 + 层内索引

层内排序优先级：sortIndex 小的在下 → sortIndex 相同则按打开顺序
```

### 6.3 自定义 sortIndex

```csharp
public class LoadingPanel : AbstractPanel
{
    public override int           sortIndex  => 9999;
    public override PanelSortLayer // 通过 OpenTopPanel 打开时已设置为 Top
}
```

---

## 七、HideMask 机制

### 7.1 PanelHideMask

```csharp
[Flags]
public enum PanelHideMask : byte
{
    None                 = 0,
    UnInteractive        = 1 << 0,   // 下层不可交互
    Hide                 = 1 << 1,   // 下层不可见
    HideAndUnInteractive = 3,        // 下层不可见且不可交互
}
```

### 7.2 计算规则

从栈顶向下遍历，遇到 `Hide` 标志则其下所有面板变为不可见，遇到 `UnInteractive` 则其下所有面板变为不可交互。

```
面板 C（最上层，hideMask = UnInteractive）← 可见 + 可交互
面板 B（hideMask = Hide）               ← 可见 + 不可交互（被 C 影响）
面板 A（hideMask = None）               ← 不可见 + 不可交互（被 B 影响）
```

**收益**：隐藏下层面板可减少 Overdraw，提升渲染性能。

---

## 八、面板缓存

```csharp
UIDataTable.Register(new UIData
{
    uiID       = (int)UIID.BagPanel,
    cacheCount = 1,   // 关闭后缓存 1 个实例，下次打开直接复用
});
```

**收益**：避免重复加载资源和重建 GameObject，减少 GC。

---

## 九、扩展指南

### 9.1 新增面板

1. 创建 Prefab，根节点挂载 `Canvas` 和继承自 `AbstractPanel` 的脚本
2. 在上层工程的 `UIID` 枚举中添加新 ID
3. 调用 `UIDataTable.Register(...)` 注册配置
4. 重写所需生命周期回调

### 9.2 新增子页面

1. 创建 Prefab，挂载继承自 `AbstractPage` 的脚本（**不需要** Canvas）
2. 在 `UIID` 枚举中添加新 ID
3. 注册 `UIData`（`sortLayer` 可不填，页面不参与排序）
4. 在面板 `OnOpen` 中通过 `panelActive.AttachPage(...)` 挂载

### 9.3 内部子页面（Inner Page）

在面板 Prefab 的子节点上直接挂载 `AbstractPage` 并设置 `uiID`，
UIManager 在面板加载完成后会自动扫描并执行 `OnCreate()`，无需代码挂载。

```
BagPanel (Prefab)
├── EquipPage  → 挂 BagEquipPage（uiID 已设置）  ← 自动发现
└── ItemPage   → 挂 BagItemPage（uiID 已设置）   ← 自动发现
```

---

## 十、注意事项

1. **Canvas 必需**：所有 `AbstractPanel` 的 Prefab 根节点必须有 `Canvas` 组件。
2. **UIID 唯一**：每个面板/页面的整型 ID 全局不重复。
3. **生命周期顺序**：`OnCreate` → `OnOpen` → `OnClose` → `OnDispose`，不可跳过。
4. **资源释放**：在 `OnDispose` 中释放引用，避免内存泄漏。
5. **单例面板**：`isSingleton = true`（默认）时，重复调用 `OpenPanel` 只返回已有实例 panelID，不会重复打开。
6. **异步加载**：`panel` 属性在加载期间为 `null`，不要在 `OpenPanel` 调用后立即访问 `panel`；在 `OnOpen` 内访问是安全的。
7. **?.  运算符**：遵循项目规范，禁止使用 `?.`，用显式 `if` null 检查替代。

---

## 十一、测试场景

> 详细说明请参阅子文档 **[→ UI 测试系统](UI_Test.md)**，以下为快速摘要。

`Runtime/Scripts/Test/` 提供了开箱即用的测试组件，无需编写任何启动代码即可在编辑器场景中验证 UIManager 的完整流程。

### 11.1 测试文件

| 文件 | 职责 |
|------|------|
| `TestGMBoxPanel.cs` | 可拖拽的 OnGUI GM 指令窗口，支持指令注册、执行、历史记录（PlayerPrefs 持久化） |
| `TestUIBoot.cs` | 场景引导类：初始化 ResourceLoad → UIManager → UIDataTable，并向 GMBox 注册 UI 调试指令 |

### 11.2 场景搭建

```
Hierarchy
├── UIRoot              ← 挂 UIRoot.cs，配置双 Camera / Canvas / PanelRoot
└── Boot
    ├── TestUIBoot.cs   ← 引导类
    └── TestGMBoxPanel.cs ← GM 面板（F1 开关）
```

**Inspector 连线：**

| TestUIBoot 字段 | 拖入对象 |
|---|---|
| `m_UIRoot` | UIRoot GameObject 上的 `UIRoot` 组件 |
| `m_GMBox` | Boot GameObject 上的 `TestGMBoxPanel` 组件 |

### 11.3 运行时 GM 指令

运行后按 **F1** 打开 GM 面板，支持以下内置指令：

| 指令 | 功能 |
|---|---|
| `help` | 列出所有已注册指令及描述 |
| `openui <uiID>` | 打开指定 uiID 的面板，例如 `openui 1` |
| `closeui <uiID>` | 关闭指定 uiID 的面板 |
| `closeall` | 关闭所有运行中的面板 |
| `isopen <uiID>` | 查询面板状态（IsOpened / IsVisible / IsActive） |
| `listpanel` | 列出所有已注册的 TestUIID 常量 |
| `echo <内容>` | 回显文本到输出区 |
| `clear` | 清空输出区 |

键盘快捷键：`↑` / `↓` 导航历史记录，`Enter` 执行指令。

### 11.4 TestUIID 面板 ID 表

```csharp
// TestUIID.cs — 独立公共常量类，TestUIBoot 与 UIFlowTest 共用
public static class TestUIID
{
    public const int GMBoxPanel = 1;   // ui/uiprefab/ui_panel_gm_box.prefab
    public const int TestPanel  = 2;   // ui/uiprefab/ui_panel_test.prefab
}
```

### 11.5 扩展测试面板

**第一步**：在 `TestUIBoot.RegisterTestPanels()` 追加注册：

```csharp
UIDataTable.Register(new UIData
{
    uiID      = TestUIID.MyPanel,
    name      = "MyPanel",
    path      = "ui/uiprefab/",
    filename  = "ui_panel_my",
    suffix    = ".prefab",
    sortLayer = PanelSortLayer.Auto,
});
```

**第二步**：在 `TestUIID` 追加常量：

```csharp
public const int MyPanel = 3;
```

运行后执行 `openui 3` 即可验证。

### 11.6 注册自定义 GM 指令

```csharp
// 在任意 MonoBehaviour 的 Start 中获取 TestGMBoxPanel 并注册
var gm = FindObjectOfType<TestGMBoxPanel>();

gm.RegisterCommand("reloadui", (args) =>
{
    UIManager.S.DoClose();
    UIManager.S.DoInit();
    gm.AppendOutput("[reloadui] UIManager 已重置。");
}, "重置 UIManager");
```

---

## 子文档

| 文档 | 说明 |
|------|------|
| [UI 测试系统](UI_Test.md) | 测试场景搭建、自动化测试用例、GM 指令手册、扩展指南 |

---

[← 返回主页](../README.md)
