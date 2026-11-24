using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ST.Core.Logging;
using UnityEditor;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 资源打包总入口：解析命令行参数、按类别收集 AssetBundle、构建并生成五个客户端数据库。
    /// </summary>
    public static class Builder
    {
        /// <summary>原始构建项。</summary>
        static List<AssetBundleBuild> s_BuildList =
            new List<AssetBundleBuild>(BuildDefine.BUILD_LIST_CAPACITY);

        /// <summary>合并后的构建项。</summary>
        static List<AssetBundleBuild> s_MergedBuildList =
            new List<AssetBundleBuild>(BuildDefine.BUILD_LIST_CAPACITY);

        /// <summary>本次启用的构建器。</summary>
        static List<ResourceBuilderBase> s_Builders = new List<ResourceBuilderBase>(8);

        /// <summary>自定义构建器复用容器。</summary>
        static List<ResourceBuilderBase> s_CustomBuilders = new List<ResourceBuilderBase>(4);

        /// <summary>Bundle 名到资源列表的映射。</summary>
        static Dictionary<string, List<string>> s_BundleAssetMap =
            new Dictionary<string, List<string>>(BuildDefine.BUILD_LIST_CAPACITY);

        /// <summary>Bundle 首次出现顺序。</summary>
        static List<string> s_BundleOrder = new List<string>(BuildDefine.BUILD_LIST_CAPACITY);

        /// <summary>资源路径到 Bundle 名的映射。</summary>
        static Dictionary<string, string> s_AssetOwnerMap =
            new Dictionary<string, string>(BuildDefine.ASSET_MAP_CAPACITY);

        /// <summary>路由复用容器。</summary>
        static List<string> s_TempDatabaseNames = new List<string>(BuildDefine.DATABASE_COUNT);

        /// <summary>不带 BOM 的 UTF-8 编码。</summary>
        static UTF8Encoding s_Utf8NoBom = new UTF8Encoding(false);

        /// <summary>命令行入口，读取启动参数并执行一次打包。</summary>
        public static void Build()
        {
            ResourceBuildOptions options = new ResourceBuildOptions();
            string[] args = Environment.GetCommandLineArgs();
            ApplyCommandLineArguments(options, args);

            bool isBuilt = Build(options);
            bool isFailed = !isBuilt;
            if (isFailed)
            {
                bool isBatchMode = Application.isBatchMode;
                if (isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        /// <summary>按给定选项执行一次完整资源打包。</summary>
        /// <param name="options">打包选项。</param>
        /// <returns>全部步骤成功返回 true。</returns>
        public static bool Build(ResourceBuildOptions options)
        {
            bool isOptionsNull = options == null;
            if (isOptionsNull)
            {
                STLogger.LogError(BuildDefine.LOG_OPTIONS_NULL);
                return false;
            }

            ResourceBuildContext context = new ResourceBuildContext(options);
            string outputRootPath = options.outputRootPath;
            bool isOutputMissing = string.IsNullOrEmpty(outputRootPath);
            if (isOutputMissing)
            {
                context.AddError(BuildDefine.LOG_OUTPUT_MISSING);
                return false;
            }

            string absoluteOutputPath = Path.GetFullPath(outputRootPath);
            context.LogInfoF(BuildDefine.LOG_BUILD_STARTED, options.platform, absoluteOutputPath);

            BuildTarget target = ConvertBuildTarget(options.platform);
            BuildTargetGroup group = ConvertBuildTargetGroup(options.platform);
            bool isTargetReady = EnsureBuildTarget(context, group, target);
            if (!isTargetReady)
            {
                return false;
            }

            bool isPrepared = PrepareOutputDirectory(context, absoluteOutputPath);
            if (!isPrepared)
            {
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool isCollected = CollectBuilds(context);
            if (!isCollected)
            {
                return false;
            }

            bool isMerged = MergeBuilds(context);
            if (!isMerged)
            {
                return false;
            }

            bool isBuildListEnabled = options.writeBuildList;
            if (isBuildListEnabled)
            {
                WriteBuildList(absoluteOutputPath, context);
            }

            bool isBuilt = BuildAssetBundles(context, target, absoluteOutputPath);
            if (!isBuilt)
            {
                return false;
            }

            context.ClearBundleNames();
            AssetDatabase.Refresh();

            bool hasErrors = context.hasErrors;
            if (hasErrors)
            {
                int errorCount = context.errorCount;
                string failedMessage = string.Format(BuildDefine.LOG_BUILD_FAILED, errorCount);
                context.LogInfo(failedMessage);
                return false;
            }

            int bundleCount = s_MergedBuildList.Count;
            int databaseCount = context.databaseCount;
            context.LogInfoF(BuildDefine.LOG_BUILD_SUCCEEDED, bundleCount, databaseCount);
            return true;
        }

        /// <summary>调用 BuildPipeline 并生成五个客户端数据库。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="target">目标平台。</param>
        /// <param name="outputRootPath">输出根目录绝对路径。</param>
        static bool BuildAssetBundles(ResourceBuildContext context, BuildTarget target,
            string outputRootPath)
        {
            BuildAssetBundleOptions bundleOptions = ResolveBundleOptions(context, target);
            AssetBundleBuild[] builds = s_MergedBuildList.ToArray();
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                outputRootPath, builds, bundleOptions, target);

            bool isManifestNull = manifest == null;
            if (isManifestNull)
            {
                context.AddError(BuildDefine.LOG_BUILD_PIPELINE_NULL);
                return false;
            }

            AssetBundleDatabaseSet databaseSet = new AssetBundleDatabaseSet();
            databaseSet.BuildFromManifest(manifest, context);
            databaseSet.WriteAll(outputRootPath, context.options.lineEnding, context);

            CleanupManifestFiles(outputRootPath);
            return true;
        }

        /// <summary>根据选项创建构建器并收集构建项。</summary>
        /// <param name="context">本次打包上下文。</param>
        static bool CollectBuilds(ResourceBuildContext context)
        {
            s_BuildList.Clear();
            CreateBuilders(context.options);

            for (int i = 0; i < s_Builders.Count; ++i)
            {
                ResourceBuilderBase builder = s_Builders[i];
                builder.Collect(context, s_BuildList);
            }

            bool isCoverageValid = ValidateCoverage(context);
            if (!isCoverageValid)
            {
                return false;
            }

            bool isBuildListEmpty = s_BuildList.Count == 0;
            if (isBuildListEmpty)
            {
                context.AddError(BuildDefine.LOG_NO_BUILD_ENTRY);
                return false;
            }

            return true;
        }

        /// <summary>按选项组装默认构建器与自定义构建器。</summary>
        /// <param name="options">打包选项。</param>
        static void CreateBuilders(ResourceBuildOptions options)
        {
            s_Builders.Clear();
            AddBuilderIfEnabled(options, EResourceBuildScope.Scene, new SceneBuilder());
            AddBuilderIfEnabled(options, EResourceBuildScope.Story, new StoryBuilder());
            AddBuilderIfEnabled(options, EResourceBuildScope.Ui, new UIBuilder());
            AddBuilderIfEnabled(options, EResourceBuildScope.Character, new CharacterBuilder());
            AddBuilderIfEnabled(options, EResourceBuildScope.Particle, new ParticleBuilder());
            AddBuilderIfEnabled(options, EResourceBuildScope.Audio, new AudioBuilder());

            // shader 共享依赖随任意类别一起构建，避免产物缺依赖。
            ShaderBuilder shaderBuilder = new ShaderBuilder();
            s_Builders.Add(shaderBuilder);

            s_CustomBuilders.Clear();
            options.GetBuilders(s_CustomBuilders);
            for (int i = 0; i < s_CustomBuilders.Count; ++i)
            {
                s_Builders.Add(s_CustomBuilders[i]);
            }
        }

        /// <summary>类别启用时追加对应构建器。</summary>
        /// <param name="options">打包选项。</param>
        /// <param name="scope">资源类别。</param>
        /// <param name="builder">待追加构建器。</param>
        static void AddBuilderIfEnabled(ResourceBuildOptions options, EResourceBuildScope scope,
            ResourceBuilderBase builder)
        {
            bool isEnabled = options.IsScopeEnabled(scope);
            if (!isEnabled)
            {
                return;
            }

            s_Builders.Add(builder);
        }

        /// <summary>校验所有已命名 Bundle 都能路由到固定数据库。</summary>
        /// <param name="context">本次打包上下文。</param>
        static bool ValidateCoverage(ResourceBuildContext context)
        {
            List<string> allBundleNames = context.allBundleNames;
            bool isValid = true;
            for (int i = 0; i < allBundleNames.Count; ++i)
            {
                string bundleName = allBundleNames[i];
                string error;
                bool isKnown = AssetBundleDatabaseRouter.TryGetDatabaseNames(
                    bundleName, s_TempDatabaseNames, out error);
                if (!isKnown)
                {
                    context.AddError(error);
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>合并同名 Bundle 并检查重复资源与名字长度。</summary>
        /// <param name="context">本次打包上下文。</param>
        static bool MergeBuilds(ResourceBuildContext context)
        {
            s_MergedBuildList.Clear();
            s_BundleAssetMap.Clear();
            s_BundleOrder.Clear();
            s_AssetOwnerMap.Clear();

            bool isValid = true;
            for (int i = 0; i < s_BuildList.Count; ++i)
            {
                AssetBundleBuild build = s_BuildList[i];
                string bundleName = build.assetBundleName;
                bool isNameValid = ValidateBundleName(context, bundleName);
                if (!isNameValid)
                {
                    isValid = false;
                    continue;
                }

                bool isMerged = MergeBuildAssets(context, build);
                if (!isMerged)
                {
                    isValid = false;
                }
            }

            if (!isValid)
            {
                return false;
            }

            for (int i = 0; i < s_BundleOrder.Count; ++i)
            {
                string bundleName = s_BundleOrder[i];
                List<string> bundleAssets = s_BundleAssetMap[bundleName];
                AssetBundleBuild mergedBuild = new AssetBundleBuild();
                mergedBuild.assetBundleName = bundleName;
                mergedBuild.assetNames = bundleAssets.ToArray();
                s_MergedBuildList.Add(mergedBuild);
            }

            return true;
        }

        /// <summary>把一个构建项的资源并入同名 Bundle。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="build">待合并构建项。</param>
        static bool MergeBuildAssets(ResourceBuildContext context, AssetBundleBuild build)
        {
            string bundleName = build.assetBundleName;
            string[] assetNames = build.assetNames;
            bool isAssetMissing = assetNames == null;
            if (isAssetMissing)
            {
                return true;
            }

            bool isValid = true;
            for (int i = 0; i < assetNames.Length; ++i)
            {
                string assetPath = assetNames[i];
                string ownerBundleName;
                bool isOwned = s_AssetOwnerMap.TryGetValue(assetPath, out ownerBundleName);
                if (isOwned)
                {
                    string duplicateText = ownerBundleName + " / " + bundleName;
                    string message = string.Format(
                        BuildDefine.LOG_DUPLICATE_ASSET, assetPath, duplicateText);
                    context.AddError(message);
                    isValid = false;
                    continue;
                }

                s_AssetOwnerMap.Add(assetPath, bundleName);
                List<string> bundleAssets;
                bool isBundleKnown = s_BundleAssetMap.TryGetValue(bundleName, out bundleAssets);
                if (!isBundleKnown)
                {
                    bundleAssets = new List<string>(CommonDefine.s_ListConst_16);
                    s_BundleAssetMap.Add(bundleName, bundleAssets);
                    s_BundleOrder.Add(bundleName);
                }

                bundleAssets.Add(assetPath);
            }

            return isValid;
        }

        /// <summary>检查 Bundle 名长度与后缀。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="bundleName">AssetBundle 逻辑路径。</param>
        static bool ValidateBundleName(ResourceBuildContext context, string bundleName)
        {
            bool isNameEmpty = string.IsNullOrEmpty(bundleName);
            if (isNameEmpty)
            {
                context.AddError(BuildDefine.LOG_NAME_EMPTY);
                return false;
            }

            int nameLength = bundleName.Length;
            bool isTooLong = nameLength > BuildDefine.MAX_BUNDLE_NAME_LENGTH;
            if (isTooLong)
            {
                int maxLength = BuildDefine.MAX_BUNDLE_NAME_LENGTH;
                string message = string.Format(
                    BuildDefine.LOG_NAME_TOO_LONG, maxLength, bundleName);
                context.AddError(message);
                return false;
            }

            bool hasExtension = bundleName.EndsWith(
                BuildDefine.BUNDLE_EXTENSION, StringComparison.Ordinal);
            bool isExtensionMissing = !hasExtension;
            if (isExtensionMissing)
            {
                string extension = BuildDefine.BUNDLE_EXTENSION;
                string message = string.Format(
                    BuildDefine.LOG_EXTENSION_MISSING, extension, bundleName);
                context.AddWarning(message);
            }

            return true;
        }

        /// <summary>按平台与类别决定 AssetBundle 构建选项。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="target">目标平台。</param>
        static BuildAssetBundleOptions ResolveBundleOptions(ResourceBuildContext context,
            BuildTarget target)
        {
            BuildAssetBundleOptions bundleOptions = BuildAssetBundleOptions.ChunkBasedCompression |
                                                    BuildAssetBundleOptions.IgnoreTypeTreeChanges;

            bool isMobileTarget = (target == BuildTarget.Android) || (target == BuildTarget.iOS);
            bool isUiEnabled = context.options.IsScopeEnabled(EResourceBuildScope.Ui);
            bool isSceneEnabled = context.options.IsScopeEnabled(EResourceBuildScope.Scene);
            bool isUiOrSceneBuild = isUiEnabled || isSceneEnabled;
            bool isTypeTreeDisabled = isMobileTarget && isUiOrSceneBuild;
            if (isTypeTreeDisabled)
            {
                bundleOptions = BuildAssetBundleOptions.ChunkBasedCompression |
                                BuildAssetBundleOptions.DisableWriteTypeTree;
            }

            BuildAssetBundleOptions result = bundleOptions;
            return result;
        }

        /// <summary>创建输出目录，按需清空旧内容。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="outputRootPath">输出根目录绝对路径。</param>
        static bool PrepareOutputDirectory(ResourceBuildContext context, string outputRootPath)
        {
            try
            {
                bool isDirectoryExists = Directory.Exists(outputRootPath);
                bool isCleanRequired = context.options.isCleanOutput;
                bool shouldDelete = isDirectoryExists && isCleanRequired;
                if (shouldDelete)
                {
                    Directory.Delete(outputRootPath, true);
                }

                Directory.CreateDirectory(outputRootPath);
                return true;
            }
            catch (Exception ex)
            {
                string message = string.Format(
                    BuildDefine.LOG_PREPARE_FAILED, outputRootPath, ex.Message);
                context.AddError(message);
                return false;
            }
        }

        /// <summary>按需把 Unity 当前构建目标切换到目标平台。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="group">目标平台分组。</param>
        /// <param name="target">目标平台。</param>
        static bool EnsureBuildTarget(ResourceBuildContext context, BuildTargetGroup group,
            BuildTarget target)
        {
            bool isSwitchDisabled = !context.options.isSwitchBuildTarget;
            if (isSwitchDisabled)
            {
                return true;
            }

            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            bool isAlreadyActive = activeTarget == target;
            if (isAlreadyActive)
            {
                return true;
            }

            bool isSwitched = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            bool isSwitchFailed = !isSwitched;
            if (isSwitchFailed)
            {
                string message = string.Format(
                    BuildDefine.LOG_SWITCH_TARGET_FAILED, target);
                context.AddError(message);
                return false;
            }

            return true;
        }

        /// <summary>把平台枚举转换成 Unity 构建目标。</summary>
        /// <param name="platform">资源打包平台。</param>
        static BuildTarget ConvertBuildTarget(EResourceBuildPlatform platform)
        {
            BuildTarget target = BuildTarget.StandaloneWindows64;
            switch (platform)
            {
                case EResourceBuildPlatform.Android:
                    target = BuildTarget.Android;
                    break;
                case EResourceBuildPlatform.IOS:
                    target = BuildTarget.iOS;
                    break;
                case EResourceBuildPlatform.Windows:
                    target = BuildTarget.StandaloneWindows64;
                    break;
            }

            return target;
        }

        /// <summary>把平台枚举转换成 Unity 构建目标分组。</summary>
        /// <param name="platform">资源打包平台。</param>
        static BuildTargetGroup ConvertBuildTargetGroup(EResourceBuildPlatform platform)
        {
            BuildTargetGroup group = BuildTargetGroup.Standalone;
            switch (platform)
            {
                case EResourceBuildPlatform.Android:
                    group = BuildTargetGroup.Android;
                    break;
                case EResourceBuildPlatform.IOS:
                    group = BuildTargetGroup.iOS;
                    break;
                case EResourceBuildPlatform.Windows:
                    group = BuildTargetGroup.Standalone;
                    break;
            }

            return group;
        }

        /// <summary>输出可选的 builds.txt 构建清单。</summary>
        /// <param name="outputRootPath">输出根目录绝对路径。</param>
        /// <param name="context">本次打包上下文。</param>
        static void WriteBuildList(string outputRootPath, ResourceBuildContext context)
        {
            StringBuilder builder = new StringBuilder(s_MergedBuildList.Count * 64);
            string lineEnding = BuildDefine.LINE_ENDING_LF;
            for (int i = 0; i < s_MergedBuildList.Count; ++i)
            {
                AssetBundleBuild build = s_MergedBuildList[i];
                builder.Append(build.assetBundleName);
                builder.Append(lineEnding);

                string[] assetNames = build.assetNames;
                for (int j = 0; j < assetNames.Length; ++j)
                {
                    builder.Append('\t');
                    builder.Append(assetNames[j]);
                    builder.Append(lineEnding);
                }
            }

            string fileName = BuildDefine.BUILD_LIST_FILE_NAME;
            string filePath = Path.Combine(outputRootPath, fileName);
            string content = builder.ToString();
            File.WriteAllText(filePath, content, s_Utf8NoBom);

            string message = string.Format(BuildDefine.LOG_BUILD_LIST_WRITTEN, filePath);
            context.LogInfo(message);
        }

        /// <summary>删除 Unity 生成的清单文件。</summary>
        /// <param name="outputRootPath">输出根目录绝对路径。</param>
        static void CleanupManifestFiles(string outputRootPath)
        {
            DirectoryInfo outputDirectory = new DirectoryInfo(outputRootPath);
            string rootBundleName = outputDirectory.Name;
            string rootBundlePath = Path.Combine(outputRootPath, rootBundleName);
            bool isRootBundleExists = File.Exists(rootBundlePath);
            if (isRootBundleExists)
            {
                File.Delete(rootBundlePath);
            }

            string rootManifestPath = rootBundlePath + BuildDefine.MANIFEST_EXTENSION;
            bool isRootManifestExists = File.Exists(rootManifestPath);
            if (isRootManifestExists)
            {
                File.Delete(rootManifestPath);
            }

            string searchPattern = "*" + BuildDefine.MANIFEST_EXTENSION;
            string[] manifestFiles = Directory.GetFiles(
                outputRootPath, searchPattern, SearchOption.AllDirectories);
            for (int i = 0; i < manifestFiles.Length; ++i)
            {
                File.Delete(manifestFiles[i]);
            }
        }

        /// <summary>把命令行参数应用到打包选项。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="args">进程启动参数。</param>
        static void ApplyCommandLineArguments(ResourceBuildOptions options, string[] args)
        {
            bool isArgsNull = args == null;
            if (isArgsNull)
            {
                return;
            }

            bool hasLegacyScope = HasAnyLegacyScope(args);
            bool hasScopesArg = HasArg(args, BuildDefine.ARG_SCOPES);
            bool shouldClearScopes = hasLegacyScope || hasScopesArg;
            if (shouldClearScopes)
            {
                options.ClearScopes();
            }

            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_SCENE,
                EResourceBuildScope.Scene);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_STORY,
                EResourceBuildScope.Story);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_UI,
                EResourceBuildScope.Ui);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_CHARACTER,
                EResourceBuildScope.Character);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_PARTICLE,
                EResourceBuildScope.Particle);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_AUDIO,
                EResourceBuildScope.Audio);
            ApplyLegacyScope(options, args, BuildDefine.ARG_BUILD_SHADER,
                EResourceBuildScope.Shader);

            string scopesValue;
            bool hasScopes = TryGetValue(args, BuildDefine.ARG_SCOPES, out scopesValue);
            if (hasScopes)
            {
                ApplyScopes(options, scopesValue);
            }

            string platformValue;
            bool hasPlatform = TryGetValue(args, BuildDefine.ARG_PLATFORM, out platformValue);
            if (!hasPlatform)
            {
                hasPlatform = TryGetValue(args, BuildDefine.ARG_TARGET_NAME, out platformValue);
            }

            if (hasPlatform)
            {
                ApplyPlatform(options, platformValue);
            }

            string outputValue;
            bool hasOutput = TryGetValue(args, BuildDefine.ARG_OUTPUT, out outputValue);
            if (!hasOutput)
            {
                hasOutput = TryGetValue(args, BuildDefine.ARG_OUTPUT_PATH, out outputValue);
            }

            if (hasOutput)
            {
                options.outputRootPath = Path.GetFullPath(outputValue);
            }

            bool isClean = HasArg(args, BuildDefine.ARG_CLEAN);
            options.isCleanOutput = isClean;
        }

        /// <summary>旧工程开关存在时启用对应类别。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="args">进程启动参数。</param>
        /// <param name="argName">开关名。</param>
        /// <param name="scope">对应类别。</param>
        static void ApplyLegacyScope(ResourceBuildOptions options, string[] args,
            string argName, EResourceBuildScope scope)
        {
            bool isPresent = HasArg(args, argName);
            if (!isPresent)
            {
                return;
            }

            options.AddScope(scope);
        }

        /// <summary>判断是否出现任一旧工程类别开关。</summary>
        /// <param name="args">进程启动参数。</param>
        static bool HasAnyLegacyScope(string[] args)
        {
            bool hasScene = HasArg(args, BuildDefine.ARG_BUILD_SCENE);
            bool hasStory = HasArg(args, BuildDefine.ARG_BUILD_STORY);
            bool hasUi = HasArg(args, BuildDefine.ARG_BUILD_UI);
            bool hasCharacter = HasArg(args, BuildDefine.ARG_BUILD_CHARACTER);
            bool hasParticle = HasArg(args, BuildDefine.ARG_BUILD_PARTICLE);
            bool hasAudio = HasArg(args, BuildDefine.ARG_BUILD_AUDIO);
            bool hasShader = HasArg(args, BuildDefine.ARG_BUILD_SHADER);
            bool hasAny = hasScene || hasStory || hasUi || hasCharacter
                          || hasParticle || hasAudio || hasShader;
            return hasAny;
        }

        /// <summary>解析逗号分隔的类别列表。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="scopesValue">类别文本。</param>
        static void ApplyScopes(ResourceBuildOptions options, string scopesValue)
        {
            bool isValueMissing = string.IsNullOrEmpty(scopesValue);
            if (isValueMissing)
            {
                return;
            }

            string[] parts = scopesValue.Split(',');
            for (int i = 0; i < parts.Length; ++i)
            {
                string scopeText = parts[i].Trim();
                bool isTextEmpty = string.IsNullOrEmpty(scopeText);
                if (isTextEmpty)
                {
                    continue;
                }

                EResourceBuildScope scope;
                bool isParsed = Enum.TryParse<EResourceBuildScope>(scopeText, true, out scope);
                if (isParsed)
                {
                    options.AddScope(scope);
                }
                else
                {
                    STLogger.LogWarningF(BuildDefine.LOG_SCOPE_UNKNOWN, scopeText);
                }
            }
        }

        /// <summary>解析平台文本。</summary>
        /// <param name="options">待填充选项。</param>
        /// <param name="platformValue">平台文本。</param>
        static void ApplyPlatform(ResourceBuildOptions options, string platformValue)
        {
            bool isValueMissing = string.IsNullOrEmpty(platformValue);
            if (isValueMissing)
            {
                return;
            }

            string normalizedValue = platformValue.Trim().ToLowerInvariant();
            switch (normalizedValue)
            {
                case "android":
                    options.platform = EResourceBuildPlatform.Android;
                    break;
                case "ios":
                case "iphone":
                    options.platform = EResourceBuildPlatform.IOS;
                    break;
                case "standalone":
                case "windows":
                    options.platform = EResourceBuildPlatform.Windows;
                    break;
                default:
                    STLogger.LogWarningF(BuildDefine.LOG_PLATFORM_UNKNOWN, platformValue);
                    break;
            }
        }

        /// <summary>读取 key=value 或 -key value 形式的参数值。</summary>
        /// <param name="args">进程启动参数。</param>
        /// <param name="key">参数名。</param>
        /// <param name="value">参数值。</param>
        static bool TryGetValue(string[] args, string key, out string value)
        {
            value = string.Empty;
            string normalizedKey = NormalizeKey(key);
            string equalsPrefix = normalizedKey + "=";
            for (int i = 0; i < args.Length; ++i)
            {
                string normalizedArg = NormalizeKey(args[i]);
                bool isEqualsForm = normalizedArg.StartsWith(
                    equalsPrefix, StringComparison.OrdinalIgnoreCase);
                if (isEqualsForm)
                {
                    value = normalizedArg.Substring(equalsPrefix.Length);
                    return true;
                }

                bool isKeyMatch = string.Equals(
                    normalizedArg, normalizedKey, StringComparison.OrdinalIgnoreCase);
                if (isKeyMatch)
                {
                    bool hasNext = (i + 1) < args.Length;
                    if (hasNext)
                    {
                        value = args[i + 1];
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>判断参数是否存在。</summary>
        /// <param name="args">进程启动参数。</param>
        /// <param name="key">参数名。</param>
        static bool HasArg(string[] args, string key)
        {
            string normalizedKey = NormalizeKey(key);
            for (int i = 0; i < args.Length; ++i)
            {
                string normalizedArg = NormalizeKey(args[i]);
                bool isMatch = string.Equals(
                    normalizedArg, normalizedKey, StringComparison.OrdinalIgnoreCase);
                if (isMatch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>去掉参数名前导横线，兼容 -key 与 key 两种写法。</summary>
        /// <param name="text">原参数文本。</param>
        static string NormalizeKey(string text)
        {
            bool isTextMissing = string.IsNullOrEmpty(text);
            if (isTextMissing)
            {
                return string.Empty;
            }

            bool hasDash = text.StartsWith("-", StringComparison.Ordinal);
            if (!hasDash)
            {
                return text;
            }

            string normalizedText = text.Substring(1);
            return normalizedText;
        }
    }
}
