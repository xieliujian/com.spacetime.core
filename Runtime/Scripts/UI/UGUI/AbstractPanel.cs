using UnityEngine;

namespace ST.Core.UI
{
    /// <summary>
    /// 面板基类，所有面板必须继承此类。
    /// <para>与 <see cref="AbstractPage"/> 的区别：</para>
    /// <list type="bullet">
    ///   <item>面板必须挂载 <see cref="Canvas"/> 组件（由 <see cref="RequireComponent"/> 强制）。</item>
    ///   <item>面板可独立打开，支持 <see cref="PanelSortLayer"/> 三层排序与 <see cref="PanelHideMask"/> 遮挡机制。</item>
    ///   <item>面板可通过 <see cref="panelActive"/> 动态挂载/卸载子 <see cref="AbstractPage"/>。</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public abstract class AbstractPanel : AbstractPage
    {
        Canvas m_Canvas;

        /// <summary>面板自身的 <see cref="Canvas"/> 组件，用于设置 <c>sortingOrder</c>。</summary>
        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                    m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        /// <summary>
        /// 面板在同层（<see cref="PanelSortLayer"/>）内的自定义排序索引，值越大越靠上。
        /// 默认 0；常驻顶层面板（Loading、引导）可使用较大固定值。
        /// </summary>
        public virtual int sortIndex => 0;

        /// <summary>
        /// 面板对下层面板的遮挡行为。
        /// 打开此面板后 <see cref="UIManager"/> 会根据此值自动刷新下层的可见性与交互性。
        /// </summary>
        public virtual PanelHideMask hideMask => PanelHideMask.None;

        /// <summary>面板关闭时的退场动画类型，<see cref="PanelCloseTween.None"/> 表示无动画。</summary>
        public virtual PanelCloseTween closeTween => PanelCloseTween.None;
    }
}
