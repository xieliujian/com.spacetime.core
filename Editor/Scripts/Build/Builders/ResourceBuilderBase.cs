using System;
using System.Collections.Generic;
using UnityEditor;

namespace ST.Core
{
    /// <summary>
    /// 类别构建器基类，按路径前缀收集 AssetImporter 已命名的 AssetBundle。
    /// </summary>
    public abstract class ResourceBuilderBase
    {
        /// <summary>构建器名。</summary>
        string m_Name;

        /// <summary>构建器负责的资源类别。</summary>
        EResourceBuildScope m_Scope;

        /// <summary>接受的路径前缀。</summary>
        string[] m_Prefixes;

        /// <summary>构造带前缀过滤的类别构建器。</summary>
        /// <param name="name">非空构建器名。</param>
        /// <param name="scope">资源类别。</param>
        /// <param name="prefixes">至少一个路径前缀。</param>
        protected ResourceBuilderBase(string name, EResourceBuildScope scope, string[] prefixes)
        {
            bool isNameMissing = string.IsNullOrEmpty(name);
            if (isNameMissing)
            {
                throw new ArgumentException("构建器名不能为空。", "name");
            }

            bool isPrefixesMissing = prefixes == null || prefixes.Length == 0;
            if (isPrefixesMissing)
            {
                throw new ArgumentException("构建器必须至少包含一个路径前缀。", "prefixes");
            }

            m_Name = name;
            m_Scope = scope;
            m_Prefixes = prefixes;
        }

        /// <summary>构建器名。</summary>
        public string name
        {
            get { return m_Name; }
        }

        /// <summary>构建器负责的资源类别。</summary>
        public EResourceBuildScope scope
        {
            get { return m_Scope; }
        }

        /// <summary>判断 Bundle 是否命中任一前缀。</summary>
        /// <param name="bundleName">AssetBundle 逻辑路径。</param>
        public virtual bool IsBundleAccepted(string bundleName)
        {
            bool isNameMissing = string.IsNullOrEmpty(bundleName);
            if (isNameMissing)
            {
                return false;
            }

            for (int i = 0; i < m_Prefixes.Length; ++i)
            {
                bool isMatch = bundleName.StartsWith(m_Prefixes[i], StringComparison.Ordinal);
                if (isMatch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>收集本构建器负责的 AssetBundle 构建项。</summary>
        /// <param name="context">本次打包上下文。</param>
        /// <param name="result">接收构建项的容器。</param>
        public virtual void Collect(ResourceBuildContext context, List<AssetBundleBuild> result)
        {
            bool isResultNull = result == null;
            if (isResultNull)
            {
                return;
            }

            List<string> allBundleNames = context.allBundleNames;
            int acceptedCount = 0;
            for (int i = 0; i < allBundleNames.Count; ++i)
            {
                string bundleName = allBundleNames[i];
                bool isAccepted = IsBundleAccepted(bundleName);
                if (!isAccepted)
                {
                    continue;
                }

                AssetBundleBuild build = CreateBuild(bundleName, context);
                bool isBuildValid = build.assetNames != null && build.assetNames.Length > 0;
                if (!isBuildValid)
                {
                    continue;
                }

                result.Add(build);
                ++acceptedCount;
            }

            context.LogInfoF(BuildDefine.LOG_TASK_COLLECTED, m_Name, acceptedCount);
        }

        /// <summary>读取单个 Bundle 的资源路径并生成构建项。</summary>
        /// <param name="bundleName">AssetBundle 逻辑路径。</param>
        /// <param name="context">本次打包上下文。</param>
        /// <returns>构建项，资源为空时 assetNames 为空数组。</returns>
        protected virtual AssetBundleBuild CreateBuild(string bundleName, ResourceBuildContext context)
        {
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            bool isAssetEmpty = assetPaths == null || assetPaths.Length == 0;
            if (isAssetEmpty)
            {
                string message = string.Format(BuildDefine.LOG_EMPTY_BUNDLE, bundleName);
                context.AddWarning(message);
                assetPaths = new string[0];
            }

            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = bundleName;
            build.assetNames = assetPaths;
            return build;
        }
    }
}
