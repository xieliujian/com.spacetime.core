# 原始文件访问

[← 返回主页](../../README.md)

按相对路径读取包内或外存文件，外存优先，包内兜底。适用于热更新清单、Lua 脚本、表文件、配置文件、音频 bank、场景 bin 这类不走 AssetBundle 的原始资源。

---

## 快速开始

```csharp
using ST.Core;

// 读取全部字节，外存存在时读外存，否则读包内
byte[] data = FileMgr.ReadSync("skill/1001.bin");

// 只要路径，交给外部库自己加载
string absolutePath = FileMgr.GetResPathInPersistentOrStream("character/characterdatabase.assetbundle");

// 判断是否存在
bool isExist = FileMgr.IsFileExists("appconfig.json");

// 线程里读取，调用方先解析好路径与来源
string path = FileMgr.GetResPathInPersistentOrStream(relativePath);
bool isInExternal = FileMgr.IsInExternal(relativePath);
byte[] result = FileMgr.ReadSyncInThread(relativePath, path, isInExternal);
```

不注入任何实现时，文件层按外存目录扫描加 `System.IO` 读取，Editor 与 Windows 播放器可以直接使用。

---

## 目录约定

| 目录 | 路径 | 说明 |
|------|------|------|
| 包内根目录 | Editor 与 Windows 播放器为 `<工程或安装目录>/BuildRes/`，移动端为 `StreamingAssets/` | `FilePath.streamingAssetsPath` |
| 外存根目录 | `Application.persistentDataPath` | `FilePath.persistentDataPath` |
| 外存资源目录 | `<外存根目录>/Res/` | `FilePath.persistentDataPath4Res` |

原始资源放在 `BuildRes/` 根下，AssetBundle 放在 `BuildRes/<appName>/` 下。两个目录共用同一个根，`FilePathHelper` 与 `AppPlatform` 都从 `FilePath.streamingAssetsPath` 取值。

---

## 注入外存清单

热更新工程通常有一份外存清单，记录哪些资源已经下载。实现 `IFileResolvePolicy` 后，清单可用时只查清单，不再逐次探测磁盘。

```csharp
public sealed class ProjectFileResolvePolicy : IFileResolvePolicy
{
    public bool IsManifestAuthoritative()
    {
        FileListModule fileListModule = GameWorld.gameKernel.GetModule<FileListModule>();
        if (fileListModule == null)
        {
            return false;
        }

        bool isDataReady = fileListModule.IsDataReady();
        return isDataReady;
    }

    public bool ExistInExternal(string relativePath)
    {
        FileListModule fileListModule = GameWorld.gameKernel.GetModule<FileListModule>();
        if (fileListModule == null)
        {
            return false;
        }

        bool isExist = fileListModule.ExistInExternal(relativePath);
        return isExist;
    }
}

FileMgr.SetResolvePolicy(new ProjectFileResolvePolicy());
```

清单权威时，清单里记录但磁盘上被删掉的文件仍然解析到外存路径，读取失败。这条语义用于暴露热更新残留问题。

---

## 注入平台实现

Android 真机的包内文件在 APK 里，`System.IO` 读不到，需要原生能力。同时 Android 的外存根目录与分包 dataPath 也可能由原生层决定。实现 `IPlatformFileProvider` 后注入。

```csharp
FilePath.SetPlatformProvider(new AndroidPlatformFileProvider());
```

未注入时 Android 包内读取会记录错误日志并返回失败，不会静默返回空数据。

---

## 启动顺序

`FilePath` 会缓存根目录。平台 native 初始化完成后调用一次 `ResetCache`，再注入 platform provider。

```csharp
AndroidNative.SDKInit();
FilePath.SetPlatformProvider(new AndroidPlatformFileProvider());
FileMgr.SetResolvePolicy(new ProjectFileResolvePolicy());
```

`SetPlatformProvider` 内部会调用 `ResetCache`，只需要在 provider 之前准备好 native 环境。

---

## API 一览

| 方法 | 说明 |
|------|------|
| `ReadSync(string)` | 读取全部字节，失败返回 null |
| `ReadSync(string, byte[])` | 读入缓冲区，返回实际字节数，失败返回 -1 |
| `ReadSyncInThread(string, string, bool)` | 按已解析路径读取，可在子线程调用 |
| `ReadSyncInternal(string)` | 只读包内 |
| `ReadSyncExtenal(string)` | 只读外存 |
| `ReadSyncByAbsoluteFilePath(string)` | 按绝对路径读取 |
| `GetResPathInPersistentOrStream(string)` | 返回外存或包内绝对路径 |
| `IsInExternal(string)` | 是否命中外存 |
| `IsFileExists(string)` | 文件是否存在 |
| `WriteString2File(string, string)` | UTF-8 覆盖写文本 |
| `CopyDataFromAndroidApk(string, string, out string)` | 包内文件拷贝到外存 |
| `SetResolvePolicy(IFileResolvePolicy)` | 注入清单判定 |
| `isAccessLogEnabled` | 是否记录资源访问日志 |
| `fileLogger` | 访问日志写入器（`LogFileWriter`），供调试导出 |

---

## 注意事项

- `ReadSync` 内部会解析路径并打开文件，不适合在高频路径上每帧调用。
- `FileMgr` 是静态类，没有生命周期方法。业务退出前按需调用 `fileLogger.Close()`。
- 资源访问日志写入 `<外存根目录>/Temp/FileLog.txt`，默认关闭，`FORCE_RES_LOG` 宏下默认开启。
- `FilePath.ResetCache()` 会清空所有根目录缓存，平台环境变化后必须调用。

---

## 许可

MIT License

---
[← 返回主页](../../README.md)

