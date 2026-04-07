# 资源加载打包代码迁移 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `UnityDemo_ResourceLoad_V1` 的资源加载与 AssetBundle 打包代码迁移到 `com.spacetime.core` 核心包，命名空间统一为 `ST.Core`，不含业务层硬编码配置值。

**Architecture:** 新增 `IResourceConfig` 接口隔离业务配置；`FilePathHelper` 替换静态 `File` 类，通过构造注入接收配置；`Packager` 通过静态 `RegisterConfig` 接受配置注入；所有基类从 `IManager` 继承并补充 `DoLateUpdate()`。

**Tech Stack:** Unity C# (UnityEngine, UnityEngine.SceneManagement, UnityEditor), .NET Standard 2.1, 无第三方依赖

**Spec:** `D:\xieliujian\com.spacetime.core\Packages\docs\superpowers\specs\2026-04-07-resource-load-migration-design.md`

---

## 文件清单

### 新建 Runtime 文件

| 文件路径 | 说明 |
|---|---|
| `Runtime/Scripts/ResourceLoad/IResourceConfig.cs` | 配置接口（新建） |
| `Runtime/Scripts/ResourceLoad/AppPlatform.cs` | 跨平台路径工具（从源项目 AppConst.cs 拆出） |
| `Runtime/Scripts/ResourceLoad/FilePathHelper.cs` | 路径帮助类，非静态，注入 IResourceConfig |
| `Runtime/Scripts/ResourceLoad/BaseResourceLoad.cs` | 资源加载抽象基类，定义委托和接口 |
| `Runtime/Scripts/ResourceLoad/ResourceLoad.cs` | 主资源加载实现 |
| `Runtime/Scripts/ResourceLoad/AssetBundleLoad.cs` | AssetBundle 加载实现 |
| `Runtime/Scripts/ResourceLoad/EditorResourceLoad.cs` | 编辑器模式资源加载 |
| `Runtime/Scripts/ResourceLoad/Bundle.cs` | Bundle 管理类 |
| `Runtime/Scripts/ResourceLoad/AssetDecorator/IAssetDecorator.cs` | 资产装饰器接口 |
| `Runtime/Scripts/ResourceLoad/AssetDecorator/LuaAssetDecorator.cs` | Lua 资产装饰器 |
| `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncTask.cs` | 异步任务抽象基类 |
| `Runtime/Scripts/ResourceLoad/AsyncTask/BaseAsyncTaskManager.cs` | 异步任务管理器抽象基类 |
| `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncTaskManager.cs` | 异步任务管理器实现 |
| `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncAssetRequest.cs` | 异步资产请求 |
| `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncBundleRequest.cs` | 异步 Bundle 请求（注入 FilePathHelper） |
| `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncSceneRequest.cs` | 异步场景请求 |
| `Runtime/Scripts/ResourceLoad/AsyncTask/EditorAsyncAssetRequest.cs` | 编辑器异步资产请求 |
| `Runtime/Scripts/ResourceLoad/AsyncTask/EditorAsyncSceneRequest.cs` | 编辑器异步场景请求（含 bug 修复） |
| `Runtime/Scripts/ResourceLoad/Lua/LuaScriptableObject.cs` | Lua ScriptableObject 容器 |

### 新建 Editor 文件

| 文件路径 | 说明 |
|---|---|
| `Editor/Scripts/ResourceLoad/EditorUtil.cs` | 编辑器工具函数（MD5、进程调用等） |
| `Editor/Scripts/ResourceLoad/Packager.cs` | AssetBundle 打包工具，菜单项 `ST/...` |

### 无需修改

- `Runtime/com.spacetime.core.runtime.asmdef`
- `Editor/com.spacetime.core.editor.asmdef`
- `Runtime/Scripts/Core/IManager.cs`（基类，已存在）
- `Runtime/Scripts/Core/CommonDefine.cs`（常量，已存在）

---

## Task 1: IResourceConfig 接口

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/IResourceConfig.cs`

- [ ] **Step 1: 创建目录**

```powershell
New-Item -ItemType Directory -Force -Path "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Runtime\Scripts\ResourceLoad"
New-Item -ItemType Directory -Force -Path "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Runtime\Scripts\ResourceLoad\AssetDecorator"
New-Item -ItemType Directory -Force -Path "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Runtime\Scripts\ResourceLoad\AsyncTask"
New-Item -ItemType Directory -Force -Path "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Runtime\Scripts\ResourceLoad\Lua"
New-Item -ItemType Directory -Force -Path "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Editor\Scripts\ResourceLoad"
```

- [ ] **Step 2: 创建 IResourceConfig.cs**

```csharp
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 资源加载配置接口，由业务项目实现，提供应用名称、Bundle 路径等配置。
    /// </summary>
    public interface IResourceConfig
    {
        /// <summary>应用/游戏名称，用于 StreamingAssets 子目录名。</summary>
        string appName { get; }

        /// <summary>AssetBundle 输出目录名（相对于工程根目录）。</summary>
        string assetDir { get; }

        /// <summary>AssetBundle 文件后缀，例如 ".unity3d"。</summary>
        string bundleSuffix { get; }

        /// <summary>编辑器模式下资源路径前缀，例如 "Assets/Package/"。</summary>
        string editorPathPrefix { get; }
    }
}
```

- [ ] **Step 3: 验证**

在 Unity Editor 中打开工程，确认 Console 无编译错误。

---

## Task 2: AppPlatform 跨平台路径工具

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/AppPlatform.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\Common\AppConst.cs`（仅迁移 `AppPlatform` 类，`AppConst` 类不迁移）

- [ ] **Step 1: 创建 AppPlatform.cs**

