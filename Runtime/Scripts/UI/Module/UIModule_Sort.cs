using System;
using UnityEngine;

namespace ST.Core.UI
{
    /// <summary>UI 模块排序部分，负责 sortingOrder 分配与 HideMask 可见性刷新。</summary>
    public partial class UIModule
    {
        /// <summary>面板排序比较器缓存，避免每次排序都分配新的委托。</summary>
        static readonly Comparison<UIPanelActive> s_PanelComparison = ComparePanelActive;

        /// <summary>
        /// 面板 Prefab 异步加载完成后由 <see cref="UIPanelActive"/> 回调，
        /// 触发一次重新排序以保证 <c>sortingOrder</c> 及时生效。
        /// </summary>
        internal void NotifyPanelReady(UIPanelActive panelActive)
        {
            bool isRunning = m_RunningActives.Contains(panelActive);
            if (!isRunning)
            {
                return;
            }

            CheckResortPanel();
        }

        /// <summary>
        /// 对运行中的面板重新排序并分配 <c>Canvas.sortingOrder</c>，
        /// 之后根据各面板的 <see cref="AbstractPanel.hideMask"/> 重新计算可见性与交互性。
        /// </summary>
        void CheckResortPanel()
        {
            // 稳定排序：先按 sortLayer，再按 sortIndex，最后按 insertOrder（打开顺序）
            m_RunningActives.Sort(s_PanelComparison);

            // 分配 sortingOrder
            int bottomIndex = 0;
            int autoIndex = 0;
            int topIndex = 0;
            for (int i = 0; i < m_RunningActives.Count; i++)
            {
                UIPanelActive active = m_RunningActives[i];
                AbstractPanel panel = active.panel;
                if (panel == null)
                {
                    continue;
                }

                Canvas canvas = panel.canvas;
                if (canvas == null)
                {
                    continue;
                }

                int sortingOrder = SORT_BASE_AUTO + autoIndex;
                switch (active.sortLayer)
                {
                    case PanelSortLayer.Bottom:
                        sortingOrder = SORT_BASE_BOTTOM + bottomIndex;
                        bottomIndex = bottomIndex + 1;
                        break;
                    case PanelSortLayer.Top:
                        sortingOrder = SORT_BASE_TOP + topIndex;
                        topIndex = topIndex + 1;
                        break;
                    default:
                        sortingOrder = SORT_BASE_AUTO + autoIndex;
                        autoIndex = autoIndex + 1;
                        break;
                }

                canvas.sortingOrder = sortingOrder;
            }

            RefreshVisibleAndMask();
        }

        /// <summary>
        /// 从栈顶向下遍历，根据各面板的 <see cref="AbstractPanel.hideMask"/> 计算每一层的可见性与交互性。
        /// </summary>
        void RefreshVisibleAndMask()
        {
            bool hiddenBelow = false;
            bool uninteractBelow = false;

            for (int i = m_RunningActives.Count - 1; i >= 0; i--)
            {
                UIPanelActive active = m_RunningActives[i];
                active.isVisible = !hiddenBelow;
                active.isInteract = !uninteractBelow;

                PanelHideMask mask = PanelHideMask.None;
                AbstractPanel panel = active.panel;
                if (panel != null)
                {
                    mask = panel.hideMask;
                }

                bool isHide = (mask & PanelHideMask.Hide) != 0;
                if (isHide)
                {
                    hiddenBelow = true;
                }

                bool isUnInteractive = (mask & PanelHideMask.UnInteractive) != 0;
                if (isUnInteractive)
                {
                    uninteractBelow = true;
                }
            }
        }

        /// <summary>面板排序比较：sortLayer → sortIndex → 打开顺序。</summary>
        static int ComparePanelActive(UIPanelActive left, UIPanelActive right)
        {
            int layerCompare = left.sortLayer.CompareTo(right.sortLayer);
            if (layerCompare != 0)
            {
                return layerCompare;
            }

            int indexCompare = left.sortIndex.CompareTo(right.sortIndex);
            if (indexCompare != 0)
            {
                return indexCompare;
            }

            int orderCompare = left.insertOrder.CompareTo(right.insertOrder);
            return orderCompare;
        }
    }
}
