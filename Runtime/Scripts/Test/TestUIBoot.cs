using ST.Core.UI;
using UnityEngine;

namespace ST.Core.Test
{
    /// <summary>
    /// UI 系统测试场景启动引导类。
    /// <para>
    /// 负责在测试场景中完整初始化以下模块并将它们串联：
    /// <list type="bullet">
    ///   <item><see cref="ResourceLoad"/> — 资源加载器（编辑器 AssetDatabase 模式）</item>
    ///   <item><see cref="UIManager"/> — UI 系统总控</item>
    ///   <item><see cref="UIDataTable"/> — 面板配置注册表</item>
    ///   <item><see cref="TestGMBoxPanel"/> — GM 指令面板，注册 UI 相关调试指令</item>
    /// </list>
    /// </para>
    /// <para>
    /// 场景搭建步骤：
    /// <list type="number">
    ///   <item>创建 UIRoot GameObject，挂载 <see cref="UIRoot"/> 组件，配置双 Camera / Canvas / PanelRoot。</item>
    ///   <item>创建 Boot GameObject，挂载本脚本，将 UIRoot 拖入 <see cref="m_UIRoot"/> 字段。</item>
    ///   <item>在同一 Boot GameObject（或子节点）上挂载 <see cref="TestGMBoxPanel"/>，拖入 <see cref="m_GMBox"/> 字段。</item>
    ///   <item>运行后按 F1 打开 GM 面板，输入 <c>help</c> 查看所有可用指令。</item>
    /// </list>
    /// </para>
    /// </summary>
    public class TestUIBoot : MonoBehaviour
    {
        // ─── Inspector 字段 ──────────────────────────────────────────────

        /// <summary>场景中的 UIRoot 组件，必须在 Inspector 中拖入。</summary>
        public UIRoot m_UIRoot;

        /// <summary>GM 调试面板组件，必须在 Inspector 中拖入。</summary>
        public TestGMBoxPanel m_GMBox;

        /// <summary>
        /// 自动化流程测试组件，可选；有则在 GM 面板注册 <c>uitest</c> 指令触发入口。
        /// </summary>
        public UIFlowTest m_FlowTest;

        // ─── 私有字段 ────────────────────────────────────────────────────

        /// <summary>异步任务管理器实例，异步资源加载依赖此管理器驱动。</summary>
        AsyncTaskManager m_AsyncTaskManager;

        /// <summary>资源加载器实例，由 Start 创建并持有。</summary>
        ResourceLoad m_ResourceLoad;

        /// <summary>UI 管理器实例，由 Start 创建并持有。</summary>
        UIManager m_UIManager;

        // ─── Unity 生命周期 ──────────────────────────────────────────────

        /// <summary>
        /// 按顺序初始化：AsyncTaskManager → ResourceLoad → UIManager → UIDataTable 注册 → GM 指令注册。
        /// </summary>
        void Start()
        {
            if (!ValidateDependencies())
                return;

            InitAsyncTaskManager();
            InitResourceLoad();
            InitUIManager();
            RegisterTestPanels();
            RegisterGMCommands();

            m_GMBox.AppendOutput("[TestUIBoot] 初始化完成，按 F1 打开 GM 面板，输入 help 查看指令。");
        }

        /// <summary>每帧驱动 <see cref="AsyncTaskManager"/> 与 <see cref="UIManager"/> 的帧更新。</summary>
        void Update()
        {
            if (m_AsyncTaskManager != null)
                m_AsyncTaskManager.DoUpdate();

            if (m_UIManager != null)
                m_UIManager.DoUpdate();
        }

        /// <summary>销毁时依次关闭 UIManager 与 AsyncTaskManager，释放资源。</summary>
        void OnDestroy()
        {
            if (m_UIManager != null)
                m_UIManager.DoClose();

            if (m_AsyncTaskManager != null)
                m_AsyncTaskManager.DoClose();
        }

        // ─── 初始化步骤 ──────────────────────────────────────────────────