```csharp
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ST.Core
{
    public class AppPlatform
    {
        /// <summary>数据根目录（Editor 下为工程目录，运行时因平台而异）。</summary>
        public static string dataPath
        {
            get
            {
#if UNITY_EDITOR
                return Application.dataPath + "/../";
#else
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                    return Application.dataPath + "/";
                return Application.persistentDataPath + "/";
#endif
            }
        }

        /// <summary>StreamingAssets 下的应用子目录路径。</summary>
        public static string GetStreamingAssetsPath(string appName)
        {
            return Application.streamingAssetsPath + "/" + appName.ToLower() + "/";
        }

#if UNITY_EDITOR
        public static BuildTarget GetCurBuildTarget()
        {
            var target = BuildTarget.NoTarget;
#if UNITY_ANDROID
            target = BuildTarget.Android;
#endif
#if UNITY_IOS
            target = BuildTarget.iOS;
#endif
#if UNITY_STANDALONE_WIN
            target = BuildTarget.StandaloneWindows;
#endif
            return target;
        }

        public static BuildTargetGroup GetCurBuildTargetGroup()
        {
            var targetgroup = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            targetgroup = BuildTargetGroup.Android;
#endif
#if UNITY_IOS
            targetgroup = BuildTargetGroup.iOS;
#endif
#if UNITY_STANDALONE_WIN
            targetgroup = BuildTargetGroup.Standalone;
#endif
            return targetgroup;
        }

        public static string GetPackageResPath(BuildTarget target, string appName)
        {
            string platformpath = "";
            if (target == BuildTarget.StandaloneWindows)
                platformpath = RuntimePlatform.WindowsPlayer.ToString().ToLower();
            else if (target == BuildTarget.Android)
                platformpath = RuntimePlatform.Android.ToString().ToLower();
            else if (target == BuildTarget.iOS)
                platformpath = RuntimePlatform.IPhonePlayer.ToString().ToLower();

            string appname = appName.ToLower();
            return Application.dataPath + "/../assetBundle/" + platformpath + "/" + appname + "/";
        }
#endif
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误。

---

## Task 3: FilePathHelper 路径帮助类

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/FilePathHelper.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\Common\File.cs`

- [ ] **Step 1: 创建 FilePathHelper.cs**

```csharp
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 运行时文件路径帮助类，依赖 <see cref="IResourceConfig"/> 提供应用名称。
    /// </summary>
    public class FilePathHelper
    {
        readonly IResourceConfig m_Config;

        public FilePathHelper(IResourceConfig config)
        {
            m_Config = config;
        }

        /// <summary>获取 Bundle 根目录（StreamingAssets 下）。</summary>
        public string GetFilePath()
        {
#if UNITY_EDITOR
            return Application.streamingAssetsPath + "/";
#else
            if (Application.platform == RuntimePlatform.WindowsPlayer)
                return Application.dataPath + "/StreamingAssets/";
            return Application.streamingAssetsPath + "/";
#endif
        }

        /// <summary>获取指定相对路径的 Bundle 完整路径。</summary>
        public string GetBundleFullPath(string respath)
        {
            return GetFilePath() + m_Config.appName + "/" + respath;
        }
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误。

---

## Task 4: IAssetDecorator 接口

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/AssetDecorator/IAssetDecorator.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\AssetDecorator\AssetDecorator.cs`

- [ ] **Step 1: 创建 IAssetDecorator.cs**

```csharp
using System;

namespace ST.Core
{
    /// <summary>资产装饰器接口，在加载前后对资产路径、类型、内容进行变换。</summary>
    public interface IAssetDecorator
    {
        /// <summary>加载前：可修改资产 key 和目标类型。</summary>
        void BeforeLoad(ref string key, ref Type type);

        /// <summary>加载后：可替换返回的资产对象。</summary>
        void AfterLoad(string key, Type type, ref object asset);
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误。

---

## Task 5: AsyncTask 异步任务基类

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncTask.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\AsyncTask\AsyncTask.cs`

注意：`AsyncTask` 使用 `ResourceLoadProgress` / `ResourceLoadComplete` 委托，这两个委托定义在 `BaseResourceLoad.cs`（Task 6）。Unity 同 Assembly 内编译不依赖顺序，但需在 Task 6 完成后才能整体验证无误。

- [ ] **Step 1: 创建 AsyncTask.cs**

```csharp
namespace ST.Core
{
    public abstract class AsyncTask
    {
        public enum ETaskState
        {
            NotStart,
            Running,
            Completed,
            End,
        }

        protected ETaskState m_State = ETaskState.NotStart;
        protected object m_Asset = null;
        protected ResourceLoadProgress m_ProgressEvent;
        protected ResourceLoadComplete m_CompleteEvent;

        public abstract float progress { get; }

        public ResourceLoadProgress progressEvent
        {
            get { return m_ProgressEvent; }
            set { m_ProgressEvent = value; }
        }

        public ResourceLoadComplete completeEvent
        {
            get { return m_CompleteEvent; }
            set { m_CompleteEvent = value; }
        }

        public bool isEnd
        {
            get { return m_State == ETaskState.End; }
        }

        public void Update()
        {
            if (m_State == ETaskState.NotStart)
            {
                m_State = ETaskState.Running;
                OnStart();
            }

            m_State = OnUpdate();

            if (m_State == ETaskState.Completed)
            {
                m_State = ETaskState.End;
                OnEnd();
            }
        }

        protected abstract void OnStart();
        protected abstract ETaskState OnUpdate();
        protected abstract void OnEnd();
    }
}
```

注意：`ResourceLoadProgress` 和 `ResourceLoadComplete` 委托定义在 `BaseResourceLoad.cs`（Task 6），Unity 同一 Assembly 内编译顺序无关，但需确保 Task 6 也已创建。

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误（Task 6 完成后一起验证）。

---

