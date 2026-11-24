# 资源加载系统

[← 返回主页](../../README.md)

运行时资源加载框架，统一管理 AssetBundle 加载与编辑器 AssetDatabase 直读两种模式，支持同步与异步加载、场景加载、Lua 资产处理，并提供编辑器打包工具。

## 目录结构

```text
Runtime/Scripts/ResourceLoad/
    Asset/           IAssetDecorator、LuaAssetDecorator、LuaScriptableObject
    AsyncTask/       AsyncTask、AsyncTaskModule、BaseAsyncTaskModule 与各请求实现
    Bundle/          AssetBundleLoad、Bundle、AssetBundleDBMgr
    Define/          ResourceDefine、ResourceType、ResourceLoadDelegate
    Module/          BaseResourceModule、ResourceModule、IResourceConfig、FilePathHelper、EditorResourceLoad
Editor/Scripts/ResourceLoad/
    Packager.cs      菜单打包入口、AB 数据库生成与清单清理
```

所有类型位于 `ST.Core` 命名空间。

## 快速开始

### 1. 实现资源配置

业务项目提供一个 `IResourceConfig` 实现，向加载与打包流程提供应用级参数。

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

### 2. 通过 GameLauncher 初始化

`GameLauncher` 已经按固定顺序创建 `AsyncTaskModule` 与 `ResourceModule`，业务项目只需实现 `CreateResourceConfig`。

```csharp
protected override IResourceConfig CreateResourceConfig()
{
    return new GameResourceConfig();
}
```

`CollectBaseModules` 先创建异步任务模块，再调用 `SetConfig` 注入配置，随后加入模块列表，由 `GameWorld` 按注册顺序调用 `DoInit`、`DoUpdate` 与 `DoClose`。

### 3. 手动初始化

不经 `GameLauncher` 时需要自行保证初始化顺序。`AsyncTaskModule` 必须先于任何异步加载请求存在，`SetConfig` 必须在 `DoInit` 之前调用。

```csharp
var asyncTaskModule = new AsyncTaskModule();
asyncTaskModule.DoInit();

var resourceModule = new ResourceModule();
resourceModule.SetConfig(new GameResourceConfig());
resourceModule.DoInit();

// 每帧驱动异步任务，不能省略
asyncTaskModule.DoUpdate();
```

`ResourceModule.DoUpdate` 自身没有异步任务逻辑，异步请求由 `AsyncTaskModule` 驱动。

### 4. 加载资源

```csharp
// 同步加载 Prefab
var prefab = ResourceModule.instance.LoadResourceSync(
    "ui/panel/", "MainPanel", ".prefab") as GameObject;

// 同步加载 Texture
var texture = ResourceModule.instance.LoadResourceSync(
    "art/texture/", "hero_icon", ".png", ResourceType.Default) as Texture2D;

// 异步加载 Prefab
ResourceModule.instance.LoadResourceAsync(
    "ui/panel/", "MainPanel", ".prefab",
    (obj) =>
    {
        var loadedPrefab = obj as GameObject;
    });

// 异步加载场景
ResourceModule.instance.LoadSceneAsync(
    "scenes/", "BattleScene", ".unity",
    (progress) =>
    {
        loadingBar.value = progress;
    },
    (obj) =>
    {
        // 场景加载完成
    });
```

Editor 下默认用 `AssetDatabase.LoadAssetAtPath` 直读，实际路径为 `editorPathPrefix + path + filename + suffix`。真机始终走 AssetBundle，实际路径为 `FilePath.streamingAssetsPath + appName + "/" + path + filename + bundleSuffix`。

### 5. 编辑器切换 Bundle 模式

编辑器下默认直读工程资产。需要验证包体时强制走 AssetBundle，切换前先释放旧的 Bundle。

```csharp
resourceModule.DoClose();
ResourceModule.useAssetBundle = true;
resourceModule.DoInit();
```

## 配置与路径归属

### IResourceConfig 的职责

