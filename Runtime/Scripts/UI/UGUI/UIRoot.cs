using UnityEngine;

namespace ST.Core.UI
{
    /// <summary>
    /// UI 场景根节点，挂载在 UI 根 GameObject 上。
    /// 包含普通层和顶层两套 Camera + Canvas + PanelRoot，
    /// 顶层（Top）不受后期特效影响，适合 Loading、引导等常驻 UI。
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        [Header("普通 UI 层")]
        public Camera        m_UICamera;
        public Canvas        m_RootCanvas;
        public RectTransform m_PanelRoot;

        [Header("顶层 UI（不受后期特效影响）")]
        public Camera        m_TopUICamera;
        public Canvas        m_TopRootCanvas;
        public RectTransform m_TopPanelRoot;

        /// <summary>UI 设计分辨率宽度。</summary>
        public const int DEFAULT_UI_WIDTH  = 1334;
        /// <summary>UI 设计分辨率高度。</summary>
        public const int DEFAULT_UI_HEIGHT = 750;

        /// <summary>
        /// 根据排序层级返回对应的面板挂载根节点。
        /// <see cref="PanelSortLayer.Top"/> 使用顶层根节点，其余使用普通根节点。
        /// </summary>
        public RectTransform GetPanelRoot(PanelSortLayer sortLayer)
        {
            return sortLayer == PanelSortLayer.Top ? m_TopPanelRoot : m_PanelRoot;
        }
    }
}
