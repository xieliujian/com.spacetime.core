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
        /// <summary>普通层 UI 摄像机，画面会经过后期处理。</summary>
        [Header("普通 UI 层")]
        public Camera        m_UICamera;
        /// <summary>普通层根 Canvas，统一管理 UI 缩放。</summary>
        public Canvas        m_RootCanvas;
        /// <summary>普通层面板挂载节点，承载 Bottom 与 Auto 层。</summary>
        public RectTransform m_PanelRoot;

        /// <summary>顶层 UI 摄像机，渲染在各后期特效之后。</summary>
        [Header("顶层 UI（不受后期特效影响）")]
        public Camera        m_TopUICamera;
        /// <summary>顶层根 Canvas。</summary>
        public Canvas        m_TopRootCanvas;
        /// <summary>顶层面板挂载节点，承载 Top 层。</summary>
        public RectTransform m_TopPanelRoot;

        /// <summary>UI 设计分辨率宽度。</summary>
        public const int DEFAULT_UI_WIDTH  = 1334;
        /// <summary>UI 设计分辨率高度。</summary>
        public const int DEFAULT_UI_HEIGHT = 750;

        /// <summary>
        /// 根据排序层级返回对应的面板挂载根节点。
        /// <see cref="PanelSortLayer.Top"/> 使用顶层根节点，其余使用普通根节点。
        /// </summary>
        /// <param name="sortLayer">面板的排序层级。</param>
        /// <returns>对应层级的挂载节点，未配置时可能为 <c>null</c>。</returns>
        public RectTransform GetPanelRoot(PanelSortLayer sortLayer)
        {
            return sortLayer == PanelSortLayer.Top ? m_TopPanelRoot : m_PanelRoot;
        }
    }
}
