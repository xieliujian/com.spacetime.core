using UnityEditor;

namespace ST.Core
{
    /// <summary>Builder 的平台与构建选项职责。</summary>
    public static partial class Builder
    {
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
    }
}
