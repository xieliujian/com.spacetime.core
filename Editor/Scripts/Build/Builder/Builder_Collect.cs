using System.Collections.Generic;
using UnityEditor;

namespace ST.Core
{
    /// <summary>Builder 的构建项收集职责。</summary>
    public static partial class Builder
    {
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
    }
}
