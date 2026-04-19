using System.IO;
using ST.Core.Test;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.Editor.Test
{
    /// <summary>
    /// 编辑器工具类：一键生成全部测试用 Prefab。
    /// <para>
    /// 菜单：<b>ST.Core / Test / Create All Test Prefabs</b>（全部生成）
    /// </para>
    /// <para>生成的三个 Prefab：</para>
    /// <list type="bullet">
    ///   <item><c>ui_panel_gm_box.prefab</c>     — <see cref="GMBoxPanel"/>（Top 层，桥接 IMGUI TestGMBoxPanel）</item>
    ///   <item><c>ui_panel_test.prefab</c>        — <see cref="TestPanel"/>（Auto 层，基础开关测试）</item>
    ///   <item><c>ui_panel_test_modal.prefab</c>  — <see cref="TestModalPanel"/>（Auto 层，HideMask 遮蔽测试）</item>
    /// </list>
    /// </summary>
    public static class CreateUITestPrefabEditor
    {
        // ─── 路径常量 ────────────────────────────────────────────────────

        /// <summary>所有测试 Prefab 的输出目录。</summary>
        const string k_PrefabDir = "Assets/Package/ui/uiprefab";

        /// <summary>设计分辨率宽度。</summary>
        const float k_DesignW = 1920f;

        /// <summary>设计分辨率高度。</summary>
        const float k_DesignH = 1080f;

        // ─── 菜单入口 ────────────────────────────────────────────────────

        /// <summary>一键生成全部三个测试 Prefab。</summary>
        [MenuItem("ST.Core/Test/Create All Test Prefabs")]
        static void CreateAllPrefabs()
        {
            EnsureDir();

            int ok = 0;
            if (SavePrefab("ui_panel_gm_box",        BuildGMBoxPrefab()))        ok++;
            if (SavePrefab("ui_panel_test",           BuildTestPrefab()))         ok++;
            if (SavePrefab("ui_panel_test_modal",     BuildTestModalPrefab()))    ok++;
            if (SavePrefab("ui_page_test_a",          BuildPageAPrefab()))        ok++;
            if (SavePrefab("ui_panel_test_bag",       BuildBagPrefab()))          ok++;

            AssetDatabase.Refresh();

            string msg = string.Format("完成：{0}/5 个 Prefab 已保存到\n{1}", ok, k_PrefabDir);
            Debug.Log("[ST.Core] " + msg);
            EditorUtility.DisplayDialog("创建结果", msg, "OK");
        }

        /// <summary>单独生成 <c>ui_panel_gm_box.prefab</c>。</summary>
        [MenuItem("ST.Core/Test/Create ui_panel_gm_box Prefab")]
        static void CreateGMBox()    => CreateSingle("ui_panel_gm_box",       BuildGMBoxPrefab());

        /// <summary>单独生成 <c>ui_panel_test.prefab</c>。</summary>
        [MenuItem("ST.Core/Test/Create ui_panel_test Prefab")]
        static void CreateTest()     => CreateSingle("ui_panel_test",          BuildTestPrefab());

        /// <summary>单独生成 <c>ui_panel_test_modal.prefab</c>。</summary>
        [MenuItem("ST.Core/Test/Create ui_panel_test_modal Prefab")]
        static void CreateModal()    => CreateSingle("ui_panel_test_modal",    BuildTestModalPrefab());

        /// <summary>单独生成 <c>ui_page_test_a.prefab</c>。</summary>
        [MenuItem("ST.Core/Test/Create ui_page_test_a Prefab")]
        static void CreatePageA()    => CreateSingle("ui_page_test_a",         BuildPageAPrefab());

        /// <summary>单独生成 <c>ui_panel_test_bag.prefab</c>。</summary>
        [MenuItem("ST.Core/Test/Create ui_panel_test_bag Prefab")]
        static void CreateBag()      => CreateSingle("ui_panel_test_bag",      BuildBagPrefab());

        // ═══════════════════════════════════════════
        // ui_panel_gm_box
        // ═══════════════════════════════════════════

        /// <summary>
        /// 构建 GM Box Prefab 节点树。
        /// <code>
        /// ui_panel_gm_box  (Canvas + GMBoxPanel)
        /// ├─ Background    (全屏半透明蒙版)
        /// ├─ TitleBar      (顶部标题条)
        /// │  └─ TitleText  (Text "GM Box")
        /// └─ CloseBtn      (右上角关闭按钮)
        ///    └─ Label       (Text "×")
        /// </code>
        /// </summary>
        static GameObject BuildGMBoxPrefab()
        {
            var root   = MakeRoot("ui_panel_gm_box", new Color(0.05f, 0.05f, 0.10f, 0.88f));
            var panel  = root.AddComponent<GMBoxPanel>();

            // TitleBar
            var (_, titleText) = AddTitleBar(root.transform, "GM Box", new Color(0.08f, 0.08f, 0.18f, 1f));

            // CloseBtn
            var closeBtn = AddCloseBtn(root.transform);

            // 绑定引用
            panel.m_TitleText   = titleText;
            panel.m_CloseButton = closeBtn;

            SetLayerRecursive(root);
            return root;
        }

        // ═══════════════════════════════════════════
        // ui_panel_test
        // ═══════════════════════════════════════════

        /// <summary>
        /// 构建通用测试面板 Prefab 节点树。
        /// <code>
        /// ui_panel_test  (Canvas + TestPanel)
        /// ├─ Background  (半透明蒙版)
        /// ├─ TitleBar    (顶部标题条)
        /// │  └─ TitleText (Text "TestPanel")
        /// ├─ CloseBtn    (右上角关闭按钮)
        /// └─ Content     (内容容器)
        /// </code>
        /// </summary>
        static GameObject BuildTestPrefab()
        {
            var root   = MakeRoot("ui_panel_test", new Color(0f, 0f, 0f, 0.55f));
            var panel  = root.AddComponent<TestPanel>();

            var (_, titleText) = AddTitleBar(root.transform, "TestPanel", new Color(0.12f, 0.12f, 0.20f, 1f));
            var closeBtn       = AddCloseBtn(root.transform);
            AddContent(root.transform);

            panel.m_TitleText   = titleText;
            panel.m_CloseButton = closeBtn;

            SetLayerRecursive(root);
            return root;
        }

        // ═══════════════════════════════════════════
        // ui_panel_test_modal
        // ═══════════════════════════════════════════

        /// <summary>
        /// 构建模态测试面板 Prefab 节点树（带 HideMask 说明文字）。
        /// <code>
        /// ui_panel_test_modal  (Canvas + TestModalPanel)
        /// ├─ Background        (深色半透明蒙版)
        /// ├─ TitleBar          (顶部标题条，深红色)
        /// │  └─ TitleText      (Text "模态面板")
        /// ├─ InfoText          (居中说明文字，描述 HideMask 效果)
        /// ├─ CloseBtn          (右上角关闭按钮)
        /// └─ Content           (内容容器)
        /// </code>
        /// </summary>
        static GameObject BuildTestModalPrefab()
        {
            var root   = MakeRoot("ui_panel_test_modal", new Color(0f, 0f, 0f, 0.72f));
            var panel  = root.AddComponent<TestModalPanel>();

            var (_, titleText) = AddTitleBar(root.transform, "模态面板  （HideMask）",
                                             new Color(0.35f, 0.05f, 0.05f, 1f));

            // 居中信息文字
            var infoGo   = CreateChild("InfoText", root.transform);
            var infoText = infoGo.AddComponent<Text>();
            infoText.text      = "HideMask = HideAndUnInteractive\n下层面板已隐藏且不可交互\n关闭本面板后恢复";
            infoText.fontSize  = 32;
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.color     = new Color(1f, 0.85f, 0.5f, 1f);
            var infoRect = infoGo.GetComponent<RectTransform>();
            infoRect.anchorMin        = new Vector2(0.1f, 0.35f);
            infoRect.anchorMax        = new Vector2(0.9f, 0.65f);
            infoRect.offsetMin        = Vector2.zero;
            infoRect.offsetMax        = Vector2.zero;

            var closeBtn = AddCloseBtn(root.transform);
            AddContent(root.transform);

            panel.m_TitleText   = titleText;
            panel.m_InfoText    = infoText;
            panel.m_CloseButton = closeBtn;

            SetLayerRecursive(root);
            return root;
        }

        // ═══════════════════════════════════════════
        // ui_page_test_a  （子页面，无 Canvas）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 构建测试子页面 A 的 Prefab 节点树（无 Canvas，由父面板 Canvas 渲染）。
        /// </summary>
        static GameObject BuildPageAPrefab()
        {
            var root = new GameObject("ui_page_test_a");

            // Page 根节点仅含 RectTransform，无 Canvas。
            var rt = root.AddComponent<RectTransform>();
            rt.localScale       = Vector3.one;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(k_DesignW, k_DesignH);
            rt.anchoredPosition = Vector2.zero;

            var page = root.AddComponent<TestPageA>();

            // Background
            var bg      = CreateChild("Background", root.transform);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0.05f, 0.15f, 0.82f);
            StretchFull(bg.GetComponent<RectTransform>());

            // TitleBar
            var (_, titleText) = AddTitleBar(root.transform, "TestPageA（子页面）",
                                             new Color(0.05f, 0.20f, 0.40f, 1f));

            // InfoText
            var infoGo   = CreateChild("InfoText", root.transform);
            var infoText = infoGo.AddComponent<Text>();
            infoText.text      = "这是 TestPanel 的子页面 A\n通过 panelActive.AttachPage 动态挂载\n点击右上角 × 卸载";
            infoText.fontSize  = 30;
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.color     = new Color(0.8f, 0.95f, 1f, 1f);
            var infoRect = infoGo.GetComponent<RectTransform>();
            infoRect.anchorMin        = new Vector2(0.1f, 0.35f);
            infoRect.anchorMax        = new Vector2(0.9f, 0.65f);
            infoRect.offsetMin        = Vector2.zero;
            infoRect.offsetMax        = Vector2.zero;

            // CloseBtn
            var closeBtn = AddCloseBtn(root.transform);

            // 绑定引用
            page.m_TitleText   = titleText;
            page.m_InfoText    = infoText;
            page.m_CloseButton = closeBtn;

            SetLayerRecursive(root);
            return root;
        }

        // ═══════════════════════════════════════════
        // ui_panel_test_bag  （背包 ScrollView 面板）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 构建背包测试面板 Prefab 节点树。
        /// <code>
        /// ui_panel_test_bag  (Canvas + TestBagPanel)
        /// ├─ Background      (全屏蒙版)
        /// ├─ TitleBar        (顶部标题条)
        /// │  └─ TitleText
        /// ├─ CloseBtn
        /// │  └─ Label
        /// ├─ ScrollView      (ScrollRect + Image + Mask)
        /// │  └─ Viewport     (Mask)
        /// │     └─ Content   (GridLayoutGroup + ContentSizeFitter)
        /// └─ ItemTemplate    (TestBagItem，SetActive=false)
        ///    ├─ IconImage
        ///    ├─ NameText
        ///    └─ CountText
        /// </code>
        /// </summary>
        static GameObject BuildBagPrefab()
        {
            var root  = MakeRoot("ui_panel_test_bag", new Color(0.08f, 0.06f, 0.04f, 0.93f));
            var panel = root.AddComponent<TestBagPanel>();

            var (_, titleText) = AddTitleBar(root.transform, "背包",
                                             new Color(0.22f, 0.15f, 0.06f, 1f));
            var closeBtn = AddCloseBtn(root.transform);

            // ScrollView（留出顶部标题栏 + 四边 padding）
            var (scrollView, content) = AddScrollView(root.transform);

            // ItemTemplate — 置于 root 下（不在 Content 内），运行时 Instantiate 到 Content
            var template = CreateBagItemTemplate(root.transform);
            template.SetActive(false);

            // 绑定引用
            panel.m_TitleText    = titleText;
            panel.m_CloseButton  = closeBtn;
            panel.m_ScrollRect   = scrollView.GetComponent<ScrollRect>();
            panel.m_Content      = content;
            panel.m_ItemTemplate = template;

            SetLayerRecursive(root);
            return root;
        }

        /// <summary>
        /// 创建 ScrollView 节点树（ScrollRect + Viewport + Content），
        /// Content 挂 <c>GridLayoutGroup</c> + <c>ContentSizeFitter</c>。
        /// </summary>
        /// <returns>(ScrollView 根节点 GameObject, Content RectTransform)</returns>
        static (GameObject sv, RectTransform content) AddScrollView(Transform parent)
        {
            // ── ScrollView 根节点 ──────────────────────────────────────
            var sv    = CreateChild("ScrollView", parent);
            var svImg = sv.AddComponent<Image>();
            svImg.color = new Color(0f, 0f, 0f, 0.25f);
            var svRect = sv.GetComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0f, 0f);
            svRect.anchorMax = new Vector2(1f, 1f);
            svRect.offsetMin = new Vector2(16f, 16f);
            svRect.offsetMax = new Vector2(-16f, -96f); // 上边留出 TitleBar

            // ── Viewport（带 Mask）────────────────────────────────────
            var vp    = CreateChild("Viewport", sv.transform);
            var vpImg = vp.AddComponent<Image>();
            vpImg.color           = new Color(0f, 0f, 0f, 0.01f);
            vpImg.raycastTarget   = true;
            var mask = vp.AddComponent<Mask>();
            mask.showMaskGraphic  = false;
            var vpRect = vp.GetComponent<RectTransform>();
            StretchFull(vpRect);

            // ── Content（格子父节点）─────────────────────────────────
            var contentGo   = CreateChild("Content", vp.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin        = new Vector2(0f, 1f);
            contentRect.anchorMax        = new Vector2(1f, 1f);
            contentRect.pivot            = new Vector2(0.5f, 1f);
            contentRect.sizeDelta        = new Vector2(0f, 0f);
            contentRect.anchoredPosition = Vector2.zero;

            var grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(160f, 180f);
            grid.spacing         = new Vector2(12f, 12f);
            grid.padding         = new RectOffset(24, 24, 24, 24);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment  = TextAnchor.UpperLeft;

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            // ── ScrollRect ────────────────────────────────────────────
            var scrollRect = sv.AddComponent<ScrollRect>();
            scrollRect.viewport          = vpRect;
            scrollRect.content           = contentRect;
            scrollRect.horizontal        = false;
            scrollRect.vertical          = true;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.movementType      = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity        = 0.1f;
            scrollRect.inertia           = true;
            scrollRect.decelerationRate  = 0.135f;

            return (sv, contentRect);
        }

        /// <summary>
        /// 创建格子模板节点（挂 <see cref="TestBagItem"/>），默认 SetActive(false) 由调用方负责。
        /// </summary>
        static GameObject CreateBagItemTemplate(Transform parent)
        {
            var go   = CreateChild("ItemTemplate", parent);
            var rt   = go.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            rt.sizeDelta  = new Vector2(160f, 180f);

            // 背景 + 按钮（整格可点击）
            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.28f, 0.22f, 0.14f, 1f);
            var btn = go.AddComponent<Button>();

            // 高亮颜色调整
            var colors       = btn.colors;
            colors.highlightedColor = new Color(0.45f, 0.36f, 0.22f, 1f);
            colors.pressedColor     = new Color(0.18f, 0.14f, 0.08f, 1f);
            btn.colors       = colors;

            var item = go.AddComponent<TestBagItem>();

            // ── 图标 ───────────────────────────────────────────────────
            var iconGo = CreateChild("IconImage", go.transform);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.55f, 0.38f, 0.18f, 1f);
            var iconRect  = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.08f, 0.28f);
            iconRect.anchorMax = new Vector2(0.92f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            // ── 物品名称 ──────────────────────────────────────────────
            var nameGo   = CreateChild("NameText", go.transform);
            var nameText = nameGo.AddComponent<Text>();
            nameText.text      = "物品名称";
            nameText.fontSize  = 18;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color     = Color.white;
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.28f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            // ── 数量角标 ──────────────────────────────────────────────
            var countGo   = CreateChild("CountText", go.transform);
            var countText = countGo.AddComponent<Text>();
            countText.text      = "×1";
            countText.fontSize  = 16;
            countText.alignment = TextAnchor.UpperRight;
            countText.color     = new Color(1f, 0.9f, 0.45f, 1f);
            var countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0.72f);
            countRect.anchorMax = new Vector2(1f,   1f);
            countRect.offsetMin = new Vector2(0f, -4f);
            countRect.offsetMax = new Vector2(-4f, 0f);

            // 绑定引用
            item.m_IconImage  = iconImg;
            item.m_NameText   = nameText;
            item.m_CountText  = countText;
            item.m_Button     = btn;

            return go;
        }

        // ─── 共用节点构建方法 ────────────────────────────────────────────

        /// <summary>
        /// 创建带 Canvas / GraphicRaycaster / CanvasGroup 的根节点，
        /// 并添加半透明全屏 Background。
        /// <para>
        /// <b>组件说明：</b>
        /// <list type="bullet">
        ///   <item><b>Canvas</b>：必须，用于控制 sortingOrder 实现层级排序。</item>
        ///   <item><b>CanvasScaler</b>：<b>不添加</b>。面板作为嵌套 Canvas 挂入 PanelRoot 后，
        ///     CanvasScaler(ScaleWithScreenSize) 会以父容器尺寸（初始为 0）计算缩放：
        ///     scale = 0 / referenceResolution = 0，导致面板不可见。
        ///     全局缩放由 UIRoot.RootCanvas 的 CanvasScaler 统一处理。</item>
        ///   <item><b>GraphicRaycaster</b>：必须，按钮等交互组件需要它响应输入事件。</item>
        ///   <item><b>CanvasGroup</b>：必须，用于 alpha / interactable / blocksRaycasts 控制。</item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>RectTransform 策略：</b>
        /// Prefab 根节点使用<b>中心锚点 + 设计分辨率固定尺寸</b>（<see cref="k_DesignW"/> × <see cref="k_DesignH"/>），
        /// 使 Edit 模式下拖入场景或在 Prefab 预览窗口中均能以 1920×1080 正确显示。
        /// 运行时 <see cref="ST.Core.UI.UIPanelActive"/> 会在实例化后强制覆盖为 stretch-fill，
        /// 确保面板铺满 PanelRoot 容器。
        /// </para>
        /// </summary>
        static GameObject MakeRoot(string name, Color bgColor)
        {
            var root = new GameObject(name);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            // 不添加 CanvasScaler，见 XML 注释说明。
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<CanvasGroup>();

            // 中心锚点 + 设计分辨率固定尺寸：
            //   - Edit 模式（根 Canvas）：Inspector 显示 Width=1920 / Height=1080 / Scale=1，可正常预览。
            //   - 运行时（嵌套 Canvas）：UIPanelActive.OnPrefabLoaded 会强制覆盖为 stretch-fill。
            // 注：Canvas 组件在 Editor 脚本中有时会把 localScale 初始化为 (0,0,0)，
            //     必须显式重置为 (1,1,1)，否则 Prefab 序列化后 Scale 仍为 0。
            var rt = root.GetComponent<RectTransform>();
            rt.localScale       = Vector3.one;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(k_DesignW, k_DesignH);
            rt.anchoredPosition = Vector2.zero;

            // Background
            var bg      = CreateChild("Background", root.transform);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = bgColor;
            StretchFull(bg.GetComponent<RectTransform>());

            // UICamera 默认只渲染 UI 层，整棵树必须设为 UI Layer 才能在 Game 视图显示。
            // 子节点在各 Build 方法末尾统一通过 SetLayerRecursive 覆盖，此处设置根节点本身。
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) root.layer = uiLayer;

            return root;
        }

        /// <summary>
        /// 在 <paramref name="parent"/> 下添加高度 80 的顶部标题条，返回 (titleBar, TitleText)。
        /// </summary>
        static (GameObject bar, Text text) AddTitleBar(Transform parent, string title, Color barColor)
        {
            var bar      = CreateChild("TitleBar", parent);
            var barImage = bar.AddComponent<Image>();
            barImage.color = barColor;
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin        = new Vector2(0f, 1f);
            barRect.anchorMax        = new Vector2(1f, 1f);
            barRect.pivot            = new Vector2(0.5f, 1f);
            barRect.sizeDelta        = new Vector2(0f, 80f);
            barRect.anchoredPosition = Vector2.zero;

            var txtGo = CreateChild("TitleText", bar.transform);
            var text  = txtGo.AddComponent<Text>();
            text.text      = title;
            text.fontSize  = 36;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color     = Color.white;
            StretchFull(txtGo.GetComponent<RectTransform>());

            return (bar, text);
        }

        /// <summary>在 <paramref name="parent"/> 右上角添加 80×80 关闭按钮，返回 Button 组件。</summary>
        static Button AddCloseBtn(Transform parent)
        {
            var go    = CreateChild("CloseBtn", parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.80f, 0.22f, 0.18f, 1f);
            var btn  = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(1f, 1f);
            rect.anchorMax        = new Vector2(1f, 1f);
            rect.pivot            = new Vector2(1f, 1f);
            rect.sizeDelta        = new Vector2(80f, 80f);
            rect.anchoredPosition = new Vector2(-4f, 0f);

            var labelGo   = CreateChild("Label", go.transform);
            var labelText = labelGo.AddComponent<Text>();
            labelText.text      = "×";
            labelText.fontSize  = 52;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color     = Color.white;
            StretchFull(labelGo.GetComponent<RectTransform>());

            return btn;
        }

        /// <summary>在 <paramref name="parent"/> 下添加铺满（留出 TitleBar）的内容容器。</summary>
        static void AddContent(Transform parent)
        {
            var content = CreateChild("Content", parent);
            var rect    = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(0f,   0f);
            rect.offsetMax = new Vector2(0f, -80f);
        }

        // ─── 工具方法 ────────────────────────────────────────────────────

        /// <summary>创建子 GameObject（含 RectTransform）并挂到 <paramref name="parent"/>。</summary>
        static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        /// <summary>将 RectTransform 拉伸为完全铺满父节点。</summary>
        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 递归地将 <paramref name="go"/> 及其所有子节点的 Layer 设置为 <c>UI</c>（Layer 5）。
        /// <para>
        /// UICamera 默认只渲染 UI 层，Prefab 若未设置正确 Layer，在 Game 视图中将不可见。
        /// </para>
        /// </summary>
        static void SetLayerRecursive(GameObject go)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                Debug.LogWarning("[ST.Core] 未找到名为 'UI' 的 Layer，请在 Project Settings > Tags and Layers 中添加。");
                return;
            }
            SetLayerRecursiveImpl(go.transform, uiLayer);
        }

        static void SetLayerRecursiveImpl(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t)
                SetLayerRecursiveImpl(child, layer);
        }

        /// <summary>确保输出目录存在。</summary>
        static void EnsureDir()
        {
            if (!Directory.Exists(k_PrefabDir))
                Directory.CreateDirectory(k_PrefabDir);
        }

        /// <summary>将 <paramref name="root"/> 保存为 Prefab，并立即 DestroyImmediate 根节点。</summary>
        /// <returns>保存成功返回 true。</returns>
        static bool SavePrefab(string filename, GameObject root)
        {
            string path = k_PrefabDir + "/" + filename + ".prefab";
            bool saved;
            PrefabUtility.SaveAsPrefabAsset(root, path, out saved);
            Object.DestroyImmediate(root);

            if (saved)
                Debug.Log("[ST.Core] 已生成：" + path);
            else
                Debug.LogError("[ST.Core] 生成失败：" + path);

            return saved;
        }

        /// <summary>单个 Prefab 的创建+弹窗入口。</summary>
        static void CreateSingle(string filename, GameObject root)
        {
            EnsureDir();
            bool ok = SavePrefab(filename, root);
            AssetDatabase.Refresh();
            string path = k_PrefabDir + "/" + filename + ".prefab";
            if (ok)
                EditorUtility.DisplayDialog("创建成功", filename + ".prefab 已保存到：\n" + path, "OK");
            else
                EditorUtility.DisplayDialog("创建失败", "请检查 Console 中的错误日志", "OK");
        }
    }
}
