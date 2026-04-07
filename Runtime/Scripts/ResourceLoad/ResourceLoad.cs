using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    public class ResourceLoad : BaseResourceLoad
    {
        public static bool useAssetBundle = false;

        EditorResourceLoad m_EditorResLoad;
        AssetBundleLoad m_AssetBundleLoad;

        public override void DoClose() { }

        public override void DoInit()
        {
            m_EditorResLoad = new EditorResourceLoad(m_Config);
            m_AssetBundleLoad = new AssetBundleLoad(m_FilePathHelper, m_Config);
            m_AssetBundleLoad.DoInit();

            InstallDecorator(new LuaAssetDecorator());
        }

        public override void DoUpdate() { }

        public override object[] LoadAllResourceSync(string path, string filename, string suffix)
        {
            string realpath = path + filename;

#if UNITY_EDITOR
            if (!useAssetBundle)
                return m_EditorResLoad.LoadAllSync(realpath, suffix);
            else
                return m_AssetBundleLoad.LoadAllSync(realpath);
#else
            return m_AssetBundleLoad.LoadAllSync(realpath);
#endif
        }

        public override object LoadResourceSync(string path, string filename, string suffix, ResourceType restype = ResourceType.Default)
        {
            string realpath = path + filename;
            var type = Type2Native(restype);
            var originType = type;
            BeforeLoad(ref realpath, ref type);

            object obj = null;

#if UNITY_EDITOR
            if (!useAssetBundle)
                obj = m_EditorResLoad.LoadSync(realpath, suffix, type);
            else
                obj = m_AssetBundleLoad.LoadSync(realpath, filename, type);
#else
            obj = m_AssetBundleLoad.LoadSync(realpath, filename, type);
#endif

            return AfterLoad(realpath, originType, obj);
        }

        public override void LoadResourceAsync(string path, string filename, string suffix, ResourceLoadComplete callback, ResourceType restype = ResourceType.Default)
        {
            string realpath = path + filename;
            var type = Type2Native(restype);
            var originType = type;
            BeforeLoad(ref realpath, ref type);

#if UNITY_EDITOR
            if (!useAssetBundle)
            {
                m_EditorResLoad.LoadAsync(realpath, suffix, type, (obj) => {
                    ResourceAsyncCallback(realpath, originType, obj, callback);
                });
            }
            else
            {
                m_AssetBundleLoad.LoadAsync(realpath, filename, type, (obj) => {
                    ResourceAsyncCallback(realpath, originType, obj, callback);
                });
            }
#else
            m_AssetBundleLoad.LoadAsync(realpath, filename, type, (obj) => {
                ResourceAsyncCallback(realpath, originType, obj, callback);
            });
#endif
        }

        public override void LoadSceneAsync(string path, string filename, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            string realpath = path + filename;

#if UNITY_EDITOR
            if (!useAssetBundle)
                m_EditorResLoad.LoadSceneAsync(realpath, suffix, progress, complete);
            else
                m_AssetBundleLoad.LoadSceneAsync(realpath, filename, suffix, progress, complete);
#else
            m_AssetBundleLoad.LoadSceneAsync(realpath, filename, suffix, progress, complete);
#endif
        }

        void ResourceAsyncCallback(string assetName, Type type, object obj, ResourceLoadComplete callback)
        {
            var decoratedObj = AfterLoad(assetName, type, obj);
            callback?.Invoke(decoratedObj);
        }

        void BeforeLoad(ref string assetName, ref Type type)
        {
            for (var i = m_Decorators.Count - 1; i >= 0; --i)
                m_Decorators[i].BeforeLoad(ref assetName, ref type);
        }

        object AfterLoad(string assetName, Type type, object asset)
        {
            for (var i = 0; i < m_Decorators.Count; ++i)
                m_Decorators[i].AfterLoad(assetName, type, ref asset);
            return asset;
        }
    }
}