## Task 6: BaseResourceLoad 资源加载抽象基类

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/BaseResourceLoad.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\BaseResourceLoad.cs`

变更：
- `Manager` → `IManager`（添加 `DoLateUpdate()` 空实现）
- `AssetDecorator` → `IAssetDecorator`
- `ConstDefine.LIST_CONST_8` → `CommonDefine.s_ListConst_8`
- 新增 `SetConfig(IResourceConfig)`、`m_Config`、`m_FilePathHelper` 字段

- [ ] **Step 1: 创建 BaseResourceLoad.cs**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    /// <summary>资源加载进度回调。</summary>
    public delegate void ResourceLoadProgress(float progress);

    /// <summary>资源加载完成回调。</summary>
    public delegate void ResourceLoadComplete(object asset);

    /// <summary>资源类型枚举。</summary>
    public enum ResourceType : byte
    {
        Default,
        String,
        Bytes,
        GameObject,
        Scene,
        Texture,
        Sprite,
        Material,
        Shader,
        AnimationClip,
        AudioClip,
        ScriptableObject,
    }

    public abstract class BaseResourceLoad : IManager
    {
        protected static BaseResourceLoad s_Instance = null;

        protected readonly List<IAssetDecorator> m_Decorators = new List<IAssetDecorator>(CommonDefine.s_ListConst_8);

        protected IResourceConfig m_Config;
        protected FilePathHelper m_FilePathHelper;

        public static BaseResourceLoad instance
        {
            get { return s_Instance; }
        }

        public BaseResourceLoad()
        {
            s_Instance = this;
            m_Decorators.Clear();
        }

        public void SetConfig(IResourceConfig config)
        {
            m_Config = config;
            m_FilePathHelper = new FilePathHelper(config);
        }

        public abstract object LoadResourceSync(string path, string filename, string suffix, ResourceType restype = ResourceType.Default);
        public abstract object[] LoadAllResourceSync(string path, string filename, string suffix);
        public abstract void LoadResourceAsync(string path, string filename, string suffix, ResourceLoadComplete callback, ResourceType restype = ResourceType.Default);
        public abstract void LoadSceneAsync(string path, string filename, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null);

        public void InstallDecorator(IAssetDecorator decorator)
        {
            if (m_Decorators.Contains(decorator))
                return;
            m_Decorators.Add(decorator);
        }

        public override void DoLateUpdate() { }

        protected Type Type2Native(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.String:
                    return typeof(string);
                case ResourceType.Bytes:
                    return typeof(byte[]);
                default:
                    return typeof(object);
            }
        }
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误（Task 5 与 Task 6 一并验证，两者委托/引用互依赖，Unity 同 Assembly 内按整体编译）。

---

## Task 7: BaseAsyncTaskManager 与 AsyncTaskManager

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/BaseAsyncTaskManager.cs`
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncTaskManager.cs`

源文件:
- `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\AsyncTask\BaseAsyncTaskManager.cs`
- `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\AsyncTask\AsyncTaskManager.cs`

变更：
- `Manager` → `IManager`
- `ConstDefine` 常量替换
- 补充 `DoLateUpdate()` 空实现

- [ ] **Step 1: 创建 BaseAsyncTaskManager.cs**

```csharp
namespace ST.Core
{
    public abstract class BaseAsyncTaskManager : IManager
    {
        protected static BaseAsyncTaskManager s_Instance;

        public static BaseAsyncTaskManager instance
        {
            get { return s_Instance; }
        }

        public BaseAsyncTaskManager()
        {
            s_Instance = this;
        }

        public abstract void AddTask(AsyncTask asynctask);

        public override void DoLateUpdate() { }
    }
}
```

- [ ] **Step 2: 创建 AsyncTaskManager.cs**

```csharp
using System.Collections.Generic;

namespace ST.Core
{
    public class AsyncTaskManager : BaseAsyncTaskManager
    {
        List<AsyncTask> m_TaskList = new List<AsyncTask>(CommonDefine.s_ListConst_100);
        List<AsyncTask> m_TempTaskList = new List<AsyncTask>(CommonDefine.s_ListConst_16);

        public override void AddTask(AsyncTask asynctask)
        {
            m_TaskList.Add(asynctask);
        }

        public override void DoClose() { }

        public override void DoInit() { }

        public override void DoUpdate()
        {
            m_TempTaskList.Clear();

            for (int i = 0; i < m_TaskList.Count; i++)
            {
                var task = m_TaskList[i];
                if (task == null)
                    continue;

                task.Update();

                if (task.isEnd)
                    m_TempTaskList.Add(task);
            }

            for (int i = 0; i < m_TempTaskList.Count; i++)
            {
                var task = m_TempTaskList[i];
                if (task == null)
                    continue;

                m_TaskList.Remove(task);
            }

            m_TempTaskList.Clear();
        }
    }
}
```

- [ ] **Step 3: 验证**

Unity Editor Console 无编译错误。

---

## Task 8: Lua 支持类

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/Lua/LuaScriptableObject.cs`
- Create: `Runtime/Scripts/ResourceLoad/AssetDecorator/LuaAssetDecorator.cs`

源文件:
- `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\Lua\LuaScriptableObject.cs`
- `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\AssetDecorator\LuaAssetDecorator.cs`

- [ ] **Step 1: 创建 LuaScriptableObject.cs**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    public class LuaScriptableObject : ScriptableObject
    {
        [Serializable]
        public class LuaEntry
        {
            public string path;
            public byte[] data;
        }

        public List<LuaEntry> m_lualist = new List<LuaEntry>();

        public byte[] FindEntity(string path)
        {
            foreach (var entity in m_lualist)
            {
                if (entity == null)
                    continue;

                if (entity.path == path)
                    return entity.data;
            }

            return null;
        }

        public void AddEntry(string path, byte[] data)
        {
            LuaEntry entity = new LuaEntry();
            entity.path = path;
            entity.data = data;
            m_lualist.Add(entity);
        }

        public void Clear()
        {
            m_lualist.Clear();
        }
    }
}
```

- [ ] **Step 2: 创建 LuaAssetDecorator.cs**

```csharp
using System;
using UnityEngine;

namespace ST.Core
{
    public class LuaAssetDecorator : IAssetDecorator
    {
        public void BeforeLoad(ref string name, ref Type type)
        {
            if (type == typeof(string) || type == typeof(byte[]))
                type = typeof(TextAsset);
        }

        public void AfterLoad(string name, Type type, ref object asset)
        {
            if (asset == null)
                return;

            if (type == typeof(string))
                asset = (asset as TextAsset).text;
            else if (type == typeof(byte[]))
                asset = (asset as TextAsset).bytes;
        }
    }
}
```

- [ ] **Step 3: 验证**

Unity Editor Console 无编译错误。

---

## Task 9: 异步请求实现类（Bundle 依赖前置）

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncAssetRequest.cs`
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncBundleRequest.cs`
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/AsyncSceneRequest.cs`
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/EditorAsyncAssetRequest.cs`
- Create: `Runtime/Scripts/ResourceLoad/AsyncTask/EditorAsyncSceneRequest.cs`

注意：`AsyncAssetRequest`、`AsyncBundleRequest`、`AsyncSceneRequest` 引用 `Bundle` 类，`Bundle` 在 Task 10 中创建。Unity 同 Assembly 内编译不受顺序限制，可同步创建。

- [ ] **Step 1: 创建 AsyncAssetRequest.cs**

```csharp
using System;
using UnityEngine;

namespace ST.Core
{
    public class AsyncAssetRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_ResName;
        AssetBundleRequest m_Request;
        Type m_Type;

