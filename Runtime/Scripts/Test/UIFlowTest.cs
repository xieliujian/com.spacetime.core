using System.Collections;
using System.Collections.Generic;
using ST.Core.UI;
using UnityEngine;

namespace ST.Core.Test
{
    /// <summary>
    /// UI 系统测试器，支持两种模式：
    /// <list type="bullet">
    ///   <item><b>自动模式（F2）</b>：依次执行全部 TC，结果输出到 Console 与 GM 面板。</item>
    ///   <item><b>手动调试 GUI（F3）</b>：IMGUI 悬浮窗，可查看实时面板状态、快速操作、逐步执行单条 TC。</item>
    /// </list>
    /// </summary>
    public class UIFlowTest : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────

        /// <summary>GM 输出面板，可选；留空时 Start() 自动查找场景中第一个。</summary>
        public TestGMBoxPanel m_GMBox;

        // ─── 常量 ────────────────────────────────────────────────────────

        /// <summary>等待异步加载完成的最大秒数。</summary>
        const float k_LoadTimeout = 5f;

        // ═══════════════════════════════════════════════════════════════
        // 自动模式 — 状态
        // ═══════════════════════════════════════════════════════════════

        /// <summary>本次自动运行的用例总数。</summary>
        int m_TotalTests;

        /// <summary>本次自动运行通过的用例数。</summary>
        int m_PassedTests;

        /// <summary>是否正在自动运行中（防止重复触发）。</summary>
        bool m_IsRunning;

        // ═══════════════════════════════════════════════════════════════
        // 手动 GUI 模式 — 状态
        // ═══════════════════════════════════════════════════════════════

        /// <summary>手动调试 GUI 窗口是否可见。</summary>
        bool m_ShowGUI;

        /// <summary>GUI 窗口矩形，支持拖拽移动。</summary>
        Rect m_WinRect = new Rect(20, 20, 740, 640);

        /// <summary>日志区域滚动位置。</summary>
        Vector2 m_LogScroll;

        /// <summary>步骤列表区域滚动位置。</summary>
        Vector2 m_StepScroll;

        /// <summary>GUI 日志缓冲，最多 60 行。</summary>
        readonly List<string> m_GuiLog = new List<string>(64);

        /// <summary>当前选中的手动 TC 索引（-1 = 未选中）。</summary>
        int m_ActiveTCIdx = -1;

        /// <summary>全部手动 TC 数组，由 <see cref="InitManualTCs"/> 构建。</summary>
        ManualTC[] m_ManualTCs;

        /// <summary>当前步骤是否正在异步等待中（防止重复点击）。</summary>
        bool m_StepRunning;

        // 步骤间跨步共享的临时状态
        int        m_TmpPanelID1;
        GameObject m_TmpGO1;
        int        m_TmpCountA;

        // ─── Unity 生命周期 ──────────────────────────────────────────────

        /// <summary>查找 GMBox、注册 uitest 指令、初始化手动 TC 定义。</summary>
        void Start()
        {
            if (m_GMBox == null)
                m_GMBox = FindObjectOfType<TestGMBoxPanel>();

            if (m_GMBox != null)
                m_GMBox.RegisterCommand("uitest", OnCmdUITest, "运行 UI 系统自动化测试（等同按 F2）");

            InitManualTCs();

            GUILog("[UIFlowTest] 就绪  F2=自动  F3=手动GUI");
            Debug.Log("[UIFlowTest] 就绪，F2 自动测试，F3 开启手动调试 GUI。");
        }

