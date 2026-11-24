using System;

namespace ST.Core
{
    /// <summary>
    /// 默认资源加载实现：编辑器下可选 AssetDatabase 直读或走 AssetBundle；真机始终走 Bundle。
    /// 须在 <see cref="BaseResourceModule.SetConfig"/> 之后再调用 <see cref="ILogicModule.DoInit"/>。
    /// </summary>
    public class ResourceModule : BaseResourceModule
    {
        /// <summary>为 <c>true</c> 时编辑器也使用 AssetBundle 路径加载（用于本地验证包体）。</summary>
        public static bool useAssetBundle = false;

        /// <summary>编辑器模式（非 AssetBundle）加载器。</summary>
        EditorResourceLoad m_EditorResLoad;
        /// <summary>AssetBundle 模式加载器。</summary>
        AssetBundleLoad m_AssetBundleLoad;

        /// <summary>卸载所有已加载的 AssetBundle，释放资源以支持重新 <see cref="DoInit"/>。</summary>
        public override void DoClose()
        {
            if (m_AssetBundleLoad != null)
            {
                m_AssetBundleLoad.DoClose(false);
            }
        }

        /// <summary>
        /// 创建编辑器/Bundle 加载器、初始化清单并安装 <see cref="LuaAssetDecorator"/>。
        /// <para>
        /// 编辑器模式下仅当 <see cref="useAssetBundle"/> 为 <c>true</c> 时才初始化 AssetBundle 加载器，
        /// 避免在未打包时因找不到 <c>assetbundledb.txt</c> 而产生错误日志。
        /// </para>
        /// </summary>
        public override void DoInit()
        {
            m_EditorResLoad = new EditorResourceLoad(m_Config);
            m_AssetBundleLoad = new AssetBundleLoad(m_FilePathHelper, m_Config);

#if UNITY_EDITOR
            if (useAssetBundle)
            {
                m_AssetBundleLoad.DoInit();
            }
#else
            m_AssetBundleLoad.DoInit();
#endif

            InstallDecorator(new LuaAssetDecorator());
        }

        /// <summary>无每帧逻辑。</summary>
        public override void DoUpdate() { }

        /// <inheritdoc />
        public override object[] LoadAllResourceSync(string path, string filename, string suffix)
        {
            string realPath = path + filename;

#if UNITY_EDITOR
            if (!useAssetBundle)
            {
                return m_EditorResLoad.LoadAllSync(realPath, suffix);
            }

            return m_AssetBundleLoad.LoadAllSync(realPath);
#else
            return m_AssetBundleLoad.LoadAllSync(realPath);
#endif
        }

        /// <inheritdoc />
        public override object LoadResourceSync(string path, string filename, string suffix,
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

            return AfterLoad(realPath, originType, obj);
        }

        /// <inheritdoc />
        public override void LoadResourceAsync(string path, string filename, string suffix,
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

        /// <inheritdoc />
        public override void LoadSceneAsync(string path, string filename, string suffix,
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
            callback?.Invoke(decoratedObj);
        }

        /// <summary>逆序调用装饰器 <see cref="IAssetDecorator.BeforeLoad"/>。</summary>
        void BeforeLoad(ref string assetName, ref Type type)
        {
            for (int i = m_Decorators.Count - 1; i >= 0; --i)
            {
                m_Decorators[i].BeforeLoad(ref assetName, ref type);
            }
        }

        /// <summary>正序调用装饰器 <see cref="IAssetDecorator.AfterLoad"/> 并返回最终对象。</summary>
        object AfterLoad(string assetName, Type type, object asset)
        {
            for (int i = 0; i < m_Decorators.Count; ++i)
            {
                m_Decorators[i].AfterLoad(assetName, type, ref asset);
            }

            return asset;
        }
    }
}