        public override float progress
        {
            get { return m_Request != null ? m_Request.progress : 0f; }
        }

        public AsyncAssetRequest(Bundle bundle, string resname, Type type, ResourceLoadComplete callback)
        {
            m_Bundle = bundle;
            m_ResName = resname;
            m_CompleteEvent = callback;
            m_Type = type;
        }

        protected override void OnEnd()
        {
            object val = m_Request != null ? m_Request.asset : null;
            m_CompleteEvent?.Invoke(val);
        }

        protected override void OnStart()
        {
            m_Request = m_Bundle.LoadAssetAsyncFromBundle(m_ResName, m_Type);
        }

        protected override ETaskState OnUpdate()
        {
            m_ProgressEvent?.Invoke(progress);
            bool isdone = m_Request != null && m_Request.isDone;
            return isdone ? ETaskState.Completed : ETaskState.Running;
        }
    }
}
```

- [ ] **Step 2: 创建 AsyncBundleRequest.cs**

变更：构造函数新增 `FilePathHelper` 参数，`OnStart` 用实例方法替换静态 `File.GetBundleFullPath`。

```csharp
using UnityEngine;

namespace ST.Core
{
    public class AsyncBundleRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_Path;
        FilePathHelper m_FilePathHelper;
        AssetBundleCreateRequest m_CreateRequest;

        public override float progress
        {
            get { return m_CreateRequest != null ? m_CreateRequest.progress : 0f; }
        }

        public AssetBundle assetBundle
        {
            get { return m_CreateRequest != null ? m_CreateRequest.assetBundle : null; }
        }

        public AsyncBundleRequest(Bundle bundle, string path, FilePathHelper filePathHelper, ResourceLoadComplete callback)
        {
            m_Bundle = bundle;
            m_Path = path;
            m_FilePathHelper = filePathHelper;
            m_CompleteEvent = callback;
        }

        protected override void OnEnd()
        {
            m_CompleteEvent?.Invoke(assetBundle);
        }

        protected override void OnStart()
        {
            var fullpath = m_FilePathHelper.GetBundleFullPath(m_Path);
            m_CreateRequest = AssetBundle.LoadFromFileAsync(fullpath);
        }

        protected override ETaskState OnUpdate()
        {
            bool isdone = m_CreateRequest != null && m_Bundle != null
                          && m_CreateRequest.isDone && m_Bundle.dependIsLoaded;
            return isdone ? ETaskState.Completed : ETaskState.Running;
        }
    }
}
```

- [ ] **Step 3: 创建 AsyncSceneRequest.cs**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ST.Core
{
    public class AsyncSceneRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_ResName;
        AsyncOperation m_Request;

        public override float progress
        {
            get { return m_Request != null ? m_Request.progress : 0f; }
        }

        public AsyncSceneRequest(Bundle bundle, string resname, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            m_Bundle = bundle;
            m_ResName = resname;
            m_ProgressEvent = progress;
            m_CompleteEvent = complete;
        }

        protected override void OnEnd()
        {
            object val = SceneManager.GetSceneByName(m_ResName);
            m_CompleteEvent?.Invoke(val);
        }

        protected override void OnStart()
        {
            m_Request = SceneManager.LoadSceneAsync(m_ResName, LoadSceneMode.Single);
        }

        protected override ETaskState OnUpdate()
        {
            m_ProgressEvent?.Invoke(progress);
            bool isdone = m_Request != null && m_Request.isDone;
            return isdone ? ETaskState.Completed : ETaskState.Running;
        }
    }
}
```

- [ ] **Step 4: 创建 EditorAsyncAssetRequest.cs**

```csharp
using System;
using UnityEngine;

namespace ST.Core
{
    public class EditorAsyncAssetRequest : AsyncTask
    {
        public override float progress { get { return 1.0f; } }

        public EditorAsyncAssetRequest(string respath, Type type)
        {
#if UNITY_EDITOR
            m_Asset = UnityEditor.AssetDatabase.LoadAssetAtPath(respath, type);
#endif
        }

        protected override void OnEnd()
        {
            m_CompleteEvent?.Invoke(m_Asset);
        }

        protected override void OnStart() { }

        protected override ETaskState OnUpdate()
        {
            return ETaskState.Completed;
        }
    }
}
```

- [ ] **Step 5: 创建 EditorAsyncSceneRequest.cs（含 bug 修复）**

原代码 `OnUpdate` 中 `iscomplete ? Running : Completed` 逻辑写反，此处修正。

```csharp
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ST.Core
{
    public class EditorAsyncSceneRequest : AsyncTask
    {
        AsyncOperation m_Operation;
        string m_ScenePath;

        public override float progress
        {
            get
            {
                if (m_Operation == null || m_Operation.isDone)
                    return 1f;
                return m_Operation.progress;
            }
        }

        public EditorAsyncSceneRequest(string scenepath)
        {
            m_ScenePath = scenepath;
            var param = new LoadSceneParameters(LoadSceneMode.Single);
            m_Operation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenepath, param);
        }

        protected override void OnEnd()
        {
            var asset = EditorSceneManager.GetSceneByPath(m_ScenePath);
            m_CompleteEvent?.Invoke(asset);
        }

        protected override void OnStart() { }

        protected override ETaskState OnUpdate()
        {
            m_ProgressEvent?.Invoke(progress);
            // 修复：原代码此处 Completed/Running 写反
            bool iscomplete = m_Operation != null && m_Operation.isDone;
            return iscomplete ? ETaskState.Completed : ETaskState.Running;
        }
    }
}

#endif
```

- [ ] **Step 6: 验证（Task 10 Bundle 创建后一并验证）**

---

