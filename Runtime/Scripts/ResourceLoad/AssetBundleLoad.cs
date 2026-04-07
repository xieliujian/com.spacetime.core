using System.Collections.Generic;
using System;
using UnityEngine;

namespace ST.Core
{
    public class AssetBundleLoad
    {
        Dictionary<string, Bundle> m_BundleDict = new Dictionary<string, Bundle>(CommonDefine.s_ListConst_1024);
        AssetBundleManifest m_Manifest = null;
        FilePathHelper m_FilePathHelper;
        IResourceConfig m_Config;

        public AssetBundleLoad(FilePathHelper filePathHelper, IResourceConfig config)
        {
            m_FilePathHelper = filePathHelper;
            m_Config = config;
        }

        public void DoInit()
        {
            InitAllBundle();
        }

        public Bundle GetBundle(string name)
        {
            Bundle bundle = null;
            m_BundleDict.TryGetValue(name, out bundle);
            return bundle;
        }

        public object[] LoadAllSync(string respath)
        {
            var fullpath = respath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return null;
            return bundle.LoadAllSync();
        }

        public object LoadSync(string respath, string filename, Type type)
        {
            var fullpath = respath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return null;
            return bundle.LoadSync(filename, type);
        }

        public void LoadAsync(string realpath, string filename, Type type, ResourceLoadComplete callback)
        {
            var fullpath = realpath + m_Config.bundleSuffix;
            var bundle = GetBundle(fullpath);
            if (bundle == null) return;
            bundle.LoadAsync(filename, type, callback);
        }

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
