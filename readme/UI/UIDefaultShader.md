# UI 默认 Shader 替换与改名说明

[← 返回主页](../../README.md)

> 适用 Unity 2022.3 + URP 14.0.11 + 内嵌 `com.unity.ugui` 1.0.0
> 示例工程 `F:\proj_se\develop\client\project`
> 包内 shader `Shaders/UI/UIDefault.shader`，Shader 名 `SpaceTime/UI/Default`

---

## 一、结论

| 问题 | 结论 |
|------|------|
| 怎么让 UGUI 不使用系统自带的 UI shader | 运行时把 `Graphic.defaultGraphicMaterial.shader` 指向自己的 shader，UGUI 没有可配置的默认 shader 选项 |
| 自写的 shader 能不能改名 | 能，shader 名只是资源标识，UGUI 不按名字找 shader |
| 改名后内嵌 UGUI 会不会不识别 | 不会，内嵌包的 `Graphic` 走 `Canvas.GetDefaultCanvasMaterial()`，拿到材质后由工程代码替换 shader |
| 改名真正会影响的地方 | 工程里按字符串 `"UI/Default"` 查找的代码和变体列表，见第四节表格 |
| 包内当前命名 | `SpaceTime/UI/Default`，文件 `Shaders/UI/UIDefault.shader`，include 目录 `Shaders/UI/UIDefault/` |

---

## 二、工程现在的替换链路

内嵌 UGUI 包打过补丁，`Graphic.defaultGraphicMaterial` 增加了 setter，允许外部换掉这个共享材质。

```csharp
// Packages/com.unity.ugui@1.0.0/Runtime/UI/Core/Graphic.cs:92
static public Material defaultGraphicMaterial
{
    get
    {
        if (s_DefaultUI == null)
            s_DefaultUI = Canvas.GetDefaultCanvasMaterial();
        return s_DefaultUI;
    }
    #region LR Modified
    set
    {
        s_DefaultUI = value;
    }
    #endregion
}
```

`CommonResHolder.InitUIDefaultMat()` 在 InSplash 资源加载完成后执行替换。

```csharp
// Assets/Script/LogicModule/ResModule/CommonResHolder.cs:701
void InitUIDefaultMat()
{
#if UNITY_EDITOR
    Shader uiDefaultShader = Shader.Find("UI/Default");
#else
    Shader uiDefaultShader = FindShader("UI/Default");
#endif

    if (uiDefaultShader == null)
    {
        return;
    }

    Material uiDefaultMat = UnityEngine.UI.Graphic.defaultGraphicMaterial;
    if (uiDefaultMat == null)
    {
        return;
    }

    if (m_UIDefaultShader == null)
    {
        m_UIDefaultShader = uiDefaultMat.shader;
    }

    uiDefaultMat.shader = uiDefaultShader;
}
```

真机路径的 `FindShader` 先查 `m_ShaderMap`，这张表由 `InitShaderMap()` 用 `ShaderContainer.prefab` 里的 shader 列表按 `shader.name` 建立（`CommonResHolder.cs:584`），查不到再回退 `Shader.Find`。

替换生效范围是所有没有单独指定材质的 UI 图元，包括 `Image`、`RawImage`、`Text` 和 `InputField` 的光标。`TextMeshPro` 使用字体资源自带的材质，不经过这条链。

替换时机必须早于 UI 创建。`RectMask2D` 和 `Mask` 第一次参与裁剪时会用 `new Material(baseMaterial)` 复制材质，复制发生在替换之后才能继承新 shader。工程把这一步放在 InSplash 阶段。

关闭模块时 `ResetUIDefaultMat()` 把 shader 还原为 `m_UIDefaultShader`。

---

## 三、为什么只能这样做

`Canvas.GetDefaultCanvasMaterial()` 由 Unity 原生实现，固定返回内置 `UI/Default` 材质。Player Settings 和 URP Asset 都没有默认 UI shader 的替换入口，要换成自己的实现只能拿到这个共享材质实例并修改它的 shader。