## Task 10: Bundle 类

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/Bundle.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\Bundle.cs`

变更：
- `ConstDefine.LIST_CONST_8` → `CommonDefine.s_ListConst_8`
- `AssetBundleLoad` 引用保留（Task 11 创建）
- `LoadBundleSync` 中 `File.GetBundleFullPath` → `m_FilePathHelper.GetBundleFullPath`
- `LoadBundleAsync` 中创建 `AsyncBundleRequest` 时传入 `m_FilePathHelper`
- 新增 `FilePathHelper m_FilePathHelper` 字段，通过构造函数传入

- [ ] **Step 1: 创建 Bundle.cs**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    public class Bundle
    {
        enum State { Unloaded, Loading, Loaded }

        State m_State = State.Unloaded;
        string m_Path;
        AssetBundleLoad m_Load;
        FilePathHelper m_FilePathHelper;
        List<Bundle> m_DependList = new List<Bundle>(CommonDefine.s_ListConst_8);
        AssetBundle m_AssetBundle;
        List<AsyncTask> m_PendingLoadList = new List<AsyncTask>(CommonDefine.s_ListConst_8);

        public AssetBundleLoad load
        {
            get { return m_Load; }
            set { m_Load = value; }
        }

        public AssetBundle assetBundle
        {
            get { return m_AssetBundle; }
        }

        public bool isLoaded
        {
            get { return m_State == State.Loaded; }
        }

        public bool dependIsLoaded
        {
            get
            {
                foreach (var depend in m_DependList)
                {
                    if (depend == null) continue;
                    if (!depend.isLoaded) return false;
                }
                return true;
            }
        }

        public Bundle(string path, FilePathHelper filePathHelper)
        {
            m_Path = path;
            m_FilePathHelper = filePathHelper;
        }

        public void InitDependencies(AssetBundleManifest manifest)
        {
            if (m_Load == null || manifest == null)
                return;

            var dependarray = manifest.GetAllDependencies(m_Path);
            if (dependarray == null) return;

            foreach (var dependname in dependarray)
            {
                if (dependname == null) continue;
                var bundle = m_Load.GetBundle(dependname);
                if (bundle == null) continue;
                m_DependList.Add(bundle);
            }
        }

        public void LoadBundleAsync()
        {
            if (m_State == State.Unloaded)
            {
                m_State = State.Loading;

                foreach (var depend in m_DependList)
                {
                    if (depend == null) continue;
                    depend.LoadBundleAsync();
                }

                AsyncTask asynctask = new AsyncBundleRequest(this, m_Path, m_FilePathHelper, OnAsyncLoaded);
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            }
        }

        public void LoadBundleSync()
        {
            if (m_State == State.Unloaded)
            {
                m_State = State.Loaded;

                foreach (var depend in m_DependList)
                {
                    if (depend == null) continue;
                    depend.LoadBundleSync();
                }

                var fullpath = m_FilePathHelper.GetBundleFullPath(m_Path);
                m_AssetBundle = AssetBundle.LoadFromFile(fullpath);
            }
        }

        public object[] LoadAllSync()
        {
            LoadBundleSync();
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAllAssets();
        }

        public object LoadSync(string resname, Type type)
        {
            LoadBundleSync();
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAsset(resname, type);
        }

        public void LoadAsync(string resname, Type type, ResourceLoadComplete callback)
        {
            LoadBundleAsync();

            AsyncTask asynctask = new AsyncAssetRequest(this, resname, type, callback);
            if (isLoaded)
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            else
                m_PendingLoadList.Add(asynctask);
        }

        public void LoadSceneAsync(string resname, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            LoadBundleAsync();

            AsyncTask asynctask = new AsyncSceneRequest(this, resname, progress, complete);
            if (isLoaded)
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            else
                m_PendingLoadList.Add(asynctask);
        }

        public AssetBundleRequest LoadAssetAsyncFromBundle(string resname, Type type)
        {
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAssetAsync(resname, type);
        }

        void OnAsyncLoaded(object obj)
        {
            if (obj == null) return;
            var bundle = (AssetBundle)obj;
            if (bundle == null) return;

            m_State = State.Loaded;
            m_AssetBundle = bundle;

            foreach (var load in m_PendingLoadList)
            {
                if (load == null) continue;
                BaseAsyncTaskManager.instance.AddTask(load);
            }

            m_PendingLoadList.Clear();
        }
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误（含 Task 9 文件）。

---

## Task 11: AssetBundleLoad

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/AssetBundleLoad.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\AssetBundleLoad.cs`

变更：
- 构造函数接受 `FilePathHelper` 和 `IResourceConfig`
- `InitAllBundle` manifest 路径使用 `m_FilePathHelper.GetFilePath() + m_Config.appName + "/" + m_Config.appName`
- `LoadAllSync`/`LoadSync`/`LoadAsync`/`LoadSceneAsync` 中 `AppConst.BUNDLE_SUFFIX` → `m_Config.bundleSuffix`
- `ConstDefine.LIST_CONST_1024` → `CommonDefine.s_ListConst_1024`
- Bundle 创建后设置 `filePathHelper`

- [ ] **Step 1: 创建 AssetBundleLoad.cs**

```csharp
using System.Collections.Generic;
using System;
using UnityEngine;

namespace ST.Core
{
    public class AssetBundleLoad
    {
        Dictionary<string, Bundle> m_BundleDict = new Dictionary<string, Bundle>(CommonDefine.s_ListConst_1024);
        AssetBundleManifest m_Manifest = null;
        FilePathHelper m_FilePathHelper;
        IResourceConfig m_Config;

        public AssetBundleLoad(FilePathHelper filePathHelper, IResourceConfig config)
        {
            m_FilePathHelper = filePathHelper;
            m_Config = config;
        }

        public void DoInit()
        {
            InitAllBundle();
        }

        public Bundle GetBundle(string name)
        {
            Bundle bundle = null;
            m_BundleDict.TryGetValue(name, out bundle);
            return bundle;
        }

        public object[] LoadAllSync(string respath)
        {
            var fullpath = respath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return null;
            return bundle.LoadAllSync();
        }

        public object LoadSync(string respath, string filename, Type type)
        {
            var fullpath = respath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return null;
            return bundle.LoadSync(filename, type);
        }

        public void LoadAsync(string realpath, string filename, Type type, ResourceLoadComplete callback)
        {
            var fullpath = realpath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return;
            bundle.LoadAsync(filename, type, callback);
        }

        public void LoadSceneAsync(string realpath, string filename, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            var fullpath = realpath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return;
            bundle.LoadSceneAsync(filename, progress, complete);
        }

        void InitAllBundle()
        {
            string manifestPath = m_FilePathHelper.GetFilePath() + m_Config.appName + "/" + m_Config.appName;
            var manifestBundle = AssetBundle.LoadFromFile(manifestPath);

            if (manifestBundle != null)
            {
                m_Manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                var allbundlearray = m_Manifest.GetAllAssetBundles();
                if (allbundlearray != null)
                {
                    foreach (var bundlename in allbundlearray)
                    {
                        if (bundlename == null) continue;

                        var bundle = new Bundle(bundlename, m_FilePathHelper);
                        bundle.load = this;
                        bundle.InitDependencies(m_Manifest);

                        if (!m_BundleDict.ContainsKey(bundlename))
                            m_BundleDict.Add(bundlename, bundle);
                    }
                }

                manifestBundle.Unload(true);
            }
        }
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误。

---

## Task 12: EditorResourceLoad

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/EditorResourceLoad.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\EditorResourceLoad.cs`

