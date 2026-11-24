namespace ST.Core.UI
{
    /// <summary>UI 模块查询部分，提供面板状态查询与可见性、交互性手动控制。</summary>
    public partial class UIModule
    {
        /// <summary>返回类型 ID 对应的面板实例；未打开或仍在加载中时返回 <c>null</c>。</summary>
        public AbstractPanel FindPanel(int uiID)
        {
            UIPanelActive active = FindRunningByUIID(uiID);
            AbstractPanel panel = null;
            if (active != null)
            {
                panel = active.panel;
            }

            return panel;
        }

        /// <summary>按 MonoBehaviour 类型泛型查找运行中的面板。</summary>
        public T FindPanel<T>() where T : AbstractPanel
        {
            for (int i = 0; i < m_RunningActives.Count; i++)
            {
                UIPanelActive active = m_RunningActives[i];
                AbstractPanel panel = active.panel;
                if (panel is T targetPanel)
                {
                    return targetPanel;
                }
            }

            return null;
        }

        /// <summary>面板是否在运行列表中（包含仍在加载的面板）。</summary>
        public bool IsOpened(int uiID)
        {
            UIPanelActive active = FindRunningByUIID(uiID);
            bool isOpened = active != null;
            return isOpened;
        }

        /// <summary>面板是否处于激活且可见状态（已加载完成 + isVisible = true）。</summary>
        public bool IsPanelActive(int uiID)
        {
            UIPanelActive active = FindRunningByUIID(uiID);
            bool isPanelActive = false;
            if (active != null)
            {
                isPanelActive = active.isReady && active.isVisible;
            }

            return isPanelActive;
        }

        /// <summary>返回面板当前的可见性状态。</summary>
        public bool IsPanelVisible(int uiID)
        {
            UIPanelActive active = FindRunningByUIID(uiID);
            bool isPanelVisible = false;
            if (active != null)
            {
                isPanelVisible = active.isVisible;
            }

            return isPanelVisible;
        }

        /// <summary>返回指定面板下某个子页面是否已挂载打开。</summary>
        public bool IsPageOpen(int uiID, int pageID)
        {
            UIPanelActive active = FindRunningByUIID(uiID);
            bool isPageOpen = false;
            if (active != null)
            {
                isPageOpen = active.IsPageOpened(pageID);
            }

            return isPageOpen;
        }

        /// <summary>手动覆盖面板可见性（不影响 HideMask 自动计算逻辑）。</summary>
        public void SetPanelVisible(int uiID, bool visible)
        {
            UIPanelActive active = FindRunningByUIID(uiID);
            if (active != null)
            {
                active.isVisible = visible;
            }
        }

        /// <summary>手动覆盖面板交互性（不影响 HideMask 自动计算逻辑）。</summary>
        public void SetPanelInteract(int uiID, bool interact)
        {
            UIPanelActive active = FindRunningByUIID(uiID);
            if (active != null)
            {
                active.isInteract = interact;
            }
        }

        /// <summary>在运行列表中按 uiID 查找最近打开的面板（列表末尾）。</summary>
        UIPanelActive FindRunningByUIID(int uiID)
        {
            for (int i = m_RunningActives.Count - 1; i >= 0; i--)
            {
                UIPanelActive active = m_RunningActives[i];
                if (active.uiID == uiID)
                {
                    return active;
                }
            }

            return null;
        }

        /// <summary>在缓存列表中按 uiID 查找可复用的面板。</summary>
        UIPanelActive FindCached(int uiID)
        {
            for (int i = 0; i < m_CachedActives.Count; i++)
            {
                UIPanelActive active = m_CachedActives[i];
                if (active.uiID == uiID)
                {
                    return active;
                }
            }

            return null;
        }

        /// <summary>统计缓存列表中指定 uiID 的面板数量。</summary>
        int CountCached(int uiID)
        {
            int count = 0;
            for (int i = 0; i < m_CachedActives.Count; i++)
            {
                UIPanelActive active = m_CachedActives[i];
                if (active.uiID == uiID)
                {
                    count = count + 1;
                }
            }

            return count;
        }
    }
}