工程需要灰度、圆形裁剪、可配置混合、SRP Batcher 兼容和 On-Chip 标记，这些能力内置 shader 都不提供。

---

## 四、Shader 名的影响面

包内 shader 与工程副本的关系如下。

| 项 | `Packages/com.spacetime.core/Shaders/UI/UIDefault.shader` | `Assets/LingRen/Shader/UI/UI-Default.shader` |
|----|------|------|
| Shader 名 | `SpaceTime/UI/Default` | `UI/Default` |
| GUID | `63f678fcbc0a93d4b91dd00e027dcdc3` | 同左 |
| 实现 | HLSL 拆分，`UnityPerMaterial` CBUFFER | CG 单 pass |
| On-Chip 标签 | `LRMobileOnChip=Transparent` | 无 |
| 遮罩属性 | `_ClipRect`、`_UIMaskSoftnessX/Y`、`_CircleClipParams` 已声明 | 未声明 |

两份资源的 GUID 相同，改名后名字不再相同。把包嵌入工程后仍然只能保留一份，同 GUID 会让 Unity 报 GUID 冲突，实际加载哪一份没有保证。

按当前工程的表现，工程内同名 shader 会优先于内置 shader 被 `Shader.Find` 命中，编辑器分支正是靠这一点拿到自定义 shader。改名后包内 shader 不再遮蔽内置 `UI/Default`，工程里 `Shader.Find("UI/Default")` 的返回值也随之稳定为内置 shader。

包内 shader 沿用了原 GUID，目的是让 `ShaderContainer.prefab`、`svc_custom.shadervariants`、`SVCDB.asset` 里的引用在删除工程副本后继续指向同一个 shader。

改名需要同步的位置如下。

| # | 位置 | 用法 | 不同步的后果 |
|---|------|------|------|
| 1 | `CommonResHolder.cs:704` | 编辑器下 `Shader.Find("UI/Default")` | Play 模式拿到内置 shader，灰度、圆裁、自定义混合失效 |
| 2 | `CommonResHolder.cs:706` | 真机 `FindShader("UI/Default")` | 查表失败后回退 `Shader.Find`，AB 未收录时返回 null，替换直接跳过 |
| 3 | `ShaderVariantDB.txt:78` | 变体清单，`ReadShaderKeyWordDBFromFile` 用 `Shader.Find(名字)` 解析 | 变体不进包，运行时关键字组合缺失 |
| 4 | `SVCDB.asset` | 变体集合的 `m_Name` 为 `UI/Default` | 需要按第五节重新生成 |
| 5 | `MobileOnChipMaterials.cs:397` | 无 `LRMobileOnChip` 标签时的特判 | 新 shader 也不带标签时校验失败，整帧回退非 On-Chip 路径 |
| 6 | `UIBuilder.cs:141`、`UIBuilder.cs:294`、`UIBuilder.cs:299` | 图集材质 `new Material(Shader.Find("UI/Default"))` | 新建的图集材质拿到内置 shader |
| 7 | `UITools.cs:395` | Spine `SkeletonGraphic` 材质 | 同上 |

`MobileOnChipMaterials.cs:397` 的特判如下，只有材质上没有 `LRMobileOnChip` 标签时才会走到。

```csharp
// Canvas may use Unity's built-in UI/Default instead of the local shader.
return !opaque && untaggedShaderName == "UI/Default" && shader == Canvas.GetDefaultCanvasMaterial().shader;
```

包内 shader 带有 `LRMobileOnChip=Transparent` 标签，会走标签分支，这段特判不会命中。

改名不影响按 GUID 引用的资源，包括 `Assets/LingRen/Shader/UI/ShaderContainer.prefab:66`、`Assets/LingRen/Shader/UI/svc_custom.shadervariants`、`SVCDB.asset` 的 `m_Shaders` 条目，以及所有 prefab 里已经指定该材质的对象。

---

## 五、包内命名与接入步骤

包内命名已按工程约定统一，shader 名为 `SpaceTime/UI/Default`，文件为 `Shaders/UI/UIDefault.shader`，include 目录为 `Shaders/UI/UIDefault/`，GUID 保持 `63f678fcbc0a93d4b91dd00e027dcdc3` 不变。改名后 `Shader.Find("UI/Default")` 稳定返回 Unity 内置 shader，两份资源不再互相遮蔽。

