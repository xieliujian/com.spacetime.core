# 资源打包（Editor）

[← 返回主页](../../README.md)

## 概述

`Editor/Scripts/Build` 提供 C# 侧的 AssetBundle 打包流程：按资源类别收集构建项、调用
`BuildPipeline`、生成五个客户端数据库并清理 Unity 清单。入口只有命令行，不提供编辑器菜单。

这套流程只负责「Unity 生成 AssetBundle 与客户端数据库」。发布、上传、版本激活、合包与平台包体
不在本模块内。

## 目录结构

```text
Editor/Scripts/Build/
├── Builder/Builder.cs              总入口：参数解析、收集调度、构建、写库、清理
├── Builders/
│   ├── ResourceBuilderBase.cs      按前缀过滤并读取 AssetDatabase
│   ├── UIBuilder.cs                ui/
│   ├── CharacterBuilder.cs         character/
│   ├── ParticleBuilder.cs          particle/
│   ├── SceneBuilder.cs             scene/ 与全局资源
│   ├── StoryBuilder.cs             story/
│   ├── ShaderBuilder.cs            depend/shader_
│   └── AudioBuilder.cs             audio/
├── Config/
│   ├── BuildDefine.cs              常量与枚举
│   ├── ResourceBuildOptions.cs     平台、输出目录、类别与换行策略
│   └── ResourceBuildContext.cs     日志、错误与 Bundle 名缓存
└── Database/
    ├── AssetBundleDatabaseTypes.cs Redirect、记录与数据库
    ├── AssetBundleDatabaseRouter.cs 路径到数据库的归属
    ├── AssetBundleDatabaseWriter.cs 校验与确定性编码
    └── AssetBundleDatabaseSet.cs   五库聚合、manifest 转换与写出
```

## 入口

唯一入口是 `ST.Core.Builder.Build`。

```text
Unity.exe -batchmode -quit -projectPath <project>
  -executeMethod ST.Core.Builder.Build
  -BUILD_UI -BUILD_CHARACTER
  BUILD_TARGET_NAME=android
  RES_OUT_PUT_DIR_PATH=<输出绝对路径>
```

参数同时支持旧工程开关和 `-resource*` 形式。

| 参数 | 说明 |
| --- | --- |
| `-BUILD_UI` | 打包 UI |
| `-BUILD_CHARACTER` | 打包角色 |
| `-BUILD_PARTICLE` | 打包特效 |
| `-BUILD_SCENE` | 打包场景与全局资源 |
| `-BUILD_STORY` | 打包剧情 |
| `-BUILD_AUDIO` | 打包音频 |
| `-BUILD_SHADER` | 打包 shader 共享依赖 |
| `BUILD_TARGET_NAME=android|ios|standalone` | 目标平台 |
| `RES_OUT_PUT_DIR_PATH=<绝对路径>` | 输出根目录 |
| `-resourcePlatform android|ios|windows` | 目标平台的新式写法 |
| `-resourceOutput <绝对路径>` | 输出根目录的新式写法 |
| `-resourceScopes ui,character,shared` | 逗号分隔的类别名 |
| `-resourceClean` | 打包前清空输出目录 |

出现任一类别开关或 `-resourceScopes` 时，未列出的类别不会参与本次打包。完全没有类别参数时
按默认启用全部类别。输出目录缺省为 `FilePath.streamingAssetsPath`，编辑器下就是工程根目录的
`BuildRes/`。

批量模式下失败会以非零状态退出。

## 构建器

| 构建器 | 类别 | 接受前缀 | 记录去向 |
| --- | --- | --- | --- |
| `UIBuilder` | UI | `ui/` | UI 库 |
| `CharacterBuilder` | 角色 | `character/` | 角色库 |
| `ParticleBuilder` | 特效 | `particle/` | 特效库 |
| `SceneBuilder` | 场景 | `scene/`、`lingren/resources/global/` | 场景库 |
| `StoryBuilder` | 剧情 | `story/` | 场景库 |
| `AudioBuilder` | 音频 | `audio/` | other 库 |
| `ShaderBuilder` | 共享依赖 | `depend/shader_*.assetbundle` | 复制到五个库 |

