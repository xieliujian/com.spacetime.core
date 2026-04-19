using System;

namespace ST.Core.UI
{
    /// <summary>
    /// 单个面板或页面的静态配置数据，由上层工程在启动时注册到 <see cref="UIDataTable"/>。
    /// </summary>
    public class UIData
    {
        /// <summary>UI 唯一整型标识，由上层 <c>enum UIID : int</c> 转换而来。</summary>
        public int uiID;

        /// <summary>UI 名称，用于调试日志。</summary>
        public string name;

        /// <summary>Prefab 所在目录（末尾不含斜杠），对应 <see cref="BaseResourceLoad.LoadResourceAsync"/> 的 <c>path</c> 参数。</summary>
        public string path;

        /// <summary>Prefab 文件名（不含扩展名），对应 <c>filename</c> 参数。</summary>
        public string filename;

        /// <summary>文件后缀，默认 <c>.prefab</c>。</summary>
        public string suffix = ".prefab";

        /// <summary>面板运行时类型，必须继承 <see cref="AbstractPanel"/> 或 <see cref="AbstractPage"/>。</summary>
        public Type type;

        /// <summary>排序层级，仅对面板（<see cref="AbstractPanel"/>）有效。</summary>
        public PanelSortLayer sortLayer = PanelSortLayer.Auto;

        /// <summary>关闭后最多缓存的实例数量；0 表示不缓存，直接销毁。</summary>
        public int cacheCount = 0;

        /// <summary>是否为单例面板：<c>true</c> 时同一 <see cref="uiID"/> 只允许存在一个运行实例。</summary>
        public bool isSingleton = true;
    }
}
