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
        Dictionary<string, Bundle> m_BundleDict = new Dictionary<string, Bundle>(CommonDefine.s_ListConst_1024);
        AssetBundleManifest m_Manifest = null;
        FilePathHelper m_FilePathHelper;
        IResourceConfig m_Config;

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

        /// <param name="respath">逻辑资源路径（不含 <see cref="IResourceConfig.bundleSuffix"/>，内部会拼接）。</param>
        public object[] LoadAllSync(string respath)
        {
            var fullpath = respath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return null;
            return bundle.LoadAllSync();
        }

        /// <param name="filename">包内资源名。</param>
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