        /// <summary>F2 → 自动测试；F3 → 切换手动 GUI 窗口。</summary>
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2) && !m_IsRunning)
                StartCoroutine(RunAllTests());

            if (Input.GetKeyDown(KeyCode.F3))
                m_ShowGUI = !m_ShowGUI;
        }

        // ═══════════════════════════════════════════════════════════════
        // 公共接口
        // ═══════════════════════════════════════════════════════════════

        /// <summary>外部触发全套自动测试（TestUIBoot uitest 指令调用此方法）。</summary>
        public void RunTests()
        {
            if (!m_IsRunning)
                StartCoroutine(RunAllTests());
        }

        // ═══════════════════════════════════════════════════════════════
        // ═══  手动调试 GUI  ═══════════════════════════════════════════
        // ═══════════════════════════════════════════════════════════════

        /// <summary>绘制手动调试 GUI 窗口（F3 开关）。</summary>
        void OnGUI()
        {
            if (!m_ShowGUI) return;

            m_WinRect = GUILayout.Window(9901, m_WinRect, DrawWindow, string.Empty,
                GUILayout.Width(740), GUILayout.MinHeight(200));
        }

        /// <summary>GUILayout.Window 绘制回调。</summary>
        void DrawWindow(int id)
        {
            // ── 标题行 ─────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>UIFlowTest 手动调试</b>", RichStyle(), GUILayout.ExpandWidth(true));
            if (m_IsRunning)
                GUILayout.Label("自动测试运行中…", GUILayout.Width(130));
            else if (GUILayout.Button("▶ 自动全跑 F2", GUILayout.Width(130)))
                StartCoroutine(RunAllTests());
            if (GUILayout.Button("✕ 关闭 F3", GUILayout.Width(90)))
                m_ShowGUI = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            DrawSeparator();

            // ── 面板状态 ────────────────────────────────────────────────
            DrawStateSection();
            DrawSeparator();

            // ── 快速操作 ────────────────────────────────────────────────
            DrawQuickControls();
            DrawSeparator();

            // ── 分步测试 ────────────────────────────────────────────────
            DrawStepSection();
            DrawSeparator();

            // ── 日志 ────────────────────────────────────────────────────
            DrawLogSection();

            GUI.DragWindow(new Rect(0, 0, m_WinRect.width, 24));
        }

        // ── 面板状态 ──────────────────────────────────────────────────

        /// <summary>绘制各已知面板的实时状态表格。</summary>
        void DrawStateSection()
        {
            GUILayout.Label("■ 面板状态", BoldStyle());
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#aaa>ID   名称            IsOpen   Visible   Active</color>",
                RichStyle(), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            DrawPanelRow(TestUIID.GMBoxPanel,  "GMBoxPanel");
            DrawPanelRow(TestUIID.TestPanel,   "TestPanel");
            DrawPanelRow(TestUIID.TestModal,   "TestModal");
            DrawPanelRow(TestUIID.TestBagPanel,"TestBagPanel");

            // 子页面行
            GUILayout.BeginHorizontal();
            bool pageAOpen = UIManager.S != null && UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA);
            string pageAStr = pageAOpen ? "<color=#4f4>●</color>" : "<color=#666>○</color>";
            GUILayout.Label(string.Format("  {0,-4} {1,-14}  {2}        <color=#aaa>(Page)</color>",
                TestUIID.TestPageA, "TestPageA", pageAStr), RichStyle());
            GUILayout.EndHorizontal();

            // 背包格子数量行
            GUILayout.BeginHorizontal();
            var bagPanel = UIManager.S != null ? UIManager.S.FindPanel<TestBagPanel>() : null;
            string bagItemStr = bagPanel != null
                ? string.Format("<color=#ff8>格子数={0}</color>", bagPanel.itemCount)
                : "<color=#666>（未加载）</color>";
            GUILayout.Label(string.Format("       {0,-14}  {1}", "  itemCount", bagItemStr), RichStyle());
            GUILayout.EndHorizontal();
        }

        /// <summary>绘制单个面板的状态行。</summary>
        void DrawPanelRow(int uiID, string label)
        {
            if (UIManager.S == null) return;

            bool open    = UIManager.S.IsOpened(uiID);
            bool visible = UIManager.S.IsPanelVisible(uiID);
            bool active  = UIManager.S.IsPanelActive(uiID);

            string openStr    = open    ? "<color=#4f4>●</color>" : "<color=#666>○</color>";
            string visibleStr = visible ? "<color=#4f4>●</color>" : "<color=#666>○</color>";
            string activeStr  = active  ? "<color=#4f4>●</color>" : "<color=#666>○</color>";

            GUILayout.Label(string.Format("  {0,-4} {1,-14}  {2}        {3}        {4}",
                uiID, label, openStr, visibleStr, activeStr), RichStyle());
        }

        // ── 快速操作 ──────────────────────────────────────────────────

        /// <summary>绘制快速操作按钮行。</summary>
        void DrawQuickControls()
        {
            GUILayout.Label("■ 快速操作", BoldStyle());

            // 面板行
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open 1",    GUILayout.Width(70))) DoOpen(TestUIID.GMBoxPanel);
            if (GUILayout.Button("Close 1",   GUILayout.Width(70))) DoClose(TestUIID.GMBoxPanel);
            GUILayout.Space(10);
            if (GUILayout.Button("Open 2",    GUILayout.Width(70))) DoOpen(TestUIID.TestPanel);
            if (GUILayout.Button("Close 2",   GUILayout.Width(70))) DoClose(TestUIID.TestPanel);
            GUILayout.Space(10);
            if (GUILayout.Button("Open 3",    GUILayout.Width(70))) DoOpen(TestUIID.TestModal);
            if (GUILayout.Button("Close 3",   GUILayout.Width(70))) DoClose(TestUIID.TestModal);
            GUILayout.Space(10);
            if (GUILayout.Button("Close All", GUILayout.Width(80)))
            {
                UIManager.S.DoClose();
                UIManager.S.DoInit();
                GUILog("[操作] Close All");
            }
            GUILayout.EndHorizontal();

            // 子页面行（需要 TestPanel 已打开）
            GUILayout.BeginHorizontal();
            GUILayout.Label("  Page A（需 TestPanel 已打开）：", GUILayout.Width(200));
            if (GUILayout.Button("Attach PageA",  GUILayout.Width(110))) DoAttachPageA();
            if (GUILayout.Button("Detach PageA",  GUILayout.Width(110))) DoDetachPageA();
            GUILayout.EndHorizontal();

            // 背包行
            GUILayout.BeginHorizontal();
            GUILayout.Label("  背包 ScrollView：", GUILayout.Width(130));
            if (GUILayout.Button("Open Bag(50)",  GUILayout.Width(100))) DoOpenBag(50);
            if (GUILayout.Button("Open Bag(10)",  GUILayout.Width(100))) DoOpenBag(10);
            if (GUILayout.Button("Close Bag",     GUILayout.Width(85)))  DoClose(TestUIID.TestBagPanel);
            if (GUILayout.Button("Refresh(10)",   GUILayout.Width(90)))  DoRefreshBag(10);
            if (GUILayout.Button("Refresh(100)",  GUILayout.Width(95)))  DoRefreshBag(100);
            GUILayout.EndHorizontal();
        }

        /// <summary>向 TestPanel 挂载 TestPageA。</summary>
        void DoAttachPageA()
        {
            if (UIManager.S == null) return;
            var panel = UIManager.S.FindPanel<TestPanel>();
            if (panel == null)
            {
                GUILog("[操作] Attach PageA 失败：TestPanel 未打开或未加载完成");
                return;
            }
            panel.OpenPageA("via GUI");
            GUILog("[操作] AttachPage TestPageA");
        }

        /// <summary>从 TestPanel 卸载 TestPageA。</summary>
        void DoDetachPageA()
        {
            if (UIManager.S == null) return;
            var panel = UIManager.S.FindPanel<TestPanel>();
            if (panel == null)
            {
                GUILog("[操作] Detach PageA 失败：TestPanel 未打开或未加载完成");
                return;
            }
            panel.ClosePageA();
            GUILog("[操作] DettachPage TestPageA");
        }

        /// <summary>打开背包面板并传入格子数量。</summary>
        void DoOpenBag(int count)
        {
            if (UIManager.S == null) return;
            int pid = UIManager.S.OpenPanel(TestUIID.TestBagPanel, count);
            GUILog(string.Format("[操作] OpenPanel(Bag, {0}) → panelID={1}", count, pid));
        }

        /// <summary>刷新背包格子数量（需面板已加载完成）。</summary>
        void DoRefreshBag(int count)
        {
            var bag = UIManager.S != null ? UIManager.S.FindPanel<TestBagPanel>() : null;
            if (bag == null)
            {
                GUILog("[操作] RefreshBag 失败：背包未打开或未加载完成");
                return;
            }
            bag.RefreshItems(count);
            GUILog(string.Format("[操作] RefreshItems({0})", count));
        }

        void DoOpen(int uiID)
        {
            if (UIManager.S == null) return;
            int pid = UIManager.S.OpenPanel(uiID);
            GUILog(string.Format("[操作] OpenPanel({0}) → panelID={1}", uiID, pid));
        }

        void DoClose(int uiID)
        {
            if (UIManager.S == null) return;
            UIManager.S.ClosePanel(uiID);
            GUILog(string.Format("[操作] ClosePanel({0})", uiID));
        }

        // ── 分步测试 ──────────────────────────────────────────────────

        /// <summary>绘制 TC 选择器与当前 TC 的逐步执行区域。</summary>
        void DrawStepSection()
        {
            GUILayout.Label("■ 分步执行测试用例", BoldStyle());

            // TC 选择按钮行
            GUILayout.BeginHorizontal();
            for (int i = 0; i < m_ManualTCs.Length; i++)
            {
                bool selected = (m_ActiveTCIdx == i);
                GUI.color = selected ? Color.cyan : Color.white;
                if (GUILayout.Button(m_ManualTCs[i].shortName, GUILayout.Width(60)))
                    SelectTC(i);
                GUI.color = Color.white;
            }
            GUILayout.FlexibleSpace();
            if (m_ActiveTCIdx >= 0)
            {
                if (GUILayout.Button("重置", GUILayout.Width(50)))
                    ResetActiveTC();
            }
            GUILayout.EndHorizontal();

            if (m_ActiveTCIdx < 0) return;

            var tc = m_ManualTCs[m_ActiveTCIdx];

            GUILayout.Label(string.Format("  {0}  [{1}/{2} 步]",
                tc.name,
                tc.DoneCount,
                tc.steps.Length), BoldStyle());

            // 步骤列表（滚动区域）
            m_StepScroll = GUILayout.BeginScrollView(m_StepScroll, GUILayout.Height(180));

            int activeIdx = tc.ActiveStepIndex;
            for (int i = 0; i < tc.steps.Length; i++)
            {
                var step = tc.steps[i];
                bool isCurrent = (i == activeIdx);

                GUILayout.BeginHorizontal();

                // 状态图标
                string icon;
                switch (step.status)
                {
                    case StepStatus.Pass:    icon = "<color=#4f4>✓</color>"; break;
                    case StepStatus.Fail:    icon = "<color=#f44>✗</color>"; break;
                    case StepStatus.Waiting: icon = "<color=#ff4>⏳</color>"; break;
                    default:                 icon = isCurrent ? "<color=#4af>►</color>" : "  "; break;
                }

                GUILayout.Label(string.Format("{0} {1,2}. {2}", icon, i + 1, step.desc),
                    RichStyle(), GUILayout.Width(350));

                // 期望 / 实际结果
                if (step.status == StepStatus.Pass)
                    GUILayout.Label("<color=#4f4>" + step.expectDesc + "</color>", RichStyle(), GUILayout.ExpandWidth(true));
                else if (step.status == StepStatus.Fail)
                    GUILayout.Label("<color=#f44>" + step.expectDesc + "</color>", RichStyle(), GUILayout.ExpandWidth(true));
                else if (step.status == StepStatus.Waiting)
                    GUILayout.Label("<color=#ff4>等待加载…</color>", RichStyle(), GUILayout.ExpandWidth(true));
                else
                    GUILayout.Label("<color=#666>" + step.expectDesc + "</color>", RichStyle(), GUILayout.ExpandWidth(true));

                // 执行按钮（当前步骤）
                if (isCurrent && !m_StepRunning)
                {
                    if (GUILayout.Button("▶ 执行", GUILayout.Width(65)))
                        StartCoroutine(ExecuteStep(tc, step));
                }
                else if (step.status == StepStatus.Fail)
                {
                    if (GUILayout.Button("重试", GUILayout.Width(65)))
                    {
                        step.status = StepStatus.Pending;
                        StartCoroutine(ExecuteStep(tc, step));
                    }
                }
                else
                {
                    GUILayout.Space(69);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            // 全部通过时显示 PASS 横幅
            if (activeIdx == -1 && tc.DoneCount == tc.steps.Length)
            {
                bool allPass = tc.AllPassed;
                GUI.color = allPass ? Color.green : new Color(1f, 0.5f, 0.5f);
                GUILayout.Label(allPass ? "  ✓  全部通过" : "  ✗  有步骤失败，点击 [重置] 重新运行",
                    BoldStyle());
                GUI.color = Color.white;
            }
        }

        /// <summary>选中一个 TC（不重置步骤，允许继续之前进度）。</summary>
        void SelectTC(int idx)
        {
            m_ActiveTCIdx = idx;
        }

        /// <summary>重置当前 TC 所有步骤为 Pending。</summary>
        void ResetActiveTC()
        {
            if (m_ActiveTCIdx >= 0)
                m_ManualTCs[m_ActiveTCIdx].Reset();
        }

        /// <summary>
        /// 异步协程执行单个步骤：运行 Action → 若需等待则轮询 Check → 设置 Pass/Fail。
        /// </summary>
        IEnumerator ExecuteStep(ManualTC tc, Step step)
        {
            m_StepRunning = true;
            step.status   = StepStatus.Waiting;

            // 执行 Action
            if (step.action != null)
                step.action();

            // 等待异步加载（轮询 check）
            if (step.waitLoad && step.check != null)
            {
                float elapsed = 0f;
                while (!step.check() && elapsed < k_LoadTimeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // 给 Unity 一帧刷新时间（有些操作需要帧边界才生效）
                yield return null;
            }

            // 判断结果
            bool passed = (step.check == null) || step.check();
            step.status = passed ? StepStatus.Pass : StepStatus.Fail;

            GUILog(string.Format("[{0}] Step{1} {2}  {3}",
                tc.shortName, System.Array.IndexOf(tc.steps, step) + 1,
                passed ? "PASS" : "FAIL",
                step.expectDesc));

            if (!passed)
                Debug.LogWarning(string.Format("[UIFlowTest] {0} Step FAIL: {1}", tc.name, step.desc));

            m_StepRunning = false;
        }

        // ── 日志区域 ─────────────────────────────────────────────────

        /// <summary>绘制 GUI 内置日志区域（最近 60 条）。</summary>
        void DrawLogSection()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("■ 操作日志", BoldStyle());
            if (GUILayout.Button("清空", GUILayout.Width(50)))
                m_GuiLog.Clear();
            GUILayout.EndHorizontal();

            m_LogScroll = GUILayout.BeginScrollView(m_LogScroll, GUILayout.Height(110));
            for (int i = m_GuiLog.Count - 1; i >= 0; i--)
                GUILayout.Label(m_GuiLog[i], RichStyle());
            GUILayout.EndScrollView();
        }

        // ─── 手动 TC 定义 ────────────────────────────────────────────

        /// <summary>构建所有手动 TC 的步骤数组。</summary>
        void InitManualTCs()
        {
            m_ManualTCs = new ManualTC[]
            {
                BuildManualTC01(),
                BuildManualTC02(),
                BuildManualTC03(),
                BuildManualTC04(),
                BuildManualTC05(),
                BuildManualTC06(),
                BuildManualTC07(),
                BuildManualTC08(),
            };
        }

        ManualTC BuildManualTC01()
        {
            return new ManualTC("TC01 基础开关流程", "TC01", new Step[]
            {
                new Step("清理：关闭 TestPanel（若打开）",
                    "IsOpened=false",
                    () => { if (UIManager.S.IsOpened(TestUIID.TestPanel)) UIManager.S.ClosePanel(TestUIID.TestPanel); },
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("OpenPanel(TestPanel)",
                    "IsOpened=true, panelID≠-1",
                    () => { m_TmpPanelID1 = UIManager.S.OpenPanel(TestUIID.TestPanel); },
                    () => m_TmpPanelID1 != -1 && UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("等待 Prefab 异步加载完成",
                    "FindPanel<TestPanel>≠null",
                    null,
                    () => UIManager.S.FindPanel<TestPanel>() != null,
                    waitLoad: true),

                new Step("ClosePanel(TestPanel)",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),
            });
        }

        ManualTC BuildManualTC02()
        {
            int id2 = -1;
            return new ManualTC("TC02 单例防重", "TC02", new Step[]
            {
                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => { if (UIManager.S.IsOpened(TestUIID.TestPanel)) UIManager.S.ClosePanel(TestUIID.TestPanel); },
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("第一次 OpenPanel(2)",
                    "panelID1≠-1",
                    () => { m_TmpPanelID1 = UIManager.S.OpenPanel(TestUIID.TestPanel); },
                    () => m_TmpPanelID1 != -1),

                new Step("第二次 OpenPanel(2)（单例，应返回相同 panelID）",
                    "panelID2 == panelID1",
                    () => { id2 = UIManager.S.OpenPanel(TestUIID.TestPanel); },
                    () => id2 == m_TmpPanelID1),

                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),
            });
        }

        ManualTC BuildManualTC03()
        {
            return new ManualTC("TC03 缓存复用", "TC03", new Step[]
            {
                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => { if (UIManager.S.IsOpened(TestUIID.TestPanel)) UIManager.S.ClosePanel(TestUIID.TestPanel); },
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("第一次 OpenPanel(2)",
                    "IsOpened=true",
                    () => UIManager.S.OpenPanel(TestUIID.TestPanel),
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("等待加载，记录 GameObject 引用",
                    "panel1≠null，go1 已记录",
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestPanel>();
                        if (p != null) m_TmpGO1 = p.gameObject;
                    },
                    () => { var p = UIManager.S.FindPanel<TestPanel>(); return p != null; },
                    waitLoad: true),

                new Step("ClosePanel(2)（面板进入缓存，不销毁）",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("第二次 OpenPanel(2)（应从缓存复用）",
                    "IsOpened=true",
                    () => UIManager.S.OpenPanel(TestUIID.TestPanel),
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("等待加载，验证 GameObject 实例相同",
                    "panel2.gameObject == go1",
                    null,
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestPanel>();
                        return p != null && ReferenceEquals(m_TmpGO1, p.gameObject);
                    },
                    waitLoad: true),

                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),
            });
        }

        ManualTC BuildManualTC04()
        {
            return new ManualTC("TC04 OnOpen 计数累加", "TC04", new Step[]
            {
                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => { if (UIManager.S.IsOpened(TestUIID.TestPanel)) UIManager.S.ClosePanel(TestUIID.TestPanel); },
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("第一次 OpenPanel(2)",
                    "IsOpened=true",
                    () => UIManager.S.OpenPanel(TestUIID.TestPanel),
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("等待加载，记录 openCount",
                    "openCount≥1，m_TmpCountA 已记录",
                    () => { var p = UIManager.S.FindPanel<TestPanel>(); if (p != null) m_TmpCountA = p.openCount; },
                    () => { var p = UIManager.S.FindPanel<TestPanel>(); return p != null && p.openCount >= 1; },
                    waitLoad: true),

                new Step("ClosePanel(2)",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("第二次 OpenPanel(2)",
                    "IsOpened=true",
                    () => UIManager.S.OpenPanel(TestUIID.TestPanel),
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("等待加载，验证 openCount == countA+1",
                    "openCount == m_TmpCountA+1",
                    null,
                    () => { var p = UIManager.S.FindPanel<TestPanel>(); return p != null && p.openCount == m_TmpCountA + 1; },
                    waitLoad: true),

                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),
            });
        }

        ManualTC BuildManualTC05()
        {
            return new ManualTC("TC05 DoClose 全关", "TC05", new Step[]
            {
                new Step("OpenPanel(2)",
                    "IsOpened=true",
                    () => UIManager.S.OpenPanel(TestUIID.TestPanel),
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("UIManager.DoClose() + DoInit()",
                    "IsOpened=false（全部面板已关闭）",
                    () => { UIManager.S.DoClose(); UIManager.S.DoInit(); },
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),
            });
        }

        ManualTC BuildManualTC06()
        {
            return new ManualTC("TC06 查询 API 准确性", "TC06", new Step[]
            {
                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => { if (UIManager.S.IsOpened(TestUIID.TestPanel)) UIManager.S.ClosePanel(TestUIID.TestPanel); },
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("验证打开前：IsOpened / IsPanelVisible / IsPanelActive 均为 false",
                    "三个查询均=false",
                    null,
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)
                       && !UIManager.S.IsPanelVisible(TestUIID.TestPanel)
                       && !UIManager.S.IsPanelActive(TestUIID.TestPanel)),

                new Step("OpenPanel(2)",
                    "IsOpened=true",
                    () => UIManager.S.OpenPanel(TestUIID.TestPanel),
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("等待加载，验证 IsOpened / IsPanelVisible / IsPanelActive 均为 true",
                    "三个查询均=true",
                    null,
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)
                       && UIManager.S.IsPanelVisible(TestUIID.TestPanel)
                       && UIManager.S.IsPanelActive(TestUIID.TestPanel),
                    waitLoad: true),

                new Step("SetPanelVisible(false)，验证 IsPanelVisible=false 但 IsOpened=true",
                    "IsPanelVisible=false, IsOpened=true",
                    () => UIManager.S.SetPanelVisible(TestUIID.TestPanel, false),
                    () => !UIManager.S.IsPanelVisible(TestUIID.TestPanel)
                       &&  UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("SetPanelVisible(true) 恢复，清理关闭",
                    "IsPanelVisible=true, 最终 IsOpened=false",
                    () => { UIManager.S.SetPanelVisible(TestUIID.TestPanel, true); UIManager.S.ClosePanel(TestUIID.TestPanel); },
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),
            });
        }

        ManualTC BuildManualTC07()
        {
            return new ManualTC("TC07 子页面 Attach/Detach", "TC07", new Step[]
            {
                new Step("清理：确保 TestPanel 已打开",
                    "IsOpened(TestPanel)=true",
                    () => { if (!UIManager.S.IsOpened(TestUIID.TestPanel)) UIManager.S.OpenPanel(TestUIID.TestPanel); },
                    () => UIManager.S.IsOpened(TestUIID.TestPanel)),

                new Step("等待 TestPanel 加载完成",
                    "FindPanel<TestPanel>≠null",
                    null,
                    () => UIManager.S.FindPanel<TestPanel>() != null,
                    waitLoad: true),

                new Step("AttachPage(TestPageA)",
                    "IsPageOpen(TestPanel, TestPageA)=true",
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestPanel>();
                        p?.OpenPageA("manual step");
                    },
                    () => UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA)),

                new Step("等待 TestPageA 加载完成",
                    "IsPageOpen=true（Prefab 就绪）",
                    null,
                    () => UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA),
                    waitLoad: true),

                new Step("DettachPage(TestPageA)",
                    "IsPageOpen(TestPanel, TestPageA)=false",
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestPanel>();
                        p?.ClosePageA();
                    },
                    () => !UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA)),

                new Step("再次 Attach（验证缓存复用）",
                    "IsPageOpen=true（缓存复用）",
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestPanel>();
                        p?.OpenPageA("cached");
                    },
                    () => UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA)),

                new Step("清理：关闭 TestPanel",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestPanel)),
            });
        }

        ManualTC BuildManualTC08()
        {
            return new ManualTC("TC08 背包 ScrollView", "TC08", new Step[]
            {
                new Step("OpenPanel(TestBagPanel, 50 格子)",
                    "IsOpened=true",
                    () => UIManager.S.OpenPanel(TestUIID.TestBagPanel, 50),
                    () => UIManager.S.IsOpened(TestUIID.TestBagPanel)),

                new Step("等待背包 Prefab 加载完成",
                    "FindPanel<TestBagPanel>≠null",
                    null,
                    () => UIManager.S.FindPanel<TestBagPanel>() != null,
                    waitLoad: true),

                new Step("验证格子数量 = 50",
                    "itemCount=50",
                    null,
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestBagPanel>();
                        return p != null && p.itemCount == 50;
                    }),

                new Step("RefreshItems(10) — 减少格子数量",
                    "itemCount=10",
                    () => UIManager.S.FindPanel<TestBagPanel>()?.RefreshItems(10),
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestBagPanel>();
                        return p != null && p.itemCount == 10;
                    }),

                new Step("RefreshItems(100) — 增加格子数量（需滚动查看）",
                    "itemCount=100",
                    () => UIManager.S.FindPanel<TestBagPanel>()?.RefreshItems(100),
                    () =>
                    {
                        var p = UIManager.S.FindPanel<TestBagPanel>();
                        return p != null && p.itemCount == 100;
                    }),

                new Step("ClosePanel(TestBagPanel) — 格子随面板销毁",
                    "IsOpened=false",
                    () => UIManager.S.ClosePanel(TestUIID.TestBagPanel),
                    () => !UIManager.S.IsOpened(TestUIID.TestBagPanel)),
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // ═══  自动模式（F2 / uitest）  ══════════════════════════════
        // ═══════════════════════════════════════════════════════════════

        IEnumerator RunAllTests()
        {
            m_IsRunning   = true;
            m_TotalTests  = 0;
            m_PassedTests = 0;

            Print("╔══════════════════════════════╗");
            Print("║  UIFlowTest  开始             ║");
            Print("╚══════════════════════════════╝");

            yield return TC01_BasicOpenClose();
            yield return TC02_Singleton();
            yield return TC03_CacheReuse();
            yield return TC04_OpenCount();
            yield return TC05_DoCloseAll();
            yield return TC06_QueryAPIs();
            yield return TC07_PageAttachDetach();
            yield return TC08_BagScrollView();

            Print("──────────────────────────────");
            Print(string.Format("结果：{0} / {1} 通过", m_PassedTests, m_TotalTests));
            Print("══════════════════════════════");

            m_IsRunning = false;
        }

        IEnumerator TC01_BasicOpenClose()
        {
            bool passed = true;
            Print("── TC01 基础开关流程");
            yield return EnsureClosed();

            int panelID = UIManager.S.OpenPanel(TestUIID.TestPanel);
            Check(panelID != -1, "OpenPanel 返回有效 panelID", ref passed);
            Check(UIManager.S.IsOpened(TestUIID.TestPanel), "OpenPanel 后 IsOpened=true", ref passed);

            yield return WaitForPanel<TestPanel>(k_LoadTimeout);
            Check(UIManager.S.FindPanel<TestPanel>() != null, "加载完成后 FindPanel≠null", ref passed);

            UIManager.S.ClosePanel(TestUIID.TestPanel);
            yield return null;
            Check(!UIManager.S.IsOpened(TestUIID.TestPanel), "ClosePanel 后 IsOpened=false", ref passed);

            ReportTC("TC01_基础开关", passed);
        }

        IEnumerator TC02_Singleton()
        {
            bool passed = true;
            Print("── TC02 单例防重");
            yield return EnsureClosed();

            int id1 = UIManager.S.OpenPanel(TestUIID.TestPanel);
            int id2 = UIManager.S.OpenPanel(TestUIID.TestPanel);
            Check(id1 != -1, "首次 OpenPanel 返回有效 ID", ref passed);
            Check(id1 == id2, string.Format("二次 OpenPanel 返回相同 panelID（{0}=={1}）", id1, id2), ref passed);

            yield return EnsureClosed();
            ReportTC("TC02_单例防重", passed);
        }

        IEnumerator TC03_CacheReuse()
        {
            bool passed = true;
            Print("── TC03 缓存复用");
            yield return EnsureClosed();

            UIManager.S.OpenPanel(TestUIID.TestPanel);
            yield return WaitForPanel<TestPanel>(k_LoadTimeout);

            var p1 = UIManager.S.FindPanel<TestPanel>();
            Check(p1 != null, "第一次打开后 panel1≠null", ref passed);
            if (p1 != null)
            {
                var go1 = p1.gameObject;
                UIManager.S.ClosePanel(TestUIID.TestPanel);
                yield return null;

                UIManager.S.OpenPanel(TestUIID.TestPanel);
                yield return WaitForPanel<TestPanel>(k_LoadTimeout);

                var p2 = UIManager.S.FindPanel<TestPanel>();
                Check(p2 != null, "第二次打开后 panel2≠null", ref passed);
                if (p2 != null)
                    Check(ReferenceEquals(go1, p2.gameObject), "二次打开复用了同一个 GameObject 实例", ref passed);
            }

            yield return EnsureClosed();
            ReportTC("TC03_缓存复用", passed);
        }

        IEnumerator TC04_OpenCount()
        {
            bool passed = true;
            Print("── TC04 OnOpen 计数累加");
            yield return EnsureClosed();

            UIManager.S.OpenPanel(TestUIID.TestPanel);
            yield return WaitForPanel<TestPanel>(k_LoadTimeout);

            var p = UIManager.S.FindPanel<TestPanel>();
            if (p == null) { Check(false, "加载超时", ref passed); ReportTC("TC04_OpenCount累加", passed); yield break; }

            int countA = p.openCount;
            Check(countA >= 1, string.Format("首次打开后 openCount={0}>=1", countA), ref passed);

            UIManager.S.ClosePanel(TestUIID.TestPanel);
            yield return null;
            UIManager.S.OpenPanel(TestUIID.TestPanel);
            yield return WaitForPanel<TestPanel>(k_LoadTimeout);

            var p2 = UIManager.S.FindPanel<TestPanel>();
            if (p2 != null)
                Check(p2.openCount == countA + 1, string.Format("二次打开 openCount={0}（期望 {1}）", p2.openCount, countA + 1), ref passed);

            yield return EnsureClosed();
            ReportTC("TC04_OpenCount累加", passed);
        }

        IEnumerator TC05_DoCloseAll()
        {
            bool passed = true;
            Print("── TC05 DoClose 全关");

            UIManager.S.OpenPanel(TestUIID.TestPanel);
            yield return null;
            Check(UIManager.S.IsOpened(TestUIID.TestPanel), "DoClose 前面板已打开", ref passed);

            UIManager.S.DoClose();
            UIManager.S.DoInit();
            yield return null;
            Check(!UIManager.S.IsOpened(TestUIID.TestPanel), "DoClose 后 IsOpened=false", ref passed);

            ReportTC("TC05_DoClose全关", passed);
        }

        IEnumerator TC06_QueryAPIs()
        {
            bool passed = true;
            Print("── TC06 查询 API 准确性");
            yield return EnsureClosed();

            Check(!UIManager.S.IsOpened(TestUIID.TestPanel),      "打开前 IsOpened=false",      ref passed);
            Check(!UIManager.S.IsPanelVisible(TestUIID.TestPanel), "打开前 IsPanelVisible=false", ref passed);
            Check(!UIManager.S.IsPanelActive(TestUIID.TestPanel),  "打开前 IsPanelActive=false",  ref passed);

            UIManager.S.OpenPanel(TestUIID.TestPanel);
            yield return WaitForPanel<TestPanel>(k_LoadTimeout);

            Check(UIManager.S.IsOpened(TestUIID.TestPanel),       "打开后 IsOpened=true",      ref passed);
            Check(UIManager.S.IsPanelVisible(TestUIID.TestPanel),  "打开后 IsPanelVisible=true", ref passed);
            Check(UIManager.S.IsPanelActive(TestUIID.TestPanel),   "打开后 IsPanelActive=true",  ref passed);

            UIManager.S.SetPanelVisible(TestUIID.TestPanel, false);
            yield return null;
            Check(!UIManager.S.IsPanelVisible(TestUIID.TestPanel), "SetVisible(false) 后 IsPanelVisible=false", ref passed);
            Check(UIManager.S.IsOpened(TestUIID.TestPanel),        "SetVisible(false) 不影响 IsOpened=true",    ref passed);

            UIManager.S.SetPanelVisible(TestUIID.TestPanel, true);
            yield return EnsureClosed();
            ReportTC("TC06_查询API", passed);
        }

        IEnumerator TC07_PageAttachDetach()
        {
            bool passed = true;
            Print("── TC07 子页面 Attach/Detach");

            // 确保 TestPanel 已打开并加载完成
            if (!UIManager.S.IsOpened(TestUIID.TestPanel))
                UIManager.S.OpenPanel(TestUIID.TestPanel);
            yield return WaitForPanel<TestPanel>(k_LoadTimeout);

            var testPanel = UIManager.S.FindPanel<TestPanel>();
            Check(testPanel != null, "TestPanel 加载完成", ref passed);
            if (testPanel == null) { ReportTC("TC07_子页面", false); yield break; }

            // 确保 PageA 未挂载
            if (UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA))
                testPanel.ClosePageA();
            yield return null;
            Check(!UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA),
                "Attach 前 IsPageOpen=false", ref passed);

            // AttachPage
            testPanel.OpenPageA("auto-tc07");
            yield return WaitForPage(k_LoadTimeout);
            Check(UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA),
                "Attach 后 IsPageOpen=true", ref passed);

            // DettachPage
            testPanel.ClosePageA();
            yield return null;
            Check(!UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA),
                "Detach 后 IsPageOpen=false", ref passed);

            // 再次 Attach，验证缓存复用
            testPanel.OpenPageA("cached");
            yield return WaitForPage(k_LoadTimeout);
            Check(UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA),
                "缓存复用后 IsPageOpen=true", ref passed);

            // 清理
            yield return EnsureClosed();
            ReportTC("TC07_子页面", passed);
        }

        IEnumerator TC08_BagScrollView()
        {
            bool passed = true;
            Print("── TC08 背包 ScrollView");

            if (UIManager.S.IsOpened(TestUIID.TestBagPanel))
                UIManager.S.ClosePanel(TestUIID.TestBagPanel);
            yield return null;

            UIManager.S.OpenPanel(TestUIID.TestBagPanel, 50);
            float elapsed = 0f;
            while (UIManager.S.FindPanel<TestBagPanel>() == null && elapsed < k_LoadTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            var bag = UIManager.S.FindPanel<TestBagPanel>();
            Check(bag != null, "TestBagPanel 加载完成", ref passed);
            if (bag == null) { ReportTC("TC08_背包ScrollView", false); yield break; }

            Check(bag.itemCount == 50,
                string.Format("生成 50 格子（实际 {0}）", bag.itemCount), ref passed);

            // 减少格子
            bag.RefreshItems(10);
            yield return null;
            Check(bag.itemCount == 10,
                string.Format("减少为 10 格子（实际 {0}）", bag.itemCount), ref passed);

            // 增加格子（验证大量 Item 不报错）
            bag.RefreshItems(100);
            yield return null;
            Check(bag.itemCount == 100,
                string.Format("增加为 100 格子（实际 {0}）", bag.itemCount), ref passed);

            // 关闭，格子随面板销毁
            UIManager.S.ClosePanel(TestUIID.TestBagPanel);
            yield return null;
            Check(!UIManager.S.IsOpened(TestUIID.TestBagPanel), "关闭后 IsOpened=false", ref passed);

            ReportTC("TC08_背包ScrollView", passed);
        }

        // ─── 辅助协程 ────────────────────────────────────────────────────

        IEnumerator EnsureClosed()
        {
            if (UIManager.S.IsOpened(TestUIID.TestPanel))
            {
                UIManager.S.ClosePanel(TestUIID.TestPanel);
                yield return null;
            }
        }

        IEnumerator WaitForPanel<T>(float timeout) where T : AbstractPanel
        {
            float elapsed = 0f;
            while (UIManager.S.FindPanel<T>() == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (UIManager.S.FindPanel<T>() == null)
                Print(string.Format("[超时] 等待 {0} 超过 {1}s", typeof(T).Name, timeout));
        }

        /// <summary>等待 TestPageA 挂载完成（IsPageOpen 变为 true），超时打印警告。</summary>
        IEnumerator WaitForPage(float timeout)
        {
            float elapsed = 0f;
            while (!UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (!UIManager.S.IsPageOpen(TestUIID.TestPanel, TestUIID.TestPageA))
                Print(string.Format("[超时] 等待 TestPageA 超过 {0}s", timeout));
        }

        // ─── 断言与输出 ──────────────────────────────────────────────────

        void Check(bool condition, string message, ref bool passed)
        {
            if (!condition)
            {
                passed = false;
                Print("  ✗ " + message);
                Debug.LogWarning("[UIFlowTest] FAIL: " + message);
            }
        }

        void ReportTC(string name, bool passed)
        {
            m_TotalTests++;
            if (passed) { m_PassedTests++; Print("  [PASS] " + name); }
            else        { Print("  [FAIL] " + name); }
        }

        void Print(string msg)
        {
            Debug.Log("[UIFlowTest] " + msg);
            if (m_GMBox != null) m_GMBox.AppendOutput(msg);
        }

        void GUILog(string msg)
        {
            m_GuiLog.Add(msg);
            if (m_GuiLog.Count > 60)
                m_GuiLog.RemoveAt(0);
        }

        // ─── GM 指令 ─────────────────────────────────────────────────────

        void OnCmdUITest(string[] args)
        {
            if (m_IsRunning) { Print("[uitest] 测试运行中…"); return; }
            StartCoroutine(RunAllTests());
        }

        // ─── IMGUI 样式工具 ───────────────────────────────────────────────

        GUIStyle RichStyle()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.richText  = true;
            s.fontSize  = 13;
            s.wordWrap  = false;
            return s;
        }

        GUIStyle BoldStyle()
        {
            var s = RichStyle();
            s.fontStyle = FontStyle.Bold;
            return s;
        }

        void DrawSeparator()
        {
            var r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            GUI.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(3);
        }

        // ═══════════════════════════════════════════════════════════════
        // 内部数据结构
        // ═══════════════════════════════════════════════════════════════

        /// <summary>单个步骤的执行状态。</summary>
        enum StepStatus { Pending, Waiting, Pass, Fail }

        /// <summary>手动测试中的单个步骤定义。</summary>
        class Step
        {
            /// <summary>步骤描述，显示在 GUI 列表。</summary>
            public readonly string desc;
            /// <summary>期望结果描述，显示在结果列。</summary>
            public readonly string expectDesc;
            /// <summary>步骤执行动作，可为 null（纯检查步骤）。</summary>
            public readonly System.Action action;
            /// <summary>断言检查，返回 true 表示通过；null 表示无需检查。</summary>
            public readonly System.Func<bool> check;
            /// <summary>是否需要异步轮询等待（面板 Prefab 加载）。</summary>
            public readonly bool waitLoad;
            /// <summary>当前执行状态。</summary>
            public StepStatus status = StepStatus.Pending;

            public Step(string desc, string expectDesc = "",
                        System.Action action = null, System.Func<bool> check = null,
                        bool waitLoad = false)
            {
                this.desc       = desc;
                this.expectDesc = expectDesc;
                this.action     = action;
                this.check      = check;
                this.waitLoad   = waitLoad;
            }
        }

        /// <summary>手动测试用例，包含名称和步骤数组。</summary>
        class ManualTC
        {
            /// <summary>完整名称（显示在步骤区域标题）。</summary>
            public readonly string name;
            /// <summary>短名称（显示在 TC 选择按钮上）。</summary>
            public readonly string shortName;
            /// <summary>步骤数组。</summary>
            public readonly Step[] steps;

            public ManualTC(string name, string shortName, Step[] steps)
            {
                this.name      = name;
                this.shortName = shortName;
                this.steps     = steps;
            }

            /// <summary>重置所有步骤为 Pending。</summary>
            public void Reset()
            {
                foreach (var s in steps)
                    s.status = StepStatus.Pending;
            }

            /// <summary>返回第一个 Pending/Waiting 步骤的索引，全部完成时返回 -1。</summary>
            public int ActiveStepIndex
            {
                get
                {
                    for (int i = 0; i < steps.Length; i++)
                        if (steps[i].status == StepStatus.Pending || steps[i].status == StepStatus.Waiting)
                            return i;
                    return -1;
                }
            }

            /// <summary>已执行完成（Pass 或 Fail）的步骤数。</summary>
            public int DoneCount
            {
                get
                {
                    int c = 0;
                    foreach (var s in steps)
                        if (s.status == StepStatus.Pass || s.status == StepStatus.Fail) c++;
                    return c;
                }
            }

            /// <summary>所有步骤都 Pass 时为 true。</summary>
            public bool AllPassed
            {
                get
                {
                    foreach (var s in steps)
                        if (s.status != StepStatus.Pass) return false;
                    return true;
                }
            }
        }
    }
}
