using System.Collections.Generic;
using ST.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.Test
{
    /// <summary>
    /// 背包测试面板，演示 ScrollView + 模板克隆的 Item 动态创建流程。
    /// <para>
    /// <b>核心设计：</b>
    /// <list type="bullet">
    ///   <item><see cref="m_ItemTemplate"/> 放在 Prefab 根节点（不在 Content 内），默认 <c>SetActive(false)</c>。</item>
    ///   <item><see cref="RefreshItems"/> 从模板克隆指定数量的格子到 <see cref="m_Content"/> 下，
    ///         由 <c>GridLayoutGroup + ContentSizeFitter</c> 自动排版并撑高 Content。</item>
    ///   <item><see cref="ClearItems"/> 销毁所有克隆格子，在 <see cref="OnClose"/> 和重新刷新前调用。</item>
    /// </list>
    /// </para>
    /// </summary>
    public class TestBagPanel : AbstractPanel
    {
        // ─── 序列化字段（Inspector） ─────────────────────────────────────

        /// <summary>面板标题文字。</summary>
        public Text        m_TitleText;

        /// <summary>关闭按钮。</summary>
        public Button      m_CloseButton;

        /// <summary>ScrollRect 组件，绑定 ScrollView 根节点。</summary>
        public ScrollRect  m_ScrollRect;

        /// <summary>GridLayoutGroup 所在的 Content RectTransform，格子的父节点。</summary>
        public RectTransform m_Content;

        /// <summary>
        /// 格子模板 GameObject。
        /// <para>在 Prefab 中默认 <c>SetActive(false)</c>；<see cref="RefreshItems"/> 从此节点
        /// <c>Instantiate</c> 后挂入 <see cref="m_Content"/>，再 <c>SetActive(true)</c>。</para>
        /// </summary>
        public GameObject  m_ItemTemplate;

        // ─── 私有状态 ────────────────────────────────────────────────────

        /// <summary>当前列表中存活的格子列表。</summary>
        readonly List<TestBagItem> m_Items = new List<TestBagItem>(64);

        /// <summary>默认生成格子数量。</summary>
        const int k_DefaultCount = 50;

        // ─── 公共属性 ────────────────────────────────────────────────────

        /// <summary>当前已创建的格子数量（供自动化 TC 断言使用）。</summary>
        public int itemCount { get { return m_Items.Count; } }

        // ─── 生命周期 ────────────────────────────────────────────────────

        /// <summary>初始化关闭按钮事件；确保模板默认隐藏。</summary>
        protected override void OnCreate()
        {
            Debug.Log("[TestBagPanel] OnCreate");

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(CloseSelf);

            // 保证模板在运行时隐藏（Prefab 内已设 false，此处双保险）
            if (m_ItemTemplate != null)
                m_ItemTemplate.SetActive(false);
        }

        /// <summary>
        /// 每次打开时生成格子列表。
        /// <para><paramref name="args"/>[0] 若为 <c>int</c>，则以此为格子数量；否则使用默认值 <see cref="k_DefaultCount"/>。</para>
        /// </summary>
        protected override void OnOpen(object[] args)
        {
            int count = k_DefaultCount;
            if (args != null && args.Length > 0 && args[0] is int n)
                count = n;

            Debug.LogFormat("[TestBagPanel] OnOpen  count={0}", count);

            if (m_TitleText != null)
                m_TitleText.text = string.Format("背包  ({0} 个物品)", count);

            // 强制 Canvas 刷新布局，保证 Content.rect.width 已经是真实宽度
            Canvas.ForceUpdateCanvases();
            FitCellSizeToRow();
            RefreshItems(count);
        }

        /// <summary>关闭时清除所有格子，避免缓存复用时残留旧数据。</summary>
        protected override void OnClose()
        {
            Debug.Log("[TestBagPanel] OnClose");
            ClearItems();
        }

        /// <summary>销毁时取消按钮监听。</summary>
        protected override void OnDispose()
        {
            Debug.Log("[TestBagPanel] OnDispose");

            if (m_CloseButton != null)
                m_CloseButton.onClick.RemoveAllListeners();
        }

        // ─── 公共操作 ────────────────────────────────────────────────────

        /// <summary>
        /// 清除所有格子后，根据 <paramref name="count"/> 重新从模板克隆并初始化。
        /// <para>克隆完成后调用 <c>LayoutRebuilder.ForceRebuildLayoutImmediate</c>
        /// 立即刷新 GridLayoutGroup + ContentSizeFitter，确保 Content 高度同步更新。</para>
        /// </summary>
        /// <param name="count">要创建的格子数量。</param>
        public void RefreshItems(int count)
        {
            ClearItems();

            if (m_ItemTemplate == null || m_Content == null)
            {
                Debug.LogWarning("[TestBagPanel] m_ItemTemplate 或 m_Content 未赋值");
                return;
            }

            // 每次刷新都重算 cellSize，保证分辨率变化或首次加载时尺寸正确
            FitCellSizeToRow();

            for (int i = 0; i < count; i++)
            {
                var go   = Object.Instantiate(m_ItemTemplate, m_Content, false);
                go.SetActive(true);

                var item = go.GetComponent<TestBagItem>();
                if (item != null)
                {
                    var color = new Color(
                        UnityEngine.Random.value,
                        UnityEngine.Random.value,
                        UnityEngine.Random.value, 1f);

                    item.Init(i + 1,
                        string.Format("物品 {0:D3}", i + 1),
                        UnityEngine.Random.Range(1, 99),
                        color);

                    m_Items.Add(item);
                }
            }

            // 强制立即重算 GridLayout + ContentSizeFitter，使 Content 高度即时生效
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);

            Debug.LogFormat("[TestBagPanel] RefreshItems 完成，count={0}", m_Items.Count);
        }

        // ─── 私有方法 ────────────────────────────────────────────────────

        /// <summary>
        /// 根据 Content 的实际宽度动态计算 GridLayoutGroup.cellSize，
        /// 使每行恰好排满 constraintCount 列（一行铺满）。
        /// </summary>
        void FitCellSizeToRow()
        {
            if (m_Content == null) return;
            var grid = m_Content.GetComponent<GridLayoutGroup>();
            if (grid == null) return;

            // 优先用 Content 自身宽度；未布局时退回父 Viewport 宽度
            float w = m_Content.rect.width;
            if (w <= 0f)
            {
                var vp = m_Content.parent as RectTransform;
                if (vp != null) w = vp.rect.width;
            }
            if (w <= 0f) return;

            int   cols     = grid.constraintCount;
            float padH     = grid.padding.left + grid.padding.right;
            float spaceSum = grid.spacing.x * (cols - 1);
            float cellW    = Mathf.Floor((w - padH - spaceSum) / cols);
            float cellH    = Mathf.Floor(cellW * 1.15f);

            grid.cellSize = new Vector2(cellW, cellH);
            Debug.LogFormat("[TestBagPanel] FitCellSizeToRow  contentW={0:F0}  cell=({1}×{2})", w, cellW, cellH);
        }

        /// <summary>销毁所有克隆格子，清空列表。</summary>
        void ClearItems()
        {
            foreach (var item in m_Items)
            {
                if (item != null)
                    Object.Destroy(item.gameObject);
            }
            m_Items.Clear();
        }

        /// <summary>关闭按钮点击回调。</summary>
        void CloseSelf()
        {
            UIManager.S?.ClosePanel(uiID);
        }
    }
}
