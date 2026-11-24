using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace ST.Core
{
    /// <summary>Builder 的静态状态定义。</summary>
    public static partial class Builder
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
    }
}
