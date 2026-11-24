using ST.Core.Logging;

namespace ST.Core.UI
{
    /// <summary>UI 模块打开与关闭部分，负责实例获取、缓存复用与运行列表维护。</summary>
    public partial class UIModule
    {
        /// <summary>
        /// 打开面板，排序层级取 <see cref="UIData.sortLayer"/> 配置。
        /// </summary>
        /// <param name="uiID">面板类型整型 ID（上层 <c>(int)UIID.XxxPanel</c>）。</param>
        /// <param name="args">透传给 <see cref="AbstractPage.OnOpen"/> 的参数。</param>
        /// <returns>面板实例 panelID，失败返回 -1。</returns>
        public int OpenPanel(int uiID, params object[] args)
        {
            int panelID = InnerOpenPanel(uiID, null, args);
            return panelID;
        }

        /// <summary>强制以 <see cref="PanelSortLayer.Top"/> 打开面板。</summary>
        public int OpenTopPanel(int uiID, params object[] args)
        {
            int panelID = InnerOpenPanel(uiID, PanelSortLayer.Top, args);
            return panelID;
        }

        /// <summary>强制以 <see cref="PanelSortLayer.Bottom"/> 打开面板。</summary>
        public int OpenBottomPanel(int uiID, params object[] args)
        {
            int panelID = InnerOpenPanel(uiID, PanelSortLayer.Bottom, args);
            return panelID;
        }

        /// <summary>
        /// 打开流程主体：校验配置、处理单例、复用缓存实例或新建实例，再执行打开并重排。
        /// </summary>
        /// <param name="uiID">面板类型 ID。</param>
        /// <param name="layerOverride">强制层级；为 <c>null</c> 时使用 <see cref="UIData.sortLayer"/>。</param>
        /// <param name="args">透传给面板的打开参数。</param>
        /// <returns>面板实例 panelID，配置缺失时返回 -1。</returns>
        int InnerOpenPanel(int uiID, PanelSortLayer? layerOverride, object[] args)
        {
            UIData data = UIDataTable.GetData(uiID);
            if (data == null)
            {
                STLogger.LogWarningF("[UIModule] UIData not registered, uiID={0}", uiID);
                return -1;
            }

            // 单例面板：已打开则直接返回已有实例的 panelID
            if (data.isSingleton)
            {
                UIPanelActive existing = FindRunningByUIID(uiID);
                if (existing != null)
                {
                    int existingPanelID = existing.panelID;
                    return existingPanelID;
                }
            }

            PanelSortLayer sortLayer = data.sortLayer;
            if (layerOverride.HasValue)
            {
                sortLayer = layerOverride.Value;
            }

            // 优先从缓存中取
            UIPanelActive panelActive = FindCached(uiID);
            if (panelActive != null)
            {
                m_CachedActives.Remove(panelActive);
            }
            else
            {
                panelActive = new UIPanelActive(uiID, sortLayer, m_ResourceLoad, m_UIRoot);
            }

            panelActive.insertOrder = ++m_InsertOrderCounter;
            AddRunning(panelActive);

            panelActive.Open(args);
            CheckResortPanel();

            int panelID = panelActive.panelID;
            return panelID;
        }

        /// <summary>按类型 ID 关闭面板（关闭最近打开的一个）。</summary>
        public void ClosePanel(int uiID)
        {
            UIPanelActive panelActive = FindRunningByUIID(uiID);
            if (panelActive == null)
            {
                return;
            }

            InnerClosePanel(panelActive);
        }

        /// <summary>按实例 panelID 精确关闭特定面板（适用于非单例场景）。</summary>
        public void ClosePanelByPanelID(int panelID)
        {
            UIPanelActive panelActive;
            bool isFound = m_RunningActivesMap.TryGetValue(panelID, out panelActive);
            if (!isFound)
            {
                return;
            }

            InnerClosePanel(panelActive);
        }

        /// <summary>
        /// 关闭流程主体：先从运行列表移除，再按缓存配置决定销毁或放入缓存，最后重排剩余面板。
        /// </summary>
        /// <param name="panelActive">待关闭的面板记录。</param>
        void InnerClosePanel(UIPanelActive panelActive)
        {
            RemoveRunning(panelActive);

            UIData data = UIDataTable.GetData(panelActive.uiID);
            bool shouldCache = false;
            if (data != null)
            {
                // 未达到配置的缓存上限时保留实例，下次打开直接复用
                int cacheCount = data.cacheCount;
                int cachedCount = CountCached(panelActive.uiID);
                shouldCache = (cacheCount > 0) && (cachedCount < cacheCount);
            }

            bool shouldDestroy = !shouldCache;

            panelActive.Close(shouldDestroy);

            if (shouldCache)
            {
                m_CachedActives.Add(panelActive);
            }

            CheckResortPanel();
        }

        /// <summary>把面板记录同时加入运行列表与 panelID 索引，保证两种查找方式结果一致。</summary>
        /// <param name="panelActive">新打开的面板记录。</param>
        void AddRunning(UIPanelActive panelActive)
        {
            m_RunningActives.Add(panelActive);
            m_RunningActivesMap[panelActive.panelID] = panelActive;
        }

        /// <summary>把面板记录从运行列表与 panelID 索引中移除，关闭流程会先经过这里。</summary>
        /// <param name="panelActive">待移除的面板记录。</param>
        void RemoveRunning(UIPanelActive panelActive)
        {
            m_RunningActives.Remove(panelActive);
            m_RunningActivesMap.Remove(panelActive.panelID);
        }
    }
}
