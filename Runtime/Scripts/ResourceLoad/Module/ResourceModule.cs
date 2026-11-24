namespace ST.Core
{
    /// <summary>
    /// 资源模块：编辑器下可选 AssetDatabase 直读或走 AssetBundle，真机始终走 Bundle。
    /// 须在 <see cref="SetConfig"/> 之后再调用 <see cref="DoInit"/>。
    /// 成员定义见 ResourceModule_Define.cs，加载流程见 ResourceModule_Load.cs。
    /// </summary>
    public partial class ResourceModule : ILogicModule
    {
        /// <summary>构造时注册全局实例并清空装饰器列表。</summary>
        public ResourceModule()
        {
            s_Instance = this;
            m_Decorators.Clear();
        }

        /// <summary>全局资源加载器实例，与最后一次构造的实例对应。</summary>
        public static ResourceModule instance
        {
            get
            {
                return s_Instance;
            }
        }

        /// <summary>
        /// 注入资源配置并创建 <see cref="FilePathHelper"/>，须在 <see cref="DoInit"/> 前调用。
        /// </summary>
        /// <param name="config">非空的资源配置实现。</param>
        public void SetConfig(IResourceConfig config)
        {
            m_Config = config;
            m_FilePathHelper = new FilePathHelper(config);
        }

        /// <summary>注册资产装饰器，相同实例不重复添加。</summary>
        /// <param name="decorator">待注册的资产装饰器。</param>
        public void InstallDecorator(IAssetDecorator decorator)
        {
            bool isDecoratorInstalled = m_Decorators.Contains(decorator);
            if (isDecoratorInstalled)
            {
                return;
            }

            m_Decorators.Add(decorator);
        }

        /// <summary>
        /// 创建编辑器与 Bundle 加载器、初始化清单并安装 <see cref="LuaAssetDecorator"/>。
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

        /// <summary>卸载所有已加载的 AssetBundle，释放资源以支持重新 <see cref="DoInit"/>。</summary>
        public override void DoClose()
        {
            bool hasAssetBundleLoad = m_AssetBundleLoad != null;
            if (hasAssetBundleLoad)
            {
                m_AssetBundleLoad.DoClose(false);
            }
        }
    }
}
