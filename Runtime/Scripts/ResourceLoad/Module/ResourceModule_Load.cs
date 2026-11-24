using System;

namespace ST.Core
{
    /// <summary>资源模块加载流程部分，负责选择加载器并执行装饰器链。</summary>
    public partial class ResourceModule
    {
        /// <summary>同步加载路径下全部子资源。</summary>
        /// <param name="path">目录或逻辑路径前缀。</param>
        /// <param name="filename">不含扩展名的资源名。</param>
        /// <param name="suffix">扩展名或后缀。</param>
        /// <returns>加载到的全部子资源，失败时可能为 <c>null</c>。</returns>
        public object[] LoadAllResourceSync(string path, string filename, string suffix)
        {
            string realPath = path + filename;
            object[] assets = null;

#if UNITY_EDITOR
            if (!useAssetBundle)
            {
                assets = m_EditorResLoad.LoadAllSync(realPath, suffix);
            }
            else
            {
                assets = m_AssetBundleLoad.LoadAllSync(realPath);
            }
#else
            assets = m_AssetBundleLoad.LoadAllSync(realPath);
#endif

            return assets;
        }

        /// <summary>同步加载单个资源。</summary>
        /// <param name="path">目录或逻辑路径前缀。</param>
        /// <param name="filename">不含扩展名的资源名。</param>
        /// <param name="suffix">扩展名或后缀。</param>
        /// <param name="restype">逻辑类型，影响装饰器与类型映射。</param>
        /// <returns>加载结果，失败时可能为 <c>null</c>。</returns>
        public object LoadResourceSync(string path, string filename, string suffix,
            ResourceType restype = ResourceType.Default)
        {
            string realPath = path + filename;
            Type type = Type2Native(restype);
            Type originType = type;
            BeforeLoad(ref realPath, ref type);

            object obj = null;

#if UNITY_EDITOR
            if (!useAssetBundle)
            {
                obj = m_EditorResLoad.LoadSync(realPath, suffix, type);
            }
            else
            {
                obj = m_AssetBundleLoad.LoadSync(realPath, filename, type);
            }
#else
            obj = m_AssetBundleLoad.LoadSync(realPath, filename, type);
#endif

            object decoratedObj = AfterLoad(realPath, originType, obj);
            return decoratedObj;
        }

        /// <summary>异步加载单个资源。</summary>
        /// <param name="path">目录或逻辑路径前缀。</param>
        /// <param name="filename">不含扩展名的资源名。</param>
        /// <param name="suffix">扩展名或后缀。</param>
        /// <param name="callback">完成回调，参数可能经装饰器处理。</param>
        /// <param name="restype">逻辑类型，影响装饰器与类型映射。</param>
        public void LoadResourceAsync(string path, string filename, string suffix,
            ResourceLoadComplete callback, ResourceType restype = ResourceType.Default)
        {
            string realPath = path + filename;
            Type type = Type2Native(restype);
            Type originType = type;
            BeforeLoad(ref realPath, ref type);

#if UNITY_EDITOR
            if (!useAssetBundle)
            {
                m_EditorResLoad.LoadAsync(realPath, suffix, type, (obj) =>
                {
                    ResourceAsyncCallback(realPath, originType, obj, callback);
                });
            }
            else
            {
                m_AssetBundleLoad.LoadAsync(realPath, filename, type, (obj) =>
                {
                    ResourceAsyncCallback(realPath, originType, obj, callback);
                });
            }
#else
            m_AssetBundleLoad.LoadAsync(realPath, filename, type, (obj) =>
            {
                ResourceAsyncCallback(realPath, originType, obj, callback);
            });
#endif
        }

        /// <summary>异步加载场景。</summary>
        /// <param name="path">目录或逻辑路径前缀。</param>
        /// <param name="filename">不含扩展名的场景名。</param>
        /// <param name="suffix">场景扩展名。</param>
        /// <param name="progress">可选的进度回调。</param>
        /// <param name="complete">完成回调，参数常为场景相关对象。</param>
        public void LoadSceneAsync(string path, string filename, string suffix,
            ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            string realPath = path + filename;

#if UNITY_EDITOR
            if (!useAssetBundle)
            {
                m_EditorResLoad.LoadSceneAsync(realPath, suffix, progress, complete);
            }
            else
            {
                m_AssetBundleLoad.LoadSceneAsync(realPath, filename, suffix, progress, complete);
            }
#else
            m_AssetBundleLoad.LoadSceneAsync(realPath, filename, suffix, progress, complete);
#endif
        }

        /// <summary>异步完成后按装饰器链处理结果再回调业务。</summary>
        void ResourceAsyncCallback(string assetName, Type type, object obj, ResourceLoadComplete callback)
        {
            object decoratedObj = AfterLoad(assetName, type, obj);
            bool hasCallback = callback != null;
            if (hasCallback)
            {
                callback(decoratedObj);
            }
        }

        /// <summary>逆序调用装饰器的 <see cref="IAssetDecorator.BeforeLoad"/>。</summary>
        void BeforeLoad(ref string assetName, ref Type type)
        {
            for (int i = m_Decorators.Count - 1; i >= 0; --i)
            {
                m_Decorators[i].BeforeLoad(ref assetName, ref type);
            }
        }

        /// <summary>正序调用装饰器的 <see cref="IAssetDecorator.AfterLoad"/> 并返回最终对象。</summary>
        object AfterLoad(string assetName, Type type, object asset)
        {
            for (int i = 0; i < m_Decorators.Count; ++i)
            {
                m_Decorators[i].AfterLoad(assetName, type, ref asset);
            }

            return asset;
        }

        /// <summary>将 <see cref="ResourceType"/> 映射为加载与装饰使用的 CLR 类型。</summary>
        static Type Type2Native(ResourceType type)
        {
            Type nativeType = typeof(object);
            switch (type)
            {
                case ResourceType.String:
                    nativeType = typeof(string);
                    break;
                case ResourceType.Bytes:
                    nativeType = typeof(byte[]);
                    break;
            }

            return nativeType;
        }
    }
}
