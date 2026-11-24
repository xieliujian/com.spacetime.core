using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>资源模块成员定义部分，集中存放全局实例、配置与加载器依赖。</summary>
    public partial class ResourceModule
    {
        /// <summary>当前实例的全局引用，由构造函数赋值。</summary>
        static ResourceModule s_Instance;

        /// <summary>已注册的资产装饰器列表，相同实例只保留一份。</summary>
        readonly List<IAssetDecorator> m_Decorators =
            new List<IAssetDecorator>(ResourceDefine.DECORATOR_LIST_CAPACITY);

        /// <summary>业务侧实现的资源配置。</summary>
        IResourceConfig m_Config;

        /// <summary>基于 <see cref="m_Config"/> 构造的路径帮助类。</summary>
        FilePathHelper m_FilePathHelper;

        /// <summary>编辑器模式下的 AssetDatabase 加载器。</summary>
        EditorResourceLoad m_EditorResLoad;

        /// <summary>AssetBundle 模式加载器。</summary>
        AssetBundleLoad m_AssetBundleLoad;

        /// <summary>为 <c>true</c> 时编辑器也使用 AssetBundle 路径加载，用于本地验证包体。</summary>
        public static bool useAssetBundle = false;
    }
}
