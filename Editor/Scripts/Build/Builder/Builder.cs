using System;
using System.Collections.Generic;
using System.IO;
using ST.Core.Logging;
using UnityEditor;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 资源打包总入口：解析命令行参数、按类别收集 AssetBundle、构建并生成五个客户端数据库。
    /// </summary>
    public static partial class Builder
    {
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
    }
}
