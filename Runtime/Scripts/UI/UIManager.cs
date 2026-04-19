using System.Collections.Generic;
using UnityEngine;

namespace ST.Core.UI
{
    /// <summary>
    /// UI 系统总控制器，负责所有面板的打开/关闭/排序/可见性。
    /// <para>
    /// 使用方式：
    /// <list type="number">
    ///   <item>在启动时调用 <see cref="Setup"/> 注入 <see cref="BaseResourceLoad"/> 与 <see cref="UIRoot"/>。</item>
    ///   <item>调用 <see cref="UIDataTable.Register"/> 注册所有面板/页面配置。</item>
    ///   <item>通过 <see cref="S"/> 单例调用 <see cref="OpenPanel"/> / <see cref="ClosePanel"/> 等 API。</item>
    /// </list>
    /// </para>
    /// </summary>
    public class UIManager : IManager
    {
        // ─── 单例 ──────────────────────────────────────────────────────────

        static UIManager s_Instance;

        /// <summary>全局 <see cref="UIManager"/> 单例引用。</summary>
        public static UIManager S { get { return s_Instance; } }

        // ─── 依赖 ──────────────────────────────────────────────────────────

        BaseResourceLoad m_ResourceLoad;
        UIRoot           m_UIRoot;

        // ─── 运行状态 ──────────────────────────────────────────────────────

        /// <summary>当前运行中的面板列表，按排序层级 + sortIndex 排列（底→顶）。</summary>
        readonly List<UIPanelActive>            m_RunningActives    = new List<UIPanelActive>(CommonDefine.s_ListConst_16);
        /// <summary>panelID → UIPanelActive，用于 O(1) 按实例 ID 查找。</summary>
        readonly Dictionary<int, UIPanelActive> m_RunningActivesMap = new Dictionary<int, UIPanelActive>(CommonDefine.s_ListConst_16);
        /// <summary>关闭后缓存的面板，下次打开时优先复用。</summary>
        readonly List<UIPanelActive>            m_CachedActives     = new List<UIPanelActive>(CommonDefine.s_ListConst_16);

        int m_InsertOrderCounter = 0;

        // sortingOrder 各层基准值（每层最多 100 个面板叠加）
        const int k_SortBase_Bottom = 0;
        const int k_SortBase_Auto   = 100;
        const int k_SortBase_Top    = 200;

        // ─── 构造 ──────────────────────────────────────────────────────────

        public UIManager()
        {
            s_Instance = this;
        }

        /// <summary>
        /// 初始化前注入依赖。必须在 <see cref="DoInit"/> 之前调用。
        /// </summary>
        /// <param name="resourceLoad">已初始化的资源加载器。</param>
        /// <param name="uiRoot">场景中的 <see cref="UIRoot"/> 组件。</param>
        public void Setup(BaseResourceLoad resourceLoad, UIRoot uiRoot)
        {
            m_ResourceLoad = resourceLoad;
            m_UIRoot       = uiRoot;
        }

        // ─── IManager 生命周期 ─────────────────────────────────────────────

        public override void DoInit()       { }
        public override void DoUpdate()     { }
        public override void DoLateUpdate() { }

        /// <summary>关闭并销毁所有运行中及缓存中的面板，清空状态。</summary>
        public override void DoClose()
        {
            for (int i = m_RunningActives.Count - 1; i >= 0; i--)
                m_RunningActives[i].Close(true);
            m_RunningActives.Clear();
            m_RunningActivesMap.Clear();

            for (int i = m_CachedActives.Count - 1; i >= 0; i--)
                m_CachedActives[i].Close(true);
            m_CachedActives.Clear();
        }

        // ─── 打开面板 ──────────────────────────────────────────────────────

        /// <summary>
        /// 打开面板，排序层级取 <see cref="UIData.sortLayer"/> 配置。
        /// </summary>
        /// <param name="uiID">面板类型整型 ID（上层 <c>(int)UIID.XxxPanel</c>）。</param>
        /// <param name="args">透传给 <see cref="AbstractPage.OnOpen"/> 的参数。</param>
        /// <returns>面板实例 panelID，失败返回 -1。</returns>
        public int OpenPanel(int uiID, params object[] args)
        {
            return InnerOpenPanel(uiID, null, args);
        }

