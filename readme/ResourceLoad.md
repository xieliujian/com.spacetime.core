# 资源加载系统

[← 返回主页](../README.md)

运行时资源加载框架，统一管理 **AssetBundle 加载**与**编辑器 AssetDatabase 直读**两种模式，支持同步/异步加载、场景加载、Lua 资产处理，以及编辑器打包工具。

---

## 快速开始

### 1. 实现资源配置

业务项目需提供一个 `IResourceConfig` 实现：

```csharp
using ST.Core;

public class GameResourceConfig : IResourceConfig
{
    public string appName           => "mygame";
    public string assetDir          => "assetBundle";
    public string bundleSuffix      => ".unity3d";
    public string editorPathPrefix  => "Assets/Package/";
    public string assetBundleDBFile => "assetbundledb.txt";
}
```

### 2. 初始化加载器

在游戏启动阶段完成初始化（依赖 `AsyncTaskManager` 的 `DoUpdate` 驱动异步任务）：

```csharp
using ST.Core;

// 异步任务管理器（须每帧调用 DoUpdate）
var asyncMgr = new AsyncTaskManager();
asyncMgr.DoInit();

// 资源加载器
var resLoad = new ResourceLoad();
resLoad.SetConfig(new GameResourceConfig());
resLoad.DoInit();  // 解析 assetbundledb.txt，构建 Bundle 字典
```

### 3. 同步加载资源

```csharp
// 加载 Prefab
var prefab = ResourceLoad.instance.LoadResourceSync(
    "ui/panel/", "MainPanel", ".prefab") as GameObject;

// 加载 Texture
var tex = ResourceLoad.instance.LoadResourceSync(
    "art/texture/", "hero_icon", ".png",
    ResourceType.Default) as Texture2D;
```

### 4. 异步加载资源

```csharp
ResourceLoad.instance.LoadResourceAsync(
    "ui/panel/", "MainPanel", ".prefab",
    (obj) => {
        var prefab = obj as GameObject;
        // 使用 prefab
    });
```

### 5. 异步加载场景

```csharp
ResourceLoad.instance.LoadSceneAsync(
    "scenes/", "BattleScene", ".unity",
    (progress) => {
        loadingBar.value = progress;
    },
    (obj) => {
        // 场景加载完成
    });
```

### 6. 编辑器模式切换

编辑器下默认走 `AssetDatabase` 直读（无需打包）：

```csharp
// 强制使用 AssetBundle（验证包体时使用）
ResourceLoad.useAssetBundle = true;
```

---

## 编辑器打包

`ST/` 菜单提供打包入口，使用前须在项目启动代码中注册配置：

```csharp
using ST.Core.Editor;
using ST.Core;

// 在 Editor 启动脚本中注册一次
Packager.RegisterConfig(new GameResourceConfig());
```

| 菜单项 | 说明 |
|--------|------|
| `ST/Build AssetBundles` | 打包当前平台所有 Bundle |
| `ST/Clear AssetBundles` | 清理输出目录 |
| `ST/Package Font` | 单独打包字体资源 |
| `ST/Package Audio` | 单独打包音频资源 |
| `ST/Package Lua` | 将 Lua 脚本打包进 ScriptableObject |

---

## API 一览

### IResourceConfig（接口）

业务项目实现此接口，向加载框架提供应用级配置：

| 属性 | 类型 | 说明 |
|------|------|------|
| `appName` | `string` | 应用名称，用于 StreamingAssets 子目录名 |
| `assetDir` | `string` | AssetBundle 输出目录名（相对工程根目录） |
| `bundleSuffix` | `string` | Bundle 文件后缀，例如 `.unity3d` |
| `editorPathPrefix` | `string` | 编辑器下资源路径前缀，例如 `Assets/Package/` |
| `assetBundleDBFile` | `string` | AB 数据库文件名，例如 `assetbundledb.txt` |

---

### ResourceLoad（主入口）

继承 `BaseResourceLoad`，实现 `IManager` 接口：

| 成员 | 说明 |
|------|------|
| `static instance` | 全局加载器引用 |
| `static useAssetBundle` | 编辑器下是否强制使用 AssetBundle 模式 |
| `SetConfig(IResourceConfig)` | **须在 `DoInit()` 前调用**，注入配置 |
| `DoInit()` | 解析 AB 数据库，构建 Bundle 字典，安装 `LuaAssetDecorator` |
| `DoUpdate()` | 驱动异步任务（需每帧调用） |
| `LoadResourceSync(path, filename, suffix, type)` | 同步加载单个资源 |
| `LoadAllResourceSync(path, filename, suffix)` | 同步加载路径下全部资源 |
| `LoadResourceAsync(path, filename, suffix, callback, type)` | 异步加载单个资源 |
| `LoadSceneAsync(path, filename, suffix, progress, complete)` | 异步加载场景 |
| `InstallDecorator(IAssetDecorator)` | 注册资产装饰器（相同实例不重复添加） |

---

### ResourceType（枚举）

