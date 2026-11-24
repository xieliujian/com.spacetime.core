using System;
using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>
    /// 资源加载抽象基类：定义同步/异步加载与场景加载接口，管理 <see cref="IAssetDecorator"/> 链，
    /// 并通过 <see cref="SetConfig"/> 注入路径配置。
    /// </summary>
    public abstract class BaseResourceModule : ILogicModule
    {
        /// <summary>当前派生实例的全局引用，由构造函数赋值。</summary>
        protected static BaseResourceModule s_Instance;

        /// <summary>已注册的资产装饰器列表（去重）。</summary>
        protected readonly List<IAssetDecorator> m_Decorators =
            new List<IAssetDecorator>(ResourceDefine.DECORATOR_LIST_CAPACITY);

        /// <summary>业务侧实现的资源配置。</summary>
        protected IResourceConfig m_Config;
        /// <summary>基于 <see cref="m_Config"/> 构造的路径帮助类。</summary>
        protected FilePathHelper m_FilePathHelper;

        /// <summary>全局资源加载器实例（与最后一次构造的派生类对应）。</summary>
        public static BaseResourceModule instance
        {
            get { return s_Instance; }
        }

        /// <summary>构造时注册 <see cref="instance"/> 并清空装饰器列表。</summary>
        public BaseResourceModule()
        {
            s_Instance = this;
            m_Decorators.Clear();
        }

        /// <summary>
        /// 注入资源配置并创建 <see cref="FilePathHelper"/>；须在 <see cref="ILogicModule.DoInit"/> 前调用。
        /// </summary>
        /// <param name="config">非空的资源配置实现。</param>
        public void SetConfig(IResourceConfig config)
        {
            m_Config = config;
            m_FilePathHelper = new FilePathHelper(config);
        }

        /// <summary>同步加载单个资源。</summary>
        /// <param name="path">目录或逻辑路径前缀。</param>
        /// <param name="filename">不含扩展名的资源名。</param>
        /// <param name="suffix">扩展名或后缀。</param>
        /// <param name="restype">逻辑类型，影响装饰器与类型映射。</param>
        /// <returns>加载结果，失败时可能为 <c>null</c>。</returns>
        public abstract object LoadResourceSync(string path, string filename, string suffix,
            ResourceType restype = ResourceType.Default);

        /// <summary>同步加载路径下全部子资源。</summary>
        public abstract object[] LoadAllResourceSync(string path, string filename, string suffix);

        /// <summary>异步加载单个资源。</summary>
        /// <param name="callback">完成回调，参数可能经装饰器处理。</param>
        public abstract void LoadResourceAsync(string path, string filename, string suffix,
            ResourceLoadComplete callback, ResourceType restype = ResourceType.Default);

        /// <summary>异步加载场景。</summary>
        /// <param name="progress">可选进度回调。</param>
        /// <param name="complete">完成回调，参数常为场景相关对象。</param>
        public abstract void LoadSceneAsync(string path, string filename, string suffix,
            ResourceLoadProgress progress = null, ResourceLoadComplete complete = null);

        /// <summary>注册资产装饰器（相同实例不重复添加）。</summary>
        public void InstallDecorator(IAssetDecorator decorator)
        {
            if (m_Decorators.Contains(decorator))
            {
                return;
            }

            m_Decorators.Add(decorator);
        }

        /// <summary>默认无滞后帧逻辑。</summary>
        public override void DoLateUpdate() { }

        /// <summary>将 <see cref="ResourceType"/> 映射为加载与装饰使用的 CLR 类型。</summary>
        protected Type Type2Native(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.String:
                    return typeof(string);
                case ResourceType.Bytes:
                    return typeof(byte[]);
                default:
                    return typeof(object);
            }
        }
    }
}
