using System.Collections.Generic;
using System;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 运行时 AssetBundle 加载中枢：从 StreamingAssets 侧清单构建 <see cref="Bundle"/> 字典，并提供按路径的同步/异步加载入口。
    /// </summary>
    public class AssetBundleLoad
    {
        /// <summary>按 Bundle 名称索引的全局字典（key 与清单中名称一致）。</summary>
        Dictionary<string, Bundle> m_BundleDict = new Dictionary<string, Bundle>(CommonDefine.s_ListConst_1024);
        /// <summary>从主清单包中读取的 <see cref="AssetBundleManifest"/>，用于获取依赖关系。</summary>
        AssetBundleManifest m_Manifest = null;
        /// <summary>路径拼接工具，解析 StreamingAssets 根目录与 Bundle 完整路径。</summary>
        FilePathHelper m_FilePathHelper;
        /// <summary>资源配置，提供 <see cref="IResourceConfig.bundleSuffix"/> 等参数。</summary>
        IResourceConfig m_Config;

        /// <summary>创建加载中枢，注入路径与配置依赖。</summary>
        /// <param name="filePathHelper">清单与 Bundle 文件路径解析。</param>
        /// <param name="config">后缀名、应用名等配置。</param>
        public AssetBundleLoad(FilePathHelper filePathHelper, IResourceConfig config)
        {
            m_FilePathHelper = filePathHelper;
            m_Config = config;
        }

        /// <summary>扫描并加载主清单，填充所有 <see cref="Bundle"/>。</summary>
        public void DoInit()
        {
            InitAllBundle();
        }

        /// <summary>按清单中的 Bundle 名称（含变体后缀路径）查找封装对象。</summary>
        public Bundle GetBundle(string name)
        {
            Bundle bundle = null;
            m_BundleDict.TryGetValue(name, out bundle);
            return bundle;
        }

        /// <summary>同步加载包内全部资产。</summary>
        /// <param name="respath">逻辑资源路径（不含 <see cref="IResourceConfig.bundleSuffix"/>，内部会拼接）。</param>
        public object[] LoadAllSync(string respath)
        {
            var fullpath = respath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return null;
            return bundle.LoadAllSync();
        }

        /// <summary>同步按名称与类型加载包内单个资产。</summary>
        /// <param name="respath">逻辑路径（不含后缀）。</param>
        /// <param name="filename">包内资源名。</param>
        /// <param name="type">目标类型。</param>
        public object LoadSync(string respath, string filename, Type type)
        {
            var fullpath = respath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return null;
            return bundle.LoadSync(filename, type);
        }

        /// <summary>异步加载指定包内资源。</summary>
        public void LoadAsync(string realpath, string filename, Type type, ResourceLoadComplete callback)
        {
            var fullpath = realpath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return;
            bundle.LoadAsync(filename, type, callback);
        }

        /// <summary>异步加载场景（<paramref name="suffix"/> 参与路径拼接规则由上层 <see cref="ResourceLoad"/> 决定）。</summary>
        public void LoadSceneAsync(string realpath, string filename, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            var fullpath = realpath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return;
            bundle.LoadSceneAsync(filename, progress, complete);
        }

        /// <summary>加载主清单包，遍历所有 Bundle 名称并构建 <see cref="m_BundleDict"/>，完成后卸载清单包。</summary>
        void InitAllBundle()
        {
            string manifestPath = m_FilePathHelper.GetFilePath() + m_Config.appName + "/" + m_Config.appName;
            var manifestBundle = AssetBundle.LoadFromFile(manifestPath);

            if (manifestBundle != null)
            {
                m_Manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                var allbundlearray = m_Manifest.GetAllAssetBundles();
                if (allbundlearray != null)
                {
                    foreach (var bundlename in allbundlearray)
                    {
                        if (bundlename == null) continue;

                        var bundle = new Bundle(bundlename, m_FilePathHelper);
                        bundle.load = this;
                        bundle.InitDependencies(m_Manifest);

                        if (!m_BundleDict.ContainsKey(bundlename))
                            m_BundleDict.Add(bundlename, bundle);
                    }
                }

                manifestBundle.Unload(true);
            }
        }
    }
}
