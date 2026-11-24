using System;
using System.Collections.Generic;
using UnityEditor;

namespace ST.Core
{
    /// <summary>Builder 的合并与校验职责。</summary>
    public static partial class Builder
    {
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
    }
}
