using System;

namespace ST.Core.UI
{
    /// <summary>面板排序层级，决定面板在三大分组中的位置。</summary>
    public enum PanelSortLayer : byte
    {
        /// <summary>底层面板，如主界面背景。</summary>
        Bottom = 0,
        /// <summary>普通面板，自动叠加。</summary>
        Auto   = 1,
        /// <summary>顶层面板，如 Loading、新手引导。</summary>
        Top    = 2,
    }

    /// <summary>面板对下层的遮挡行为，支持位标志组合。</summary>
    [Flags]
    public enum PanelHideMask : byte
    {
        /// <summary>不影响下层。</summary>
        None                 = 0,
        /// <summary>下层不可交互。</summary>
        UnInteractive        = 1 << 0,
        /// <summary>下层不可见。</summary>
        Hide                 = 1 << 1,
        /// <summary>下层不可见且不可交互。</summary>
        HideAndUnInteractive = 3,
    }

    /// <summary>面板关闭时的退场动画类型。</summary>
    public enum PanelCloseTween : byte
    {
        /// <summary>无动画，直接关闭。</summary>
        None  = 0,
        /// <summary>缩放退场。</summary>
        Scale = 1,
        /// <summary>淡出退场。</summary>
        Fade  = 2,
    }
}