        /// <summary>
        /// 检查 Inspector 必填字段；任意为空则输出错误并返回 false。
        /// </summary>
        /// <returns>所有依赖就绪时返回 true。</returns>
        bool ValidateDependencies()
        {
            if (m_UIRoot == null)
            {
                Debug.LogError("[TestUIBoot] m_UIRoot 未赋值，请在 Inspector 中拖入 UIRoot 组件。");
                return false;
            }

            if (m_GMBox == null)
            {
                Debug.LogError("[TestUIBoot] m_GMBox 未赋值，请在 Inspector 中拖入 TestGMBoxPanel 组件。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建并初始化 <see cref="AsyncTaskManager"/>。
        /// 异步资源加载（<see cref="EditorResourceLoad.LoadAsync"/>）依赖
        /// <see cref="BaseAsyncTaskManager.instance"/> 不为 null，必须在 ResourceLoad 之前初始化。
        /// </summary>
        void InitAsyncTaskManager()
        {
            m_AsyncTaskManager = new AsyncTaskManager();
            m_AsyncTaskManager.DoInit();
        }

        /// <summary>
        /// 创建并初始化 <see cref="ResourceLoad"/>，使用内嵌的 <see cref="TestResourceConfig"/>。
        /// 编辑器下走 AssetDatabase 模式，无需打 AssetBundle。
        /// </summary>
        void InitResourceLoad()
        {
            m_ResourceLoad = new ResourceLoad();
            m_ResourceLoad.SetConfig(new TestResourceConfig());
            m_ResourceLoad.DoInit();
        }

        /// <summary>
        /// 创建 <see cref="UIManager"/>，注入资源加载器与 UIRoot，完成初始化。
        /// </summary>
        void InitUIManager()
        {
            m_UIManager = new UIManager();
            m_UIManager.Setup(m_ResourceLoad, m_UIRoot);
            m_UIManager.DoInit();
        }

        /// <summary>
        /// 向 <see cref="UIDataTable"/> 注册测试场景所需的面板配置。
        /// <para>
        /// 注意：注册的 Prefab 根节点必须挂载继承自 <see cref="AbstractPanel"/> 的脚本，
        /// 否则加载后 <see cref="UIPanelActive"/> 会输出警告并销毁实例化对象。
        /// </para>
        /// 新增测试面板时在此方法内追加 <see cref="UIDataTable.Register"/> 调用。
        /// </summary>
        void RegisterTestPanels()
        {
            UIDataTable.Register(new UIData
            {
                uiID        = TestUIID.GMBoxPanel,
                name        = "GMBoxPanel",
                path        = "ui/uiprefab/",
                filename    = "ui_panel_gm_box",
                suffix      = ".prefab",
                sortLayer   = PanelSortLayer.Top,
                cacheCount  = 1,
                isSingleton = true,
            });

            UIDataTable.Register(new UIData
            {
                uiID        = TestUIID.TestPanel,
                name        = "TestPanel",
                path        = "ui/uiprefab/",
                filename    = "ui_panel_test",
                suffix      = ".prefab",
                sortLayer   = PanelSortLayer.Auto,
                cacheCount  = 1,
                isSingleton = true,
            });

            UIDataTable.Register(new UIData
            {
                uiID        = TestUIID.TestModal,
                name        = "TestModal",
                path        = "ui/uiprefab/",
                filename    = "ui_panel_test_modal",
                suffix      = ".prefab",
                sortLayer   = PanelSortLayer.Auto,
                cacheCount  = 1,
                isSingleton = true,
            });

            // Page 注册：路径与 Panel 相同目录，sortLayer 对 Page 无实际意义（由父 Panel 决定）
            UIDataTable.Register(new UIData
            {
                uiID        = TestUIID.TestPageA,
                name        = "TestPageA",
                path        = "ui/uiprefab/",
                filename    = "ui_page_test_a",
                suffix      = ".prefab",
                sortLayer   = PanelSortLayer.Auto,
                cacheCount  = 1,
                isSingleton = false,
            });

            UIDataTable.Register(new UIData
            {
                uiID        = TestUIID.TestBagPanel,
                name        = "TestBagPanel",
                path        = "ui/uiprefab/",
                filename    = "ui_panel_test_bag",
                suffix      = ".prefab",
                sortLayer   = PanelSortLayer.Auto,
                cacheCount  = 1,
                isSingleton = true,
            });
        }

        /// <summary>
        /// 向 <see cref="TestGMBoxPanel"/> 注册 UI 系统相关的调试 GM 指令。
        /// </summary>
        void RegisterGMCommands()
        {
            m_GMBox.RegisterCommand("openui",    CmdOpenUI,    "打开面板：openui <uiID(int)>");
            m_GMBox.RegisterCommand("closeui",   CmdCloseUI,   "关闭面板：closeui <uiID(int)>");
            m_GMBox.RegisterCommand("closeall",  CmdCloseAll,  "关闭所有面板");
            m_GMBox.RegisterCommand("isopen",    CmdIsOpen,    "查询面板是否打开：isopen <uiID(int)>");
            m_GMBox.RegisterCommand("listpanel", CmdListPanel, "列出所有已注册面板配置");
            m_GMBox.RegisterCommand("openpage",  CmdOpenPage,  "挂载子页面到 TestPanel：openpage");
            m_GMBox.RegisterCommand("closepage", CmdClosePage, "卸载 TestPanel 的子页面：closepage");
            m_GMBox.RegisterCommand("openbag",   CmdOpenBag,   "打开背包面板：openbag [itemCount=50]");
            m_GMBox.RegisterCommand("refreshbag",CmdRefreshBag,"刷新背包格子数量：refreshbag <count>");

            if (m_FlowTest != null)
                m_GMBox.RegisterCommand("uitest", _ => m_FlowTest.RunTests(), "运行 UI 自动化流程测试（等同按 F2）");
        }

        // ─── GM 指令实现 ─────────────────────────────────────────────────

        /// <summary>
        /// 指令 <c>openui</c>：解析 uiID 并调用 <see cref="UIManager.OpenPanel"/>。
        /// 用法：<c>openui 1</c>
        /// </summary>
        void CmdOpenUI(string[] args)
        {
            if (args.Length < 2)
            {
                m_GMBox.AppendOutput("[openui] 用法：openui <uiID(int)>");
                return;
            }

            if (!int.TryParse(args[1], out int uiID))
            {
                m_GMBox.AppendOutput(string.Format("[openui] 参数不是有效整数：{0}", args[1]));
                return;
            }

            int panelID = UIManager.S.OpenPanel(uiID);
            m_GMBox.AppendOutput(string.Format("[openui] uiID={0}  panelID={1}", uiID, panelID));
        }

        /// <summary>
        /// 指令 <c>closeui</c>：解析 uiID 并调用 <see cref="UIManager.ClosePanel"/>。
        /// 用法：<c>closeui 1</c>
        /// </summary>
        void CmdCloseUI(string[] args)
        {
            if (args.Length < 2)
            {
                m_GMBox.AppendOutput("[closeui] 用法：closeui <uiID(int)>");
                return;
            }

            if (!int.TryParse(args[1], out int uiID))
            {
                m_GMBox.AppendOutput(string.Format("[closeui] 参数不是有效整数：{0}", args[1]));
                return;
            }

            UIManager.S.ClosePanel(uiID);
            m_GMBox.AppendOutput(string.Format("[closeui] uiID={0} 已关闭", uiID));
        }

        /// <summary>
        /// 指令 <c>closeall</c>：调用 <see cref="UIManager.DoClose"/> 后重新 <see cref="UIManager.DoInit"/>。
        /// </summary>
        void CmdCloseAll(string[] args)
        {
            m_UIManager.DoClose();
            m_UIManager.DoInit();
            m_GMBox.AppendOutput("[closeall] 所有面板已关闭。");
        }

        /// <summary>
        /// 指令 <c>isopen</c>：查询指定 uiID 的面板是否处于打开状态。
        /// 用法：<c>isopen 1</c>
        /// </summary>
        void CmdIsOpen(string[] args)
        {
            if (args.Length < 2)
            {
                m_GMBox.AppendOutput("[isopen] 用法：isopen <uiID(int)>");
                return;
            }

            if (!int.TryParse(args[1], out int uiID))
            {
                m_GMBox.AppendOutput(string.Format("[isopen] 参数不是有效整数：{0}", args[1]));
                return;
            }

            bool opened  = UIManager.S.IsOpened(uiID);
            bool visible = UIManager.S.IsPanelVisible(uiID);
            bool active  = UIManager.S.IsPanelActive(uiID);

            m_GMBox.AppendOutput(string.Format(
                "[isopen] uiID={0}  IsOpened={1}  IsVisible={2}  IsActive={3}",
                uiID, opened, visible, active));
        }

        /// <summary>
        /// 指令 <c>listpanel</c>：列出当前已激活注册的 <see cref="TestUIID"/> 常量。
        /// </summary>
        void CmdListPanel(string[] args)
        {
            m_GMBox.AppendOutput("── 已注册 TestUIID ──");
            m_GMBox.AppendOutput(string.Format("  {0,-6} GMBoxPanel   (ui_panel_gm_box.prefab)        Top", TestUIID.GMBoxPanel));
            m_GMBox.AppendOutput(string.Format("  {0,-6} TestPanel    (ui_panel_test.prefab)           Auto", TestUIID.TestPanel));
            m_GMBox.AppendOutput(string.Format("  {0,-6} TestModal    (ui_panel_test_modal.prefab)     Auto  HideMask=HideAndUnInteractive", TestUIID.TestModal));
            m_GMBox.AppendOutput(string.Format("  {0,-6} TestPageA    (ui_page_test_a.prefab)          Page（挂载于 TestPanel）", TestUIID.TestPageA));
            m_GMBox.AppendOutput(string.Format("  {0,-6} TestBagPanel (ui_panel_test_bag.prefab)       Auto  ScrollView+GridLayout", TestUIID.TestBagPanel));
        }

        /// <summary>
        /// 指令 <c>openpage</c>：向已打开的 TestPanel 挂载子页面 A。
        /// 用法：<c>openpage</c>（TestPanel 必须已打开）
        /// </summary>
        void CmdOpenPage(string[] args)
        {
            var testPanel = UIManager.S != null ? UIManager.S.FindPanel<TestPanel>() : null;
            if (testPanel == null)
            {
                m_GMBox.AppendOutput("[openpage] TestPanel 未打开或尚未加载完成，请先 openui 2");
                return;
            }

            testPanel.OpenPageA("via GM");
            m_GMBox.AppendOutput("[openpage] TestPageA OpenPageA 请求已发送");
        }

        /// <summary>
        /// 指令 <c>closepage</c>：从 TestPanel 卸载子页面 A。
        /// 用法：<c>closepage</c>
        /// </summary>
        void CmdClosePage(string[] args)
        {
            var testPanel = UIManager.S != null ? UIManager.S.FindPanel<TestPanel>() : null;
            if (testPanel == null)
            {
                m_GMBox.AppendOutput("[closepage] TestPanel 未打开或尚未加载完成");
                return;
            }

            testPanel.ClosePageA();
            m_GMBox.AppendOutput("[closepage] TestPageA ClosePageA 请求已发送");
        }

        /// <summary>
        /// 指令 <c>openbag</c>：打开背包测试面板，可选指定格子数量。
        /// 用法：<c>openbag</c> 或 <c>openbag 100</c>
        /// </summary>
        void CmdOpenBag(string[] args)
        {
            int count = 50;
            if (args.Length >= 2 && int.TryParse(args[1], out int n))
                count = n;

            int panelID = UIManager.S.OpenPanel(TestUIID.TestBagPanel, count);
            m_GMBox.AppendOutput(string.Format("[openbag] count={0}  panelID={1}", count, panelID));
        }

        /// <summary>
        /// 指令 <c>refreshbag</c>：刷新已打开背包面板的格子数量。
        /// 用法：<c>refreshbag 30</c>
        /// </summary>
        void CmdRefreshBag(string[] args)
        {
            if (args.Length < 2)
            {
                m_GMBox.AppendOutput("[refreshbag] 用法：refreshbag <count>");
                return;
            }

            if (!int.TryParse(args[1], out int count))
            {
                m_GMBox.AppendOutput(string.Format("[refreshbag] 参数不是有效整数：{0}", args[1]));
                return;
            }

            var bag = UIManager.S != null ? UIManager.S.FindPanel<TestBagPanel>() : null;
            if (bag == null)
            {
                m_GMBox.AppendOutput("[refreshbag] 背包面板未打开或未加载完成，请先 openbag");
                return;
            }

            bag.RefreshItems(count);
            m_GMBox.AppendOutput(string.Format("[refreshbag] 已刷新为 {0} 个格子", count));
        }

        // ══════════════════════════════════════════
        // 内嵌配置
        // ══════════════════════════════════════════

        // UI ID 常量见 TestUIID.cs（ST.Core.Test.TestUIID）

        /// <summary>
        /// 测试专用资源配置，路径指向 com.spacetime.core 的 Assets/Package 目录。
        /// </summary>
        class TestResourceConfig : IResourceConfig
        {
            /// <inheritdoc />
            public string appName           { get { return "spacetime"; } }
            /// <inheritdoc />
            public string assetDir          { get { return "assetBundle"; } }
            /// <inheritdoc />
            public string bundleSuffix      { get { return ".unity3d"; } }
            /// <inheritdoc />
            public string editorPathPrefix  { get { return "Assets/Package/"; } }
            /// <inheritdoc />
            public string assetBundleDBFile { get { return "assetbundledb.txt"; } }
        }
    }
}