`ShaderBuilder` 随任意类别一起运行，保证依赖包存在。它只接受 `depend/shader_` 开头且以
`.assetbundle` 结尾的包，其它 `depend/` 包会输出告警。

自定义类别可以继承 `ResourceBuilderBase`，重写 `IsBundleAccepted` 收紧命名规则，或重写
`Collect` 追加检查，然后通过 `ResourceBuildOptions.AddBuilder` 注入。

## 打包流程

1. 解析命令行参数，切换 Unity 构建目标。
2. 创建或清空输出目录，刷新 AssetDatabase。
3. 每个构建器从 `AssetDatabase.GetAllAssetBundleNames` 中按前缀取包，再读取包内资源路径。
4. 合并同名 Bundle，检查资源重复引用、名字长度与 `.assetbundle` 后缀。
5. 调用 `BuildPipeline.BuildAssetBundles`，默认使用
   `ChunkBasedCompression | IgnoreTypeTreeChanges`，移动端打 UI 或场景时改用
   `ChunkBasedCompression | DisableWriteTypeTree`。
6. 从 `AssetBundleManifest` 生成五个数据库并写出。
7. 删除 Unity 生成的根清单与所有 `*.manifest`，可选写出 `builds.txt`。

## 输出

```text
<输出根目录>/
├── assetbundledb_scene.txt
├── assetbundledb_ui.txt
├── assetbundledb_other.txt
├── assetbundledb_character.txt
├── assetbundledb_particle.txt
├── builds.txt
├── ui/...
├── character/...
├── particle/...
├── scene/...
├── story/...
├── audio/...
└── depend/shader_*.assetbundle
```

`builds.txt` 每行是一个 Bundle 名，随后是缩进的资源路径，用于排查漏包与重复引用。

## 数据库格式

```text
<Bundle 逻辑路径>\t<库内序号>
\tDepend:<依赖序号>\t<依赖序号>...
\tRedirect:<容器序号>\t<偏移>\t<长度>
```

编码规则与旧客户端一致。

1. 记录按名字做序数排序，序号从零连续分配。
2. `Depend` 与 `Redirect` 每个库最多各一行，缺省时整行不输出。
3. 依赖与 Redirect 都使用同一个库内的序号。
4. 换行统一为 LF，文件末尾保留一个换行，编码为不带 BOM 的 UTF-8。
5. 空库输出零字节文件。

路由规则固定，未知前缀会中止打包。

| 前缀 | 数据库 |
| --- | --- |
| `scene/`、`story/`、`lingren/resources/global/` | `assetbundledb_scene.txt` |
| `ui/` | `assetbundledb_ui.txt` |
| `texture/`、`skill/`、`audio/` | `assetbundledb_other.txt` |
| `character/` | `assetbundledb_character.txt` |
| `particle/` | `assetbundledb_particle.txt` |
| `depend/shader_*.assetbundle` | 全部五个库 |

## 资源命名前提

构建器只读取 `AssetImporter.assetBundleName`，不会自己生成名字。工程里的资源规则必须先执行，
否则收集结果为空。

```csharp
AssetImporter importer = AssetImporter.GetAtPath(assetPath);
importer.assetBundleName = "ui/panelui/mainpanel.assetbundle";
importer.SaveAndReimport();
```

## 当前限制

1. `texture/` 与 `skill/` 已经路由到 other 库，但还没有独立构建器。
2. `audio/` 在旧客户端是 FMOD bank 原始文件。这里按基础流程把音频 Bundle 写入 other 库，
   如果音频继续以原始文件下发，需要改为拷贝流程。
3. Redirect 只支持编码已有的容器切片，合包算法不在本模块。
4. 运行时 `AssetBundleLoad` 目前只初始化单个数据库，消费五个库需要先完成多库改造。
5. 打包代码尚未在 Unity 内做过完整构建验证。

## 相关文档

- [资源加载系统](../ResourceLoad/ResourceModule.md)
- [资源打包框架设计](../../.agents/superpowers/specs/2026-11-24-resource-build-framework-design.md)

[← 返回主页](../../README.md)
