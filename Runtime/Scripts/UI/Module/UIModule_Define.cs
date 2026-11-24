using System.Collections.Generic;

namespace ST.Core.UI
{
    /// <summary>
    /// UI 模块成员定义部分，集中存放单例、依赖与运行状态数据。
    /// </summary>
    public partial class UIModule
    {
        /// <summary>底层面板 sortingOrder 基准值。</summary>
        const int SORT_BASE_BOTTOM = 0;

        /// <summary>普通面板 sortingOrder 基准值。</summary>
        const int SORT_BASE_AUTO = 100;

        /// <summary>顶层面板 sortingOrder 基准值。</summary>
        const int SORT_BASE_TOP = 200;

        /// <summary>已初始化的资源加载器，由 <see cref="Setup"/> 注入。</summary>
        ResourceModule m_ResourceLoad;

        /// <summary>场景中的 UI 根节点，由 <see cref="Setup"/> 注入。</summary>
        UIRoot m_UIRoot;

        /// <summary>当前运行中的面板列表，按排序层级 + sortIndex 排列（底→顶）。</summary>
        readonly List<UIPanelActive> m_RunningActives =
            new List<UIPanelActive>(CommonDefine.s_ListConst_16);

        /// <summary>panelID → UIPanelActive，用于 O(1) 按实例 ID 查找。</summary>
        readonly Dictionary<int, UIPanelActive> m_RunningActivesMap =
            new Dictionary<int, UIPanelActive>(CommonDefine.s_ListConst_16);

        /// <summary>关闭后缓存的面板，下次打开时优先复用。</summary>
        readonly List<UIPanelActive> m_CachedActives =
            new List<UIPanelActive>(CommonDefine.s_ListConst_16);

        /// <summary>面板打开顺序计数，用于同层同 sortIndex 时的稳定排序。</summary>
        int m_InsertOrderCounter;
    }
}