`IResourceConfig` 是业务工程与资源模块之间唯一的配置注入点，框架内的用途如下。

| 属性 | 使用位置 | 说明 |
|------|----------|------|
| `appName` | `AssetBundleLoad`、`FilePathHelper`、`Packager` | 定位 `BuildRes/<appName>/` 子目录 |
| `bundleSuffix` | `AssetBundleLoad` | 由逻辑路径拼出 Bundle 名称 |
| `editorPathPrefix` | `EditorResourceLoad`、`Packager` | 编辑器直读路径前缀与打包时的路径裁剪 |
| `assetBundleDBFile` | `AssetBundleLoad`、`Packager` | 文本依赖数据库文件名 |
| `assetDir` | 暂无框架内引用 | 预留给业务打包脚本，当前框架不读取 |

这些参数取决于具体游戏工程，包括应用名、资源目录、后缀与数据库文件名。缺少这层配置接口，核心包只能写死常量或增加大量构造参数，`GameLauncher` 与 `Packager` 也会失去统一的配置入口。

### FilePathHelper 的位置

`FilePathHelper` 位于本包的 `Runtime/Scripts/ResourceLoad/Module`，描述的是 AssetBundle 目录约定。

| 方法 | 说明 |
|------|------|
| `GetFilePath()` | 直接返回 `Core/IO` 中 `FilePath.streamingAssetsPath` |
| `GetAppResPath(appName)` | 静态方法，拼出 `BuildRes/<小写 appName>/`，供编辑器打包使用 |
| `GetBundleFullPath(respath)` | 拼出 `根目录 + appName + "/" + respath` |

通用根目录、外存目录与 Windows 超长路径处理已经在 `Core/IO/File` 的 `FilePath` 与 `FileMgr` 中实现。`FilePathHelper` 只保留 Bundle 布局相关的拼接，根目录统一从 `FilePath.streamingAssetsPath` 获取。它的职责与 `IResourceConfig` 绑定，保留在资源模块内可以让通用文件层继续服务其它业务，不引入资源配置依赖。

## 编辑器打包

`Packager` 的菜单位于 `SpaceTime/Build/`。

| 菜单项 | 说明 |
|--------|------|
| `SpaceTime/Build/Build iPhone Resource` | 切换到 iOS 目标并打包 |
| `SpaceTime/Build/Build Android Resource` | 切换到 Android 目标并打包 |
| `SpaceTime/Build/Build Windows Resource` | 切换到 Standalone 目标并打包 |

打包前可以提供配置实例。未调用 `RegisterConfig` 时，`Packager` 会遍历已加载程序集，找到第一个非抽象的 `IResourceConfig` 实现并用无参构造创建，随后缓存结果。工程内存在多个实现时应显式注册，避免按程序集顺序选中其它配置。

```csharp
using ST.Core;

Packager.RegisterConfig(new GameResourceConfig());
```

输出目录由 `FilePathHelper.GetAppResPath` 决定，应用名统一转成小写。运行时 `GetBundleFullPath` 直接使用配置值，因此 `appName` 应全部使用小写，避免大小写敏感平台找不到资源。

```text
BuildRes/
    <appName 小写>/
        assetbundledb.txt
        <bundle 名称>
```

打包流程依次清空输出目录、扫描 `Assets/Package/` 下的字体、贴图、音频、Prefab、场景与 Lua 资产、调用 `BuildPipeline.BuildAssetBundles`、生成文本依赖数据库，最后删除 Unity 生成的 Manifest 文件。

## API 一览

### BaseResourceModule（抽象基类）

定义加载接口、装饰器链与配置注入，派生类实现具体的加载分支。

| 成员 | 说明 |
|------|------|
| `static instance` | 最后一次构造的派生实例 |
| `SetConfig(IResourceConfig)` | 注入配置并创建 `FilePathHelper`，必须在 `DoInit` 前调用 |
| `InstallDecorator(IAssetDecorator)` | 注册装饰器，相同实例不重复添加 |
| `LoadResourceSync(path, filename, suffix, type)` | 同步加载单个资源 |
| `LoadAllResourceSync(path, filename, suffix)` | 同步加载路径下全部资源 |
| `LoadResourceAsync(path, filename, suffix, callback, type)` | 异步加载单个资源 |
| `LoadSceneAsync(path, filename, suffix, progress, complete)` | 异步加载场景 |