        /// <summary>强制以 <see cref="PanelSortLayer.Top"/> 打开面板。</summary>
        public int OpenTopPanel(int uiID, params object[] args)
        {
            return InnerOpenPanel(uiID, PanelSortLayer.Top, args);
        }

        /// <summary>强制以 <see cref="PanelSortLayer.Bottom"/> 打开面板。</summary>
        public int OpenBottomPanel(int uiID, params object[] args)
        {
            return InnerOpenPanel(uiID, PanelSortLayer.Bottom, args);
        }

        int InnerOpenPanel(int uiID, PanelSortLayer? layerOverride, object[] args)
        {
            UIData data = UIDataTable.GetData(uiID);
            if (data == null)
            {
                Debug.LogWarning($"[UIManager] UIData not registered, uiID={uiID}");
                return -1;
            }

            // 单例面板：已打开则直接返回已有实例的 panelID
            if (data.isSingleton)
            {
                var existing = FindRunningByUIID(uiID);
                if (existing != null) return existing.panelID;
            }

            var sortLayer   = layerOverride ?? data.sortLayer;

            // 优先从缓存中取
            UIPanelActive panelActive = FindCached(uiID);
            if (panelActive != null)
                m_CachedActives.Remove(panelActive);
            else
                panelActive = new UIPanelActive(uiID, sortLayer, m_ResourceLoad, m_UIRoot);

            panelActive.insertOrder = ++m_InsertOrderCounter;
            AddRunning(panelActive);

            panelActive.Open(args);
            CheckResortPanel();

            return panelActive.panelID;
        }

        // ─── 关闭面板 ──────────────────────────────────────────────────────

        /// <summary>按类型 ID 关闭面板（关闭最近打开的一个）。</summary>
        public void ClosePanel(int uiID)
        {
            var panelActive = FindRunningByUIID(uiID);
            if (panelActive == null) return;
            InnerClosePanel(panelActive);
        }

        /// <summary>按实例 panelID 精确关闭特定面板（适用于非单例场景）。</summary>
        public void ClosePanelByPanelID(int panelID)
        {
            if (!m_RunningActivesMap.TryGetValue(panelID, out var panelActive)) return;
            InnerClosePanel(panelActive);
        }

        void InnerClosePanel(UIPanelActive panelActive)
        {
            RemoveRunning(panelActive);

            UIData data        = UIDataTable.GetData(panelActive.uiID);
            bool   shouldCache = data != null
                                 && data.cacheCount > 0
                                 && CountCached(panelActive.uiID) < data.cacheCount;

            panelActive.Close(!shouldCache);

            if (shouldCache)
                m_CachedActives.Add(panelActive);

            CheckResortPanel();
        }

        // ─── 查询 API ──────────────────────────────────────────────────────

        /// <summary>返回类型 ID 对应的面板实例；未打开或仍在加载中时返回 <c>null</c>。</summary>
        public AbstractPanel FindPanel(int uiID)
        {
            var active = FindRunningByUIID(uiID);
            return active?.panel;
        }

        /// <summary>按 MonoBehaviour 类型泛型查找运行中的面板。</summary>
        public T FindPanel<T>() where T : AbstractPanel
        {
            foreach (var active in m_RunningActives)
            {
                if (active.panel is T t) return t;
            }
            return null;
        }

        /// <summary>面板是否在运行列表中（包含仍在加载的面板）。</summary>
        public bool IsOpened(int uiID)
        {
            return FindRunningByUIID(uiID) != null;
        }

        /// <summary>面板是否处于激活且可见状态（已加载完成 + isVisible = true）。</summary>
        public bool IsPanelActive(int uiID)
        {
            var active = FindRunningByUIID(uiID);
            return active != null && active.isReady && active.isVisible;
        }

        /// <summary>返回面板当前的可见性状态。</summary>
        public bool IsPanelVisible(int uiID)
        {
            var active = FindRunningByUIID(uiID);
            return active != null && active.isVisible;
        }

        /// <summary>返回指定面板下某个子页面是否已挂载打开。</summary>
        public bool IsPageOpen(int uiID, int pageID)
        {
            var active = FindRunningByUIID(uiID);
            return active != null && active.IsPageOpened(pageID);
        }

        /// <summary>手动覆盖面板可见性（不影响 HideMask 自动计算逻辑）。</summary>
        public void SetPanelVisible(int uiID, bool visible)
        {
            var active = FindRunningByUIID(uiID);
            if (active != null) active.isVisible = visible;
        }