| 值 | CLR 映射 | 说明 |
|----|----------|------|
| `Default` | `object` | 默认，不做类型特殊处理 |
| `String` | `string` | Lua 文本资源（由 `LuaAssetDecorator` 拦截转换） |
| `Bytes` | `byte[]` | Lua 字节码（由 `LuaAssetDecorator` 拦截转换） |
| `GameObject` | — | Prefab 等 |
| `Scene` | — | 场景文件 |
| `Texture` | — | 贴图 |
| `Sprite` | — | 精灵 |
| `Material` | — | 材质 |
| `Shader` | — | 着色器 |
| `AnimationClip` | — | 动画片段 |
| `AudioClip` | — | 音频 |
| `ScriptableObject` | — | ScriptableObject 资产 |

---

### AssetBundleDBMgr

解析 `assetbundledb.txt` 文本数据库，替代 Unity 原生 `AssetBundleManifest`，提供 Bundle 名称枚举与依赖查询：

文件格式：

```
AB名称\t序号ID
\tDepend:依赖ID1\t依赖ID2\t...
```

| 成员 | 说明 |
|------|------|
| `Init(dbFilePath)` | 解析单个数据库文件 |
| `Init(dbFilePaths[])` | 解析多个数据库文件（合并） |
| `GetAllAssetBundleNames()` | 返回所有已注册的 Bundle 名称列表 |
| `GetAssetBundleDepends(abName)` | 返回指定 Bundle 的直接依赖名称数组 |

---

### FilePathHelper

运行时路径拼接工具，由 `BaseResourceLoad.SetConfig` 自动创建：

| 方法 | 说明 |
|------|------|
| `GetFilePath()` | Bundle 根目录；Windows 独立包返回 `dataPath/StreamingAssets/`，其余返回 `streamingAssetsPath/` |
| `GetBundleFullPath(respath)` | 完整磁盘路径：`GetFilePath() + appName + "/" + respath` |

---

### AppPlatform（静态工具）

| 成员 | 说明 |
|------|------|
| `dataPath` | 数据根目录（Editor 为工程目录，运行时因平台而异） |
| `GetStreamingAssetsPath(appName)` | `StreamingAssets/<appName>/` 路径 |
| `GetCurBuildTarget()` *(Editor only)* | 根据编译宏推断 `BuildTarget` |
| `GetCurBuildTargetGroup()` *(Editor only)* | 根据编译宏推断 `BuildTargetGroup` |
| `GetPackageResPath(target, appName)` *(Editor only)* | Bundle 打包输出目录 |

---

### IAssetDecorator（接口）

在加载前后对资产路径、类型、内容进行变换：

```csharp
public interface IAssetDecorator
{
    void BeforeLoad(ref string key, ref Type type);   // 修改路径或目标类型
    void AfterLoad(string key, Type type, ref object asset);  // 替换返回对象
}
```

内置实现 **`LuaAssetDecorator`**：将 `string`/`byte[]` 类型请求映射为 `TextAsset` 加载，完成后自动还原为文本或字节码。

---

### 异步任务系统

`AsyncTaskManager` 每帧驱动所有 `AsyncTask` 子类，完成后自动移除：

| 类 | 说明 |
|----|------|
| `AsyncTaskManager` | 默认任务管理器，须每帧调用 `DoUpdate()` |
| `AsyncAssetRequest` | Bundle 内单个资产异步加载 |
| `AsyncBundleRequest` | Bundle 文件异步加载（自动等待依赖完成） |
| `AsyncSceneRequest` | 运行时场景异步加载 |
| `EditorAsyncAssetRequest` | 编辑器资产异步加载（单帧即完成） |
| `EditorAsyncSceneRequest` | 编辑器 Play 模式场景异步加载 |

---

## 架构

```
ResourceLoad（唯一公开入口）
  ├── UNITY_EDITOR & !useAssetBundle
  │     └── EditorResourceLoad
  │           ├── AssetDatabase.LoadAssetAtPath（同步）
  │           ├── EditorAsyncAssetRequest（异步）
  │           └── EditorAsyncSceneRequest（场景）
  │
  └── AssetBundle 模式（真机 / useAssetBundle=true）
        └── AssetBundleLoad
              ├── Bundle（单包封装）
              │     ├── AsyncBundleRequest（包文件异步加载）
              │     ├── AsyncAssetRequest（资产异步加载）
              │     └── AsyncSceneRequest（场景异步加载）
              └── AssetBundleDBMgr（解析 assetbundledb.txt，提供名称枚举与依赖查询）

AsyncTaskManager（每帧驱动所有 AsyncTask）

IAssetDecorator 链（BeforeLoad → 加载 → AfterLoad）
  └── LuaAssetDecorator（string/byte[] ↔ TextAsset 互转）
```

---

## 生命周期

```
SetConfig(config)          ← 注入 IResourceConfig（DoInit 前必须调用）
    ↓
DoInit()                   ← 解析 assetbundledb.txt，构建 Bundle 字典，安装装饰器
    ↓
DoUpdate() × N             ← 每帧驱动异步任务
    ↓
DoClose()                  ← 释放资源
```

---

## 依赖

**运行时**
- `UnityEngine`（`AssetBundle`、`SceneManager`、`Application`）
- `ST.Core`（`IManager`、`CommonDefine`）

**编辑器**
- `UnityEditor`（`AssetDatabase`、`BuildPipeline`、`EditorSceneManager`）
- `ST.Core.Editor`（`EditorUtils`）

---

[← 返回主页](../README.md)