### ResourceModule（默认实现）

| 成员 | 说明 |
|------|------|
| `static useAssetBundle` | 编辑器下是否强制走 AssetBundle 模式 |
| `DoInit()` | 创建编辑器与 Bundle 加载器，按平台初始化 Bundle 字典，安装 `LuaAssetDecorator` |
| `DoUpdate()` | 空实现，异步任务由 `AsyncTaskModule` 驱动 |
| `DoClose()` | 卸载全部已加载 Bundle，释放字典 |

编辑器下只有 `useAssetBundle` 为 `true` 时才初始化 Bundle 字典，避免未打包时反复输出数据库缺失日志。真机在 `DoInit` 时直接初始化。

### IResourceConfig（接口）

| 属性 | 类型 | 说明 |
|------|------|------|
| `appName` | `string` | 应用名称，同时作为 `BuildRes` 子目录名，建议全小写 |
| `assetDir` | `string` | AssetBundle 输出目录名，预留给业务打包脚本，框架当前不读取 |
| `bundleSuffix` | `string` | Bundle 文件后缀，例如 `.unity3d` |
| `editorPathPrefix` | `string` | 编辑器资源路径前缀，例如 `Assets/Package/` |
| `assetBundleDBFile` | `string` | AB 数据库文件名，例如 `assetbundledb.txt` |

### ResourceType（枚举）

| 值 | CLR 映射 | 说明 |
|----|----------|------|
| `Default` | `object` | 默认，不做类型特殊处理 |
| `String` | `string` | Lua 文本资源，由 `LuaAssetDecorator` 拦截转换 |
| `Bytes` | `byte[]` | Lua 字节码，由 `LuaAssetDecorator` 拦截转换 |
| `GameObject` | — | Prefab 等 |
| `Scene` | — | 场景文件 |
| `Texture` | — | 贴图 |
| `Sprite` | — | 精灵 |
| `Material` | — | 材质 |
| `Shader` | — | 着色器 |
| `AnimationClip` | — | 动画片段 |
| `AudioClip` | — | 音频 |
| `ScriptableObject` | — | ScriptableObject 资产 |

### FilePathHelper

| 方法 | 说明 |
|------|------|
| `GetFilePath()` | Bundle 根目录，返回 `FilePath.streamingAssetsPath` |
| `GetAppResPath(appName)` | 静态方法，应用资源根目录 `根目录 + 小写 appName + "/"` |
| `GetBundleFullPath(respath)` | 完整磁盘路径 `GetFilePath() + appName + "/" + respath` |

### AssetBundleDBMgr

解析文本依赖数据库，替代 Unity 原生 `AssetBundleManifest`，提供 Bundle 名称枚举与依赖查询。

文件格式如下。

```text
AB名称\t序号ID
\tDepend:依赖ID1\t依赖ID2\t...
```

| 成员 | 说明 |
|------|------|
| `Init(dbFilePath)` | 解析单个数据库文件 |
| `Init(dbFilePaths[])` | 解析多个数据库文件并合并 |
| `GetAllAssetBundleNames()` | 返回所有已注册的 Bundle 名称 |
| `GetAssetBundleDepends(abName)` | 返回指定 Bundle 的直接依赖名称数组 |

### ResourceDefine（常量）