材质面板通过 `CustomEditor "LWGUI.LWGUI"` 使用 LWGUI 分组，属性按基础、混合与裁剪、模板三组排布，依赖工程内的 LWGUI-2.x 包。工程缺少 LWGUI 时 Unity 回退默认材质面板，shader 渲染不受影响。

接入工程时按下面的顺序处理。

1. 删除工程副本 `Assets/LingRen/Shader/UI/UI-Default.shader` 和对应的 `.meta`，GUID 相同的引用会自动指向包内 shader。
2. 把 `CommonResHolder.cs:704` 和 `CommonResHolder.cs:706` 的查找字符串改成 `SpaceTime/UI/Default`，两处在同一个方法里，可以共用一个局部变量。
3. 把 `ShaderVariantDB.txt:78` 的 `UI/Default` 改成 `SpaceTime/UI/Default`，关键字保持不变。
4. 执行菜单 `MHT/Tools/CreateShaderSVCDB` 重新生成 `SVCDB.asset`。变体集合按 GUID 引用 shader，名字变化只影响集合的 `m_Name`。
5. 检查 `ShaderContainer.prefab` 是否仍包含该 shader。`MHT/Tools/RefreshShaderContainer` 只扫描 `Assets/LingRen/Shader/<子目录>`，重跑会把已经移进包里的 shader 从容器中剔除，导致真机查表失败。可以保留现有 prefab 不重跑，也可以让生成工具额外扫描包内目录。
6. 按第六节逐项验证。

如果工程要保留自己的 `UI/Default`，包内 shader 需要同时换 GUID。新 GUID 用 Unity 重新导入生成，然后由使用方显式引用包内 shader。这种情况下包内 shader 与工程默认 UI 材质没有关联，替换仍由使用方自己的初始化代码完成。

---

## 六、验证清单

| 场景 | 期望结果 |
|------|------|
| 普通 `Image` 与 `RawImage` | 显示正常，与改名前一致 |
| `RectMask2D` 软裁剪 | 边缘过渡正常 |
| 圆形裁剪 `UNITY_UI_CLIP_CIRCLE` | 圆外区域透明 |
| 灰度显示 | 顶点色为黑时输出灰度 |
| Alpha 裁剪 `UNITY_UI_ALPHACLIP` | 低于阈值的像素被裁掉 |
| 自定义混合 `_SrcBlendCustom`、`_DstBlendCustom` | 混合结果正常 |
| `InputField` 光标 | 正常显示 |
| `TextMeshPro` 文本 | 正常显示，走自身材质 |
| Spine `SkeletonGraphic` | 正常显示 |
| 真机 AssetBundle 包 | 替换成功，日志中没有找不到 shader 的记录 |
| Android 与 iOS On-Chip | 没有 `UI/Default` 相关的回退日志 |

---

## 七、常见问题

**只改 shader 名，代码不动会怎样**

编辑器的 Play 模式会拿到内置 `UI/Default`，灰度、圆形裁剪、自定义混合都不生效。真机 `FindShader` 查不到新名字，`InitUIDefaultMat` 直接返回，所有默认 UI 图元继续使用内置 shader。

**编辑器非运行状态看起来也不一样**

非运行状态没有执行 `InitUIDefaultMat`，`Canvas.GetDefaultCanvasMaterial()` 固定返回内置材质，差异只在 Play 模式或真机生效。

**内嵌 UGUI 升级后编译不过**

`Graphic.defaultGraphicMaterial` 的 setter 是 `LR Modified` 补丁，用 Unity 官方包覆盖会丢失，升级内嵌 UGUI 时需要重新打这个补丁。

**两份 shader 能不能都留着，只改其中一份的名字**

GUID 相同会让 Unity 无法稳定区分两份资源，引用落到哪一份取决于加载顺序。保留一份、修改名字或 GUID 才可控。

---

[← 返回主页](../../README.md)
