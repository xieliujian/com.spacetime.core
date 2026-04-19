using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ST.Core.UI
{
    /// <summary>
    /// 管理单个面板（<see cref="AbstractPanel"/>）的完整生命周期：
    /// 异步资源加载、GameObject 实例化、子页面（<see cref="AbstractPage"/>）的挂载与卸载、
    /// 可见性与交互状态的刷新。
    /// <para>由 <see cref="UIManager"/> 创建并持有，外部不应直接构造。</para>
    /// </summary>
    public class UIPanelActive
    {
        static int s_PanelIDCounter = 0;

        readonly int           m_PanelID;
        readonly int           m_UIID;
        readonly PanelSortLayer m_SortLayer;
        readonly BaseResourceLoad m_ResourceLoad;
        readonly UIRoot        m_UIRoot;

        AbstractPanel m_Panel;
        bool          m_IsVisible  = true;
        bool          m_IsInteract = true;
        bool          m_IsLoading;
        object[]      m_PendingArgs;

        // 插入顺序，供 UIManager 稳定排序使用
        internal int insertOrder;

        readonly List<PageInfo> m_PageInfos       = new List<PageInfo>(CommonDefine.s_ListConst_8);
        readonly List<PageInfo> m_CachedPageInfos = new List<PageInfo>(CommonDefine.s_ListConst_8);

        // ─── 属性 ──────────────────────────────────────────────────────────

        /// <summary>本次运行实例的唯一 ID（自增），可通过 <see cref="UIManager.ClosePanelByPanelID"/> 精确关闭。</summary>
        public int panelID    { get { return m_PanelID; } }

        /// <summary>面板类型 ID（对应上层 <c>UIID</c> 枚举的 int 值）。</summary>
        public int uiID       { get { return m_UIID; } }

        /// <summary>面板的 MonoBehaviour 实例；异步加载完成前为 <c>null</c>。</summary>
        public AbstractPanel panel { get { return m_Panel; } }

        /// <summary>面板排序层级，由 <see cref="UIData.sortLayer"/> 或 <see cref="UIManager.OpenTopPanel"/> 决定。</summary>
        public PanelSortLayer sortLayer { get { return m_SortLayer; } }

        /// <summary>面板在同层内的自定义排序索引；加载完成前返回 0。</summary>
        public int sortIndex  { get { return m_Panel != null ? m_Panel.sortIndex : 0; } }

        /// <summary>面板是否处于可见状态；由 <see cref="UIManager"/> 根据 HideMask 计算后写入。</summary>
        public bool isVisible
        {
            get { return m_IsVisible; }
            set { ApplyVisible(value); }
        }

        /// <summary>面板是否可交互；由 <see cref="UIManager"/> 根据 HideMask 计算后写入。</summary>
        public bool isInteract
        {
            get { return m_IsInteract; }
            set { ApplyInteract(value); }
        }

        /// <summary>面板 Prefab 是否仍在异步加载中。</summary>
        public bool isLoading { get { return m_IsLoading; } }

        /// <summary>面板 Prefab 已加载并实例化完成。</summary>
        public bool isReady   { get { return m_Panel != null; } }

        // ─── 构造 ──────────────────────────────────────────────────────────

        internal UIPanelActive(int uiID, PanelSortLayer sortLayer,
                               BaseResourceLoad resourceLoad, UIRoot uiRoot)
        {
            m_PanelID      = ++s_PanelIDCounter;
            m_UIID         = uiID;
            m_SortLayer    = sortLayer;
            m_ResourceLoad = resourceLoad;
            m_UIRoot       = uiRoot;
        }

        // ─── 面板打开 / 关闭 ───────────────────────────────────────────────

        /// <summary>
        /// 打开面板。若 Prefab 已缓存则直接复用，否则异步加载后再执行生命周期。
        /// </summary>
        internal void Open(object[] args)
        {
            if (m_Panel != null)
            {
                m_Panel.gameObject.SetActive(true);
                m_Panel.InternalOpen(args);
                return;
            }

            m_PendingArgs = args;
            m_IsLoading   = true;

            UIData data = UIDataTable.GetData(m_UIID);
            if (data == null)
            {
                Debug.LogWarning($"[UIPanelActive] UIData not found, uiID={m_UIID}");
                m_IsLoading = false;
                return;
            }

            m_ResourceLoad.LoadResourceAsync(
                data.path, data.filename, data.suffix,
                OnPrefabLoaded,
                ResourceType.GameObject);
        }

        void OnPrefabLoaded(object asset)
        {
            m_IsLoading = false;

            var prefab = asset as GameObject;
            if (prefab == null)
            {
                Debug.LogWarning($"[UIPanelActive] Prefab load failed, uiID={m_UIID}");
                return;
            }

            var root = m_UIRoot != null ? m_UIRoot.GetPanelRoot(m_SortLayer) : null;
            var go   = UnityEngine.Object.Instantiate(prefab, root, false);

            // ── 嵌套 Canvas 修正 ──────────────────────────────────────────
            // 1. 强制拉伸填满父容器（PanelRoot / TopPanelRoot）。
            //    Root Canvas 序列化的 RectTransform 变为嵌套 Canvas 后不会自动继承父节点尺寸。
            //    同时强制 localScale=(1,1,1)，防止旧 Prefab 序列化了 scale=0 的情况。
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin  = Vector2.zero;
                rt.anchorMax  = Vector2.one;
                rt.offsetMin  = Vector2.zero;
                rt.offsetMax  = Vector2.zero;
            }

            // 2. 移除嵌套 Canvas 上的 CanvasScaler。
            //    CanvasScaler(ScaleWithScreenSize) 在嵌套 Canvas 上以父容器尺寸为基准计算缩放，
            //    若父容器尺寸为 0 则 scale = 0 / referenceResolution = 0，导致面板不可见。
            //    缩放由根 Canvas（UIRoot.RootCanvas）的 CanvasScaler 统一管理即可。
            var nestedScaler = go.GetComponent<CanvasScaler>();
            if (nestedScaler != null)
                UnityEngine.Object.Destroy(nestedScaler);

            var panel = go.GetComponent<AbstractPanel>();
            if (panel == null)
            {
                Debug.LogWarning($"[UIPanelActive] AbstractPanel component missing, uiID={m_UIID}");
                UnityEngine.Object.Destroy(go);
                return;
            }

            m_Panel             = panel;
            m_Panel.uiID        = m_UIID;
            m_Panel.panelActive = this;

            DiscoverInnerPages();

            m_Panel.InternalCreate();
            m_Panel.InternalOpen(m_PendingArgs);
            m_PendingArgs = null;

            // 通知 UIManager 重新排序（面板就绪后 sortingOrder 才能被赋值）
            UIManager.S?.NotifyPanelReady(this);
        }

        /// <summary>关闭面板。<paramref name="destroy"/> 为 <c>true</c> 时销毁 GameObject，否则仅 SetActive(false)。</summary>
        internal void Close(bool destroy)
        {
            CloseAllPages(destroy);

            if (m_Panel == null) return;

            if (m_Panel.closeTween != PanelCloseTween.None)
                m_Panel.InternalPlayCloseAnimation(() => FinishClose(destroy));
            else
                FinishClose(destroy);
        }

        void FinishClose(bool destroy)
        {
            if (m_Panel == null) return;

            m_Panel.InternalClose();

            if (destroy)
            {
                m_Panel.InternalDispose();
                UnityEngine.Object.Destroy(m_Panel.gameObject);
                m_Panel = null;
            }
            else
            {
                m_Panel.gameObject.SetActive(false);
            }
        }

        // ─── 可见性 / 交互 ─────────────────────────────────────────────────

        void ApplyVisible(bool visible)
        {
            if (m_IsVisible == visible) return;
            m_IsVisible = visible;
            m_Panel?.InternalVisibleChanged(visible);
        }

        void ApplyInteract(bool interact)
        {
            if (m_IsInteract == interact) return;
            m_IsInteract = interact;

            if (m_Panel == null) return;
            var cg = m_Panel.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.interactable = interact;
        }

        // ─── 子页面管理 ────────────────────────────────────────────────────

        /// <summary>
        /// 动态挂载一个子页面到本面板。若已挂载则忽略；优先从缓存复用。
        /// </summary>
        public void AttachPage(int pageID, params object[] args)
        {
            if (IsPageOpened(pageID)) return;

            PageInfo cached = FindCachedPage(pageID);
            if (cached != null)
            {
                m_CachedPageInfos.Remove(cached);
                m_PageInfos.Add(cached);
                cached.page.gameObject.SetActive(true);
                cached.page.InternalOpen(args);
                return;
            }

            UIData data = UIDataTable.GetData(pageID);
            if (data == null)
            {
                Debug.LogWarning($"[UIPanelActive] UIData not found for pageID={pageID}");
                return;
            }

            var info = new PageInfo(pageID);
            m_PageInfos.Add(info);

            m_ResourceLoad.LoadResourceAsync(
                data.path, data.filename, data.suffix,
                (asset) => OnPagePrefabLoaded(asset, info, args),
                ResourceType.GameObject);
        }

        void OnPagePrefabLoaded(object asset, PageInfo info, object[] args)
        {
            // 挂载前可能已被 DettachPage 移除
            if (!m_PageInfos.Contains(info))
            {
                var go2 = (asset as GameObject);
                if (go2 != null) UnityEngine.Object.Destroy(go2);
                return;
            }

            var prefab = asset as GameObject;
            if (prefab == null)
            {
                m_PageInfos.Remove(info);
                return;
            }

            var parent = m_Panel != null ? m_Panel.transform : null;
            var go     = UnityEngine.Object.Instantiate(prefab, parent, false);

            var page = go.GetComponent<AbstractPage>();
            if (page == null)
            {
                UnityEngine.Object.Destroy(go);
                m_PageInfos.Remove(info);
                return;
            }

            info.page        = page;
            page.uiID        = info.uiID;
            page.panelActive = this;

            page.InternalCreate();
            page.InternalOpen(args);
            page.InternalPlayOpenAnimation(null);
        }

        /// <summary>
        /// 卸载子页面。<paramref name="useCache"/> 为 <c>true</c> 且配置了缓存数量时不销毁 GameObject。
        /// </summary>
        public void DettachPage(int pageID, bool useCache = false)
        {
            PageInfo info = FindRunningPage(pageID);
            if (info == null) return;

            m_PageInfos.Remove(info);
            if (info.page == null) return;

            UIData data      = UIDataTable.GetData(pageID);
            int    maxCache  = data != null ? data.cacheCount : 0;
            bool   doCache   = useCache && maxCache > 0 && CountCachedPages(pageID) < maxCache;

            if (doCache)
            {
                info.page.InternalPlayCloseAnimation(() =>
                {
                    info.page.InternalClose();
                    info.page.gameObject.SetActive(false);
                    m_CachedPageInfos.Add(info);
                });
            }
            else
            {
                info.page.InternalPlayCloseAnimation(() =>
                {
                    info.page.InternalClose();
                    info.page.InternalDispose();
                    UnityEngine.Object.Destroy(info.page.gameObject);
                });
            }
        }

        /// <summary>返回指定 pageID 的子页面当前是否已挂载并打开。</summary>
        public bool IsPageOpened(int pageID)
        {
            return FindRunningPage(pageID) != null;
        }

        // ─── 内部页面自动发现 ──────────────────────────────────────────────

        /// <summary>
        /// 面板 Prefab 实例化后，自动扫描子节点中已挂载 <see cref="AbstractPage"/> 且 <c>uiID != 0</c> 的组件，
        /// 注册为内部页面并执行 <see cref="AbstractPage.OnCreate"/>。
        /// </summary>
        void DiscoverInnerPages()
        {
            if (m_Panel == null) return;

            var pages = m_Panel.GetComponentsInChildren<AbstractPage>(true);
            foreach (var page in pages)
            {
                if (page == (AbstractPage)m_Panel) continue;
                if (page.uiID == 0) continue;
                if (IsPageOpened(page.uiID)) continue;

                var info  = new PageInfo(page.uiID);
                info.page = page;
                page.panelActive = this;
                m_PageInfos.Add(info);
                page.InternalCreate();
            }
        }

        // ─── 批量关闭页面 ──────────────────────────────────────────────────

        void CloseAllPages(bool destroy)
        {
            for (int i = m_PageInfos.Count - 1; i >= 0; i--)
            {
                var info = m_PageInfos[i];
                if (info.page == null) continue;
                info.page.InternalClose();
                if (destroy)
                {
                    info.page.InternalDispose();
                    UnityEngine.Object.Destroy(info.page.gameObject);
                }
                else
                {
                    info.page.gameObject.SetActive(false);
                }
            }
            m_PageInfos.Clear();

            if (destroy)
            {
                foreach (var info in m_CachedPageInfos)
                {
                    if (info.page == null) continue;
                    info.page.InternalDispose();
                    UnityEngine.Object.Destroy(info.page.gameObject);
                }
                m_CachedPageInfos.Clear();
            }
        }

        // ─── 辅助查找 ──────────────────────────────────────────────────────

        PageInfo FindRunningPage(int pageID)
        {
            foreach (var info in m_PageInfos)
                if (info.uiID == pageID) return info;
            return null;
        }

        PageInfo FindCachedPage(int pageID)
        {
            foreach (var info in m_CachedPageInfos)
                if (info.uiID == pageID) return info;
            return null;
        }

        int CountCachedPages(int pageID)
        {
            int count = 0;
            foreach (var info in m_CachedPageInfos)
                if (info.uiID == pageID) count++;
            return count;
        }

        // ─── 内部数据结构 ─────────────────────────────────────────────────

        class PageInfo
        {
            public readonly int uiID;
            public AbstractPage page;

            public PageInfo(int uiID) { this.uiID = uiID; }
        }
    }
}