| 常量 | 说明 |
|------|------|
| `BUNDLE_FULL_PATH_FORMAT` | Bundle 完整路径模板 `{0}{1}/{2}` |
| `APP_RES_PATH_FORMAT` | 应用资源目录模板 `{0}{1}/` |
| `BUNDLE_DICT_CAPACITY` | Bundle 字典初始容量 |
| `BUNDLE_INFO_DICT_CAPACITY` | 数据库 Bundle 信息字典初始容量 |
| `DEPEND_LIST_CAPACITY` | 单个 Bundle 的依赖列表初始容量 |
| `PENDING_REQUEST_LIST_CAPACITY` | Bundle 加载完成前挂起请求的列表初始容量 |
| `DECORATOR_LIST_CAPACITY` | 资产装饰器列表初始容量 |
| `TASK_LIST_CAPACITY` | 异步任务列表初始容量 |
| `TEMP_TASK_LIST_CAPACITY` | 已结束任务暂存列表初始容量 |
| `FIELD_SEPARATOR` | 数据库字段分隔符 |
| `DEPEND_ATTR_HEADER` | 数据库依赖属性行头 |
| `LOG_DB_FILE_NOT_FOUND` | 数据库文件缺失日志模板 |
| `LOG_DB_INDEX_OUT_OF_RANGE` | 依赖索引越界日志模板 |

### IAssetDecorator（接口）

在加载前后变换资产路径、类型与返回对象。

```csharp
public interface IAssetDecorator
{
    void BeforeLoad(ref string key, ref Type type);
    void AfterLoad(string key, Type type, ref object asset);
}
```

内置 `LuaAssetDecorator` 把 `string` 与 `byte[]` 请求映射成 `TextAsset` 加载，完成后还原为文本或字节。

### 异步任务系统

`AsyncTaskModule` 每帧调用 `AsyncTask.Update`，完成任务自动移除。

| 类 | 说明 |
|----|------|
| `BaseAsyncTaskModule` | 任务模块抽象基类，维护全局 `instance` |
| `AsyncTaskModule` | 默认任务管理器，由 `DoUpdate` 驱动队列 |
| `AsyncAssetRequest` | Bundle 内单个资产异步加载 |
| `AsyncBundleRequest` | Bundle 文件异步加载，自动等待依赖完成 |
| `AsyncSceneRequest` | 运行时场景异步加载 |
| `EditorAsyncAssetRequest` | 编辑器资产异步加载，单帧完成 |
| `EditorAsyncSceneRequest` | 编辑器 Play 模式场景异步加载 |

## 架构

```text
ResourceModule（唯一公开入口）
  ├── UNITY_EDITOR 且 useAssetBundle 为 false
  │     └── EditorResourceLoad
  │           ├── AssetDatabase.LoadAssetAtPath（同步）
  │           ├── EditorAsyncAssetRequest（异步）
  │           └── EditorAsyncSceneRequest（场景）
  │
  └── AssetBundle 模式（真机或 useAssetBundle 为 true）
        └── AssetBundleLoad
              ├── Bundle（单包封装）
              │     ├── AsyncBundleRequest（包文件异步加载）
              │     ├── AsyncAssetRequest（资产异步加载）
              │     └── AsyncSceneRequest（场景异步加载）
              └── AssetBundleDBMgr（文本依赖数据库）

AsyncTaskModule（每帧驱动所有 AsyncTask）

IAssetDecorator 链（BeforeLoad → 加载 → AfterLoad）
  └── LuaAssetDecorator（string/byte[] 与 TextAsset 互转）
```

## 生命周期

```text
SetConfig(config)          ← 注入 IResourceConfig，必须在 DoInit 之前
    ↓
DoInit()                   ← 创建加载器，按平台解析 AB 数据库，安装装饰器
    ↓
AsyncTaskModule.DoUpdate() ← 每帧驱动异步任务
    ↓
DoClose()                  ← 卸载 Bundle
```

## 依赖

**运行时**

- `UnityEngine`（`AssetBundle`、`TextAsset`、`SceneManager`）
- `ST.Core`（`ILogicModule`、`FilePath`、`STLogger`）

**编辑器**

- `UnityEditor`（`AssetDatabase`、`BuildPipeline`、`EditorSceneManager`）
- `ST.Core`（`EditorUtils`）

## 相关文档

- [原始文件访问](../File/File.md)

[← 返回主页](../../README.md)
