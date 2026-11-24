using System;
using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>
    /// 按客户端逻辑路径把 AssetBundle 路由到固定的五个数据库。
    /// </summary>
    public static class AssetBundleDatabaseRouter
    {
        /// <summary>
        /// 计算一个 Bundle 路径归属的数据库集合。
        /// </summary>
        /// <param name="logicalPath">Bundle 逻辑路径，例如 ui/a.assetbundle。</param>
        /// <param name="result">接收数据库名的容器，调用前会被清空。</param>
        /// <param name="error">无法归类时的错误描述。</param>
        /// <returns>路径有明确归属时返回 true。</returns>
        public static bool TryGetDatabaseNames(string logicalPath, List<string> result, out string error)
        {
            error = string.Empty;
            bool isResultNull = result == null;
            if (isResultNull)
            {
                error = string.Format(BuildDefine.LOG_UNKNOWN_PREFIX, "result 为空");
                return false;
            }

            result.Clear();

            bool isPathMissing = string.IsNullOrEmpty(logicalPath);
            if (isPathMissing)
            {
                error = string.Format(BuildDefine.LOG_UNKNOWN_PREFIX, string.Empty);
                return false;
            }

            bool isShaderDepend = logicalPath.StartsWith(
                BuildDefine.PREFIX_SHADER_DEPEND, StringComparison.Ordinal);
            bool isBundleExtension = logicalPath.EndsWith(
                BuildDefine.BUNDLE_EXTENSION, StringComparison.Ordinal);
            bool isSharedDepend = isShaderDepend && isBundleExtension;
            if (isSharedDepend)
            {
                AddAllDatabases(result);
                return true;
            }

            bool isDepend = logicalPath.StartsWith(
                BuildDefine.PREFIX_DEPEND, StringComparison.Ordinal);
            if (isDepend)
            {
                error = string.Format(BuildDefine.LOG_UNKNOWN_DEPEND, logicalPath);
                return false;
            }

            string databaseName = GetSingleDatabaseName(logicalPath);
            bool isDatabaseMissing = string.IsNullOrEmpty(databaseName);
            if (isDatabaseMissing)
            {
                error = string.Format(BuildDefine.LOG_UNKNOWN_PREFIX, logicalPath);
                return false;
            }

            result.Add(databaseName);
            return true;
        }

        /// <summary>把五库名全部加入结果。</summary>
        /// <param name="result">接收数据库名的容器。</param>
        static void AddAllDatabases(List<string> result)
        {
            result.Add(BuildDefine.DB_SCENE);
            result.Add(BuildDefine.DB_UI);
            result.Add(BuildDefine.DB_OTHER);
            result.Add(BuildDefine.DB_CHARACTER);
            result.Add(BuildDefine.DB_PARTICLE);
        }

        /// <summary>按固定前缀返回唯一数据库名。</summary>
        /// <param name="logicalPath">Bundle 逻辑路径。</param>
        /// <returns>命中的数据库名，未命中时返回空串。</returns>
        static string GetSingleDatabaseName(string logicalPath)
        {
            bool isScenePath = StartsWith(logicalPath, BuildDefine.PREFIX_SCENE);
            bool isStoryPath = StartsWith(logicalPath, BuildDefine.PREFIX_STORY);
            bool isGlobalPath = StartsWith(logicalPath, BuildDefine.PREFIX_GLOBAL);
            bool isSceneDatabase = isScenePath || isStoryPath || isGlobalPath;
            if (isSceneDatabase)
            {
                return BuildDefine.DB_SCENE;
            }

            bool isUiPath = StartsWith(logicalPath, BuildDefine.PREFIX_UI);
            if (isUiPath)
            {
                return BuildDefine.DB_UI;
            }

            bool isTexturePath = StartsWith(logicalPath, BuildDefine.PREFIX_TEXTURE);
            bool isSkillPath = StartsWith(logicalPath, BuildDefine.PREFIX_SKILL);
            bool isAudioPath = StartsWith(logicalPath, BuildDefine.PREFIX_AUDIO);
            bool isOtherDatabase = isTexturePath || isSkillPath || isAudioPath;
            if (isOtherDatabase)
            {
                return BuildDefine.DB_OTHER;
            }

            bool isCharacterPath = StartsWith(logicalPath, BuildDefine.PREFIX_CHARACTER);
            if (isCharacterPath)
            {
                return BuildDefine.DB_CHARACTER;
            }

            bool isParticlePath = StartsWith(logicalPath, BuildDefine.PREFIX_PARTICLE);
            if (isParticlePath)
            {
                return BuildDefine.DB_PARTICLE;
            }

            return string.Empty;
        }

        /// <summary>判断路径是否以指定前缀开头。</summary>
        /// <param name="logicalPath">Bundle 逻辑路径。</param>
        /// <param name="prefix">待匹配前缀。</param>
        static bool StartsWith(string logicalPath, string prefix)
        {
            bool isMatch = logicalPath.StartsWith(prefix, StringComparison.Ordinal);
            return isMatch;
        }
    }
}
