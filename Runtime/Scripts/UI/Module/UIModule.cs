namespace ST.Core.UI
{
    /// <summary>
    /// UI 系统总控制器，负责所有面板的打开/关闭/排序/可见性。
    /// <para>
    /// 使用方式：
    /// <list type="number">
    ///   <item>在启动时调用 <see cref="Setup"/> 注入 <see cref="ResourceModule"/> 与 <see cref="UIRoot"/>。</item>
    ///   <item>调用 <see cref="UIDataTable.Register"/> 注册所有面板/页面配置。</item>
    ///   <item>通过 <see cref="S"/> 单例调用 <see cref="OpenPanel"/> / <see cref="ClosePanel"/> 等 API。</item>
    /// </list>
    /// </para>
    /// <para>
    /// 成员定义见 UIModule_Define.cs，打开与关闭流程见 UIModule_Open.cs，
    /// 查询接口见 UIModule_Query.cs，排序与可见性刷新见 UIModule_Sort.cs。
    /// </para>
    /// </summary>
    public partial class UIModule : ILogicModule
    {
        /// <summary>全局 <see cref="UIModule"/> 单例引用。</summary>
        public static UIModule S { get; private set; }

        /// <summary>构造时注册全局实例，后构造的实例会覆盖先前的引用。</summary>
        public UIModule()
        {
            S = this;
        }

        /// <summary>
        /// 初始化前注入依赖，必须在 <see cref="DoInit"/> 之前调用。
        /// </summary>
        /// <param name="resourceLoad">已初始化的资源加载器。</param>
        /// <param name="uiRoot">场景中的 <see cref="UIRoot"/> 组件。</param>
        public void Setup(ResourceModule resourceLoad, UIRoot uiRoot)
        {
            m_ResourceLoad = resourceLoad;
            m_UIRoot = uiRoot;
        }

        /// <summary>面板按需懒加载，初始化阶段没有额外工作。</summary>
        public override void DoInit() { }

        /// <summary>UIModule 不参与逐帧逻辑更新。</summary>
        public override void DoUpdate() { }

        /// <summary>UIModule 不参与逐帧逻辑更新。</summary>
        public override void DoLateUpdate() { }

        /// <summary>关闭并销毁所有运行中及缓存中的面板，清空状态。</summary>
        public override void DoClose()
        {
            for (int i = m_RunningActives.Count - 1; i >= 0; i--)
            {
                UIPanelActive panelActive = m_RunningActives[i];
                panelActive.Close(true);
            }

            m_RunningActives.Clear();
            m_RunningActivesMap.Clear();

            for (int i = m_CachedActives.Count - 1; i >= 0; i--)
            {
                UIPanelActive panelActive = m_CachedActives[i];
                panelActive.Close(true);
            }

            m_CachedActives.Clear();
        }
    }
}