变更：
- 移除静态常量 `EDITOR_PATH_PREFIX`，改为通过 `IResourceConfig` 传入
- 构造函数接受 `IResourceConfig`

- [ ] **Step 1: 创建 EditorResourceLoad.cs**

```csharp
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ST.Core
{
    public class EditorResourceLoad
    {
        readonly IResourceConfig m_Config;

        public EditorResourceLoad(IResourceConfig config)
        {
            m_Config = config;
        }

        public object LoadSync(string realpath, string suffix, Type type)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            return UnityEditor.AssetDatabase.LoadAssetAtPath(loadpath, type);
#else
            return null;
#endif
        }

        public object[] LoadAllSync(string realpath, string suffix)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(loadpath);
#else
            return null;
#endif
        }

        public void LoadAsync(string realpath, string suffix, Type type, ResourceLoadComplete callback)
        {
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            AsyncTask asynctask = new EditorAsyncAssetRequest(loadpath, type);
            if (callback != null)
                asynctask.completeEvent = callback;
            BaseAsyncTaskManager.instance.AddTask(asynctask);
        }

        public void LoadSceneAsync(string realpath, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            AsyncTask asynctask = new EditorAsyncSceneRequest(loadpath);
            asynctask.progressEvent = progress;
            asynctask.completeEvent = complete;
            BaseAsyncTaskManager.instance.AddTask(asynctask);
#endif
        }
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误。

---

## Task 13: ResourceLoad 主实现

**Files:**
- Create: `Runtime/Scripts/ResourceLoad/ResourceLoad.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\ResourceLoad\ResourceLoad.cs`

变更：
- `AssetDecorator` → `IAssetDecorator`
- `EditorResourceLoad` 构造传入 `m_Config`
- `AssetBundleLoad` 构造传入 `m_FilePathHelper` 和 `m_Config`
- `DoInit` 改为在 `SetConfig` 之后调用（业务层负责顺序），或在 `DoInit` 内检查 `m_Config`

- [ ] **Step 1: 创建 ResourceLoad.cs**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    public class ResourceLoad : BaseResourceLoad
    {
        public static bool useAssetBundle = false;

        EditorResourceLoad m_EditorResLoad;
        AssetBundleLoad m_AssetBundleLoad;

        public override void DoClose() { }

        public override void DoInit()
        {
            m_EditorResLoad = new EditorResourceLoad(m_Config);
            m_AssetBundleLoad = new AssetBundleLoad(m_FilePathHelper, m_Config);
            m_AssetBundleLoad.DoInit();

            InstallDecorator(new LuaAssetDecorator());
        }

        public override void DoUpdate() { }

        public override object[] LoadAllResourceSync(string path, string filename, string suffix)
        {
            string realpath = path + filename;

#if UNITY_EDITOR
            if (!useAssetBundle)
                return m_EditorResLoad.LoadAllSync(realpath, suffix);
            else
                return m_AssetBundleLoad.LoadAllSync(realpath);
#else
            return m_AssetBundleLoad.LoadAllSync(realpath);
#endif
        }

        public override object LoadResourceSync(string path, string filename, string suffix, ResourceType restype = ResourceType.Default)
        {
            string realpath = path + filename;
            var type = Type2Native(restype);
            var originType = type;
            BeforeLoad(ref realpath, ref type);

            object obj = null;

#if UNITY_EDITOR
            if (!useAssetBundle)
                obj = m_EditorResLoad.LoadSync(realpath, suffix, type);
            else
                obj = m_AssetBundleLoad.LoadSync(realpath, filename, type);
#else
            obj = m_AssetBundleLoad.LoadSync(realpath, filename, type);
#endif

            return AfterLoad(realpath, originType, obj);
        }

        public override void LoadResourceAsync(string path, string filename, string suffix, ResourceLoadComplete callback, ResourceType restype = ResourceType.Default)
        {
            string realpath = path + filename;
            var type = Type2Native(restype);
            var originType = type;
            BeforeLoad(ref realpath, ref type);

#if UNITY_EDITOR
            if (!useAssetBundle)
            {
                m_EditorResLoad.LoadAsync(realpath, suffix, type, (obj) => {
                    ResourceAsyncCallback(realpath, originType, obj, callback);
                });
            }
            else
            {
                m_AssetBundleLoad.LoadAsync(realpath, filename, type, (obj) => {
                    ResourceAsyncCallback(realpath, originType, obj, callback);
                });
            }
#else
            m_AssetBundleLoad.LoadAsync(realpath, filename, type, (obj) => {
                ResourceAsyncCallback(realpath, originType, obj, callback);
            });
#endif
        }

        public override void LoadSceneAsync(string path, string filename, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            string realpath = path + filename;

#if UNITY_EDITOR
            if (!useAssetBundle)
                m_EditorResLoad.LoadSceneAsync(realpath, suffix, progress, complete);
            else
                m_AssetBundleLoad.LoadSceneAsync(realpath, filename, suffix, progress, complete);
#else
            m_AssetBundleLoad.LoadSceneAsync(realpath, filename, suffix, progress, complete);
#endif
        }

        void ResourceAsyncCallback(string assetName, Type type, object obj, ResourceLoadComplete callback)
        {
            // AfterLoad 返回装饰后的对象，必须用其结果调用 callback
            var decoratedObj = AfterLoad(assetName, type, obj);
            callback?.Invoke(decoratedObj);
        }

        void BeforeLoad(ref string assetName, ref Type type)
        {
            for (var i = m_Decorators.Count - 1; i >= 0; --i)
                m_Decorators[i].BeforeLoad(ref assetName, ref type);
        }

        object AfterLoad(string assetName, Type type, object asset)
        {
            for (var i = 0; i < m_Decorators.Count; ++i)
                m_Decorators[i].AfterLoad(assetName, type, ref asset);
            return asset;
        }
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误，全部 Runtime 文件编译通过。

---

## Task 14: EditorUtil

**Files:**
- Create: `Editor/Scripts/ResourceLoad/EditorUtil.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\Package\Editor\Util.cs`

变更：命名空间改为 `ST.Core.Editor`，类名改为 `EditorUtil`。

- [ ] **Step 1: 确认 Editor 目录存在**

```powershell
New-Item -ItemType Directory -Force -Path "D:\xieliujian\com.spacetime.core\Packages\com.spacetime.core\Editor\Scripts\ResourceLoad"
```

- [ ] **Step 2: 创建 EditorUtil.cs**

```csharp
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace ST.Core.Editor
{
    class EditorUtil
    {
        public static System.Diagnostics.Process CreateShellExProcess(string cmd, string args, string workingDir = "")
        {
#if UNITY_EDITOR_OSX
            var pStartInfo = new System.Diagnostics.ProcessStartInfo();
            pStartInfo.FileName = "/bin/sh";
            pStartInfo.UseShellExecute = false;
            pStartInfo.RedirectStandardOutput = true;
            pStartInfo.Arguments = cmd;
            if (!string.IsNullOrEmpty(workingDir))
                pStartInfo.WorkingDirectory = workingDir;
            var p = System.Diagnostics.Process.Start(pStartInfo);
            string strOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return p;
#else
            var pStartInfo = new System.Diagnostics.ProcessStartInfo(cmd);
            pStartInfo.Arguments = args;
            pStartInfo.CreateNoWindow = false;
            pStartInfo.UseShellExecute = true;
            pStartInfo.RedirectStandardError = false;
            pStartInfo.RedirectStandardInput = false;
            pStartInfo.RedirectStandardOutput = false;
            if (!string.IsNullOrEmpty(workingDir))
                pStartInfo.WorkingDirectory = workingDir;
            return System.Diagnostics.Process.Start(pStartInfo);
#endif
        }

        public static void RunBat(string batfile, string args, string workingDir = "")
        {
            var p = CreateShellExProcess(batfile, args, workingDir);
            p.Close();
        }

        public static string FormatPath(string path)
        {
            path = path.Replace("/", "\\");
            if (Application.platform == RuntimePlatform.OSXEditor)
                path = path.Replace("\\", "/");
            return path;
        }

        public static string Md5file(string file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                    sb.Append(retVal[i].ToString("x2"));
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Md5file() fail, error:" + ex.Message);
            }
        }

        public static bool ExecuteProcess(string file, string args)
        {
            var info = new ProcessStartInfo()
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                ErrorDialog = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(file),
            };

            var err = string.Empty;
            using (var process = Process.Start(info))
            {
                err = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(err))
                    UnityEngine.Debug.LogError(err);
                else
                    UnityEngine.Debug.Log(file + args);
                process.WaitForExit();
            }

            return string.IsNullOrEmpty(err);
        }
    }
}
```

- [ ] **Step 3: 验证**

Unity Editor Console 无编译错误。

---

## Task 15: Packager 打包工具

**Files:**
- Create: `Editor/Scripts/ResourceLoad/Packager.cs`

源文件: `D:\xieliujian\UnityDemo_ResourceLoad_V1\Assets\GTM\Scripts\Package\Editor\Packager.cs`

变更：
- 命名空间 `gtm.Editor` → `ST.Core.Editor`
- 菜单路径 `"unityframework/..."` → `"ST/..."`
- 新增 `static IResourceConfig s_Config` + `RegisterConfig(IResourceConfig)`
- `AppConst.APP_NAME` → `s_Config.appName`
- `EditorResourceLoad.EDITOR_PATH_PREFIX`（6 处）→ `s_Config.editorPathPrefix`
- `AppPlatform.streamingAssetsPath` → `AppPlatform.GetStreamingAssetsPath(s_Config.appName)`
- `AppPlatform.GetPackageResPath(target)` → `AppPlatform.GetPackageResPath(target, s_Config.appName)`

- [ ] **Step 1: 创建 Packager.cs**

```csharp
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using ST.Core;

