using System;
using System.Collections.Generic;
using UnityEditor;

namespace ST.Core
{
    /// <summary>
    /// Shader 构建器，收集 depend/shader_ 下的共享 AssetBundle，并提示未被收集的 depend 包。
    /// </summary>
    public class ShaderBuilder : ResourceBuilderBase
    {
        /// <summary>接受的前缀。</summary>
        static readonly string[] s_Prefixes =
        {
            BuildDefine.PREFIX_SHADER_DEPEND,
        };

        /// <summary>构造 Shader 构建器。</summary>
        public ShaderBuilder()
            : base("ShaderBuilder", EResourceBuildScope.Shader, s_Prefixes)
        {
        }

        /// <summary>判断 Bundle 是否属于 shader 共享依赖。</summary>
        /// <param name="bundleName">AssetBundle 逻辑路径。</param>
        public override bool IsBundleAccepted(string bundleName)
        {
            bool isPrefixMatch = base.IsBundleAccepted(bundleName);
            if (!isPrefixMatch)
            {
                return false;
            }

            bool hasBundleExtension = bundleName.EndsWith(
                BuildDefine.BUNDLE_EXTENSION, StringComparison.Ordinal);
            return hasBundleExtension;
        }

        /// <summary>收集 shader 共享依赖，并提示未被收集的 depend 包。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="result">接收构建项的容器。</param>
        public override void Collect(ResourceBuildContext context, List<AssetBundleBuild> result)
        {
            WarnNonShaderDependBundles(context);
            base.Collect(context, result);
        }

        /// <summary>对 depend 目录下非 shader 命名的包输出告警。</summary>
        /// <param name="context">本次打包上下文。</param>
        void WarnNonShaderDependBundles(ResourceBuildContext context)
        {
            List<string> allBundleNames = context.allBundleNames;
            for (int i = 0; i < allBundleNames.Count; ++i)
            {
                string bundleName = allBundleNames[i];
                bool isDepend = bundleName.StartsWith(
                    BuildDefine.PREFIX_DEPEND, StringComparison.Ordinal);
                if (!isDepend)
                {
                    continue;
                }

                bool isShaderDepend = bundleName.StartsWith(
                    BuildDefine.PREFIX_SHADER_DEPEND, StringComparison.Ordinal);
                if (isShaderDepend)
                {
                    continue;
                }

                string message = string.Format(
                    BuildDefine.LOG_DEPEND_NOT_SHARED, bundleName);
                context.AddWarning(message);
            }
        }
    }
}