        /// <summary>手动覆盖面板交互性（不影响 HideMask 自动计算逻辑）。</summary>
        public void SetPanelInteract(int uiID, bool interact)
        {
            var active = FindRunningByUIID(uiID);
            if (active != null) active.isInteract = interact;
        }

        // ─── 排序与可见性刷新 ──────────────────────────────────────────────

        /// <summary>
        /// 对运行中的面板重新排序并分配 <c>Canvas.sortingOrder</c>，
        /// 之后根据各面板的 <see cref="AbstractPanel.hideMask"/> 重新计算可见性与交互性。
        /// </summary>
        void CheckResortPanel()
        {
            // 稳定排序：先按 sortLayer，再按 sortIndex，最后按 insertOrder（打开顺序）
            m_RunningActives.Sort((a, b) =>
            {
                int cmp = a.sortLayer.CompareTo(b.sortLayer);
                if (cmp != 0) return cmp;
                cmp = a.sortIndex.CompareTo(b.sortIndex);
                if (cmp != 0) return cmp;
                return a.insertOrder.CompareTo(b.insertOrder);
            });

            // 分配 sortingOrder
            int bottomIdx = 0, autoIdx = 0, topIdx = 0;
            foreach (var active in m_RunningActives)
            {
                if (active.panel == null || active.panel.canvas == null) continue;
                int order;
                switch (active.sortLayer)
                {
                    case PanelSortLayer.Bottom:
                        order = k_SortBase_Bottom + bottomIdx++;
                        break;
                    case PanelSortLayer.Top:
                        order = k_SortBase_Top + topIdx++;
                        break;
                    default:
                        order = k_SortBase_Auto + autoIdx++;
                        break;
                }
                active.panel.canvas.sortingOrder = order;
            }

            RefreshVisibleAndMask();
        }

        /// <summary>
        /// 从栈顶向下遍历，根据各面板的 <see cref="AbstractPanel.hideMask"/> 计算每一层的可见性与交互性。
        /// </summary>
        void RefreshVisibleAndMask()
        {
            bool hiddenBelow     = false;
            bool uninteractBelow = false;

            for (int i = m_RunningActives.Count - 1; i >= 0; i--)
            {
                var active  = m_RunningActives[i];
                active.isVisible  = !hiddenBelow;
                active.isInteract = !uninteractBelow;

                var mask = active.panel != null ? active.panel.hideMask : PanelHideMask.None;
                if ((mask & PanelHideMask.Hide) != 0)
                    hiddenBelow = true;
                if ((mask & PanelHideMask.UnInteractive) != 0)
                    uninteractBelow = true;
            }
        }

        // ─── 内部回调（供 UIPanelActive 调用） ───────────────────────────

        /// <summary>
        /// 面板 Prefab 异步加载完成后由 <see cref="UIPanelActive"/> 回调，
        /// 触发一次重新排序以保证 <c>sortingOrder</c> 及时生效。
        /// </summary>
        internal void NotifyPanelReady(UIPanelActive panelActive)
        {
            if (!m_RunningActives.Contains(panelActive)) return;
            CheckResortPanel();
        }

        // ─── 辅助方法 ──────────────────────────────────────────────────────

        void AddRunning(UIPanelActive panelActive)
        {
            m_RunningActives.Add(panelActive);
            m_RunningActivesMap[panelActive.panelID] = panelActive;
        }

        void RemoveRunning(UIPanelActive panelActive)
        {
            m_RunningActives.Remove(panelActive);
            m_RunningActivesMap.Remove(panelActive.panelID);
        }

        /// <summary>在运行列表中按 uiID 查找最近打开的面板（列表末尾）。</summary>
        UIPanelActive FindRunningByUIID(int uiID)
        {
            for (int i = m_RunningActives.Count - 1; i >= 0; i--)
            {
                if (m_RunningActives[i].uiID == uiID)
                    return m_RunningActives[i];
            }
            return null;
        }

        UIPanelActive FindCached(int uiID)
        {
            foreach (var a in m_CachedActives)
                if (a.uiID == uiID) return a;
            return null;
        }

        int CountCached(int uiID)
        {
            int count = 0;
            foreach (var a in m_CachedActives)
                if (a.uiID == uiID) count++;
            return count;
        }
    }
}