namespace ST.Core.Editor
{
    public class Packager
    {
        public enum EAndroidBuildPlatform { NoPlatform }

        static IResourceConfig s_Config;

        /// <summary>业务项目在 InitializeOnLoad 中调用此方法注入配置。</summary>
        public static void RegisterConfig(IResourceConfig config)
        {
            s_Config = config;
        }

        static string[] TexturePackageDir = { "ui/icon/", "ui/image/" };
        static string[] FontPackageDir = { "font/" };
        static string[] AudioMusicPackageDir = { "audio/music/" };
        static string[] AudioSoundPackageDir = { "audio/sound/" };
        static string[] PrefabPackageDir = { "prefabs/", "ui/uiprefab/" };
        static string[] SceneDir = { "scene/" };

        const string LuaCodePath = "/Lua/";
        const string LuaCExeRelativePath = "/tools/xlua_v2.1.14/build/luac/build64/Release/luac.exe";
        const string LuaCExeMacRelativePath = "/tools/xlua_v2.1.14/build/luac/build_unix/luac";
        const string LuaCSrcFileRelativePath = "/Assets/Lua/";
        const string LuaCGenFileRelativePath = "/Temp/";
        const string LuaPath = "config/lua/";
        const string LuaFileName = "luapackage";
        const string Lua_Suffix = ".asset";
        const string Bytes_Suffix = ".bytes";

        static List<AssetBundleBuild> m_BundleBuildList = new List<AssetBundleBuild>();

        [MenuItem("ST/Build iPhone Resource", false, 100)]
        public static void BuildiPhoneResource()
        {
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "24AZABCKN4";
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            BuildAssetResource(BuildTarget.iOS, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        [MenuItem("ST/Build Android Resource", false, 101)]
        public static void BuildAndroidResource()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            BuildAssetResource(BuildTarget.Android, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        [MenuItem("ST/Build Windows Resource", false, 102)]
        public static void BuildWindowsResource()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            BuildAssetResource(BuildTarget.StandaloneWindows, AppPlatform.GetStreamingAssetsPath(s_Config.appName));
        }

        [MenuItem("ST/Package All Resource", false, 103)]
        public static void PackageAllResource()
        {
            BuildAssetResource(BuildTarget.StandaloneWindows, AppPlatform.GetPackageResPath(BuildTarget.StandaloneWindows, s_Config.appName));
            BuildAssetResource(BuildTarget.Android, AppPlatform.GetPackageResPath(BuildTarget.Android, s_Config.appName));
            BuildAssetResource(BuildTarget.iOS, AppPlatform.GetPackageResPath(BuildTarget.iOS, s_Config.appName));

            BuildTargetGroup curtargetgroup = AppPlatform.GetCurBuildTargetGroup();
            BuildTarget curtarget = AppPlatform.GetCurBuildTarget();
            EditorUserBuildSettings.SwitchActiveBuildTarget(curtargetgroup, curtarget);
        }

        static void BuildAssetResource(BuildTarget target, string resPath)
        {
            m_BundleBuildList.Clear();

            if (Directory.Exists(resPath))
                Directory.Delete(resPath, true);

            Directory.CreateDirectory(resPath);
            AssetDatabase.Refresh();

            GenerateLuaScriptableObject();

            string packagePathPrefix = "/Package/";
            AddAllAssetBundle(Application.dataPath + packagePathPrefix);

            BuildPipeline.BuildAssetBundles(resPath, m_BundleBuildList.ToArray(), BuildAssetBundleOptions.None, target);
            AssetDatabase.Refresh();
        }

        static void AddAssetBundleBuild(string assetBundleName, string[] assetNames, string assetBundleVariant = "unity3d")
        {
            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = assetBundleName;
            build.assetBundleVariant = assetBundleVariant;
            build.assetNames = assetNames;
            m_BundleBuildList.Add(build);
        }

        static void PackageFont(string rootpath)
        {
            foreach (var subdir in FontPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.ttf", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".ttf", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        static void PackageAudio(string rootpath)
        {
            foreach (var subdir in AudioMusicPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.mp3", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".mp3", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            foreach (var subdir in AudioSoundPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.ogg", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".ogg", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        static void PackageTexture(string rootpath)
        {
            foreach (var subdir in TexturePackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.png", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".png", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        static void PackagePrefab(string rootpath)
        {
            foreach (var subdir in PrefabPackageDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.prefab", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".prefab", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        static void PackageScene(string rootpath)
        {
            foreach (var subdir in SceneDir)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(rootpath + subdir);
                foreach (FileInfo file in dirInfo.GetFiles("*.unity", SearchOption.AllDirectories))
                {
                    string source = file.FullName.Replace("\\", "/");
                    string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                    var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(".unity", "");
                    AddAssetBundleBuild(bundlePath, new string[] { assetpath });
                }
            }
            AssetDatabase.Refresh();
        }

        static void PackageConfig_Lua(string rootpath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(rootpath + LuaPath);
            foreach (FileInfo file in dirInfo.GetFiles("*.asset", SearchOption.AllDirectories))
            {
                string source = file.FullName.Replace("\\", "/");
                string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(Lua_Suffix, "");
                AddAssetBundleBuild(bundlePath, new string[] { assetpath });
            }
            foreach (FileInfo file in dirInfo.GetFiles("*.bytes", SearchOption.AllDirectories))
            {
                string source = file.FullName.Replace("\\", "/");
                string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                var bundlePath = assetpath.Replace(s_Config.editorPathPrefix, "").Replace(Bytes_Suffix, "");
                AddAssetBundleBuild(bundlePath, new string[] { assetpath });
            }
            AssetDatabase.Refresh();
        }

        static void AddAllAssetBundle(string rootpath)
        {
            PackageFont(rootpath);
            PackageTexture(rootpath);
            PackageAudio(rootpath);
            PackagePrefab(rootpath);
            PackageScene(rootpath);
            PackageConfig_Lua(rootpath);
        }

        public static void GenerateLuaScriptableObject()
        {
            string packagePathPrefix = "/Package/";
            var path = "Assets/" + packagePathPrefix + LuaPath + LuaFileName + Lua_Suffix;
            var obj = AssetDatabase.LoadAssetAtPath<LuaScriptableObject>(path);
            if (obj == null)
            {
                obj = UnityEngine.ScriptableObject.CreateInstance<LuaScriptableObject>();
                AssetDatabase.CreateAsset(obj, path);
            }
            else
            {
                obj.Clear();
            }

            var luacodepath = Application.dataPath + LuaCodePath;
            luacodepath = luacodepath.Replace("\\", "/");
            var luafilearray = Directory.GetFiles(luacodepath, "*.lua", SearchOption.AllDirectories);
            foreach (var luafile in luafilearray)
            {
                var luapath = luafile.Replace("\\", "/");
                var subluapath = luapath.Substring(luacodepath.Length);

                var destFile = Environment.CurrentDirectory + LuaCGenFileRelativePath + subluapath;
                var destfiledir = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(destfiledir))
                    Directory.CreateDirectory(destfiledir);

                var srcFile = Environment.CurrentDirectory + LuaCSrcFileRelativePath + subluapath;
                var exepath = Environment.CurrentDirectory + LuaCExeRelativePath;
#if UNITY_IOS
                exepath = Environment.CurrentDirectory + LuaCExeMacRelativePath;
#endif
                var args = " -o " + destFile + " " + srcFile;
                if (!EditorUtil.ExecuteProcess(exepath, args))
                    continue;

                var luacode = System.IO.File.ReadAllBytes(destFile);
                System.IO.File.Delete(destFile);
                obj.AddEntry(subluapath, luacode);
            }

            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        static string GetAndroidBuildPath(EAndroidBuildPlatform platform)
        {
            string prefix = "../../gameapp/";
            if (platform == EAndroidBuildPlatform.NoPlatform)
                return string.Format("{0}{1}.apk", prefix, s_Config.appName);
            return "";
        }

        static List<string> GetAllScenes()
        {
            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                scenes.Add(scene.path);
            }
            return scenes;
        }
    }
}
```

- [ ] **Step 2: 验证**

Unity Editor Console 无编译错误，菜单栏出现 `ST/` 菜单组（需配置 `s_Config` 后方可正常调用）。

---

## 最终验证

- [ ] **所有文件创建完成后，在 Unity Editor 打开工程**
- [ ] **确认 Console 面板无任何编译错误**
- [ ] **确认菜单 `ST/Build Windows Resource` 等项目存在**
- [ ] **检查 `Runtime/Scripts/ResourceLoad/` 目录下共 19 个 `.cs` 文件**
- [ ] **检查 `Editor/Scripts/ResourceLoad/` 目录下共 2 个 `.cs` 文件**
