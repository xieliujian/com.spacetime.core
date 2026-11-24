namespace ST.Core
{
    /// <summary>
    /// 逻辑模块基类，约定统一的初始化、帧更新与关闭生命周期。
    /// <para>
    /// 模块由 <see cref="ST.Core.Launch.GameWorld"/> 注册并按注册顺序驱动。
    /// 注册时会注入内核，模块内可通过 <c>m_Kernel</c> 查找其它模块。
    /// </para>
    /// </summary>
    public abstract class ILogicModule
    {
        /// <summary>所属内核，由注册它的游戏世界注入；未注册时为 null。</summary>
        protected IKernel m_Kernel;

        /// <summary>由游戏世界在注册模块时注入内核引用，业务代码不应主动调用。</summary>
        /// <param name="kernel">模块所属的内核实例。</param>
        internal void SetKernel(IKernel kernel)
        {
            m_Kernel = kernel;
        }

        /// <summary>模块初始化：注册事件、加载配置等，在游戏世界启动时按注册顺序调用一次。</summary>
        public virtual void DoInit()
        {
        }

        /// <summary>每帧逻辑更新，与 Unity <c>Update</c> 对齐；只对重写了本方法的模块调用。</summary>
        public virtual void DoUpdate()
        {
        }

        /// <summary>在每帧 <c>Update</c> 之后调用；只对重写了本方法的模块调用。</summary>
        public virtual void DoLateUpdate()
        {
        }

        /// <summary>释放资源、取消订阅，在游戏世界关闭时按注册逆序调用。</summary>
        public virtual void DoClose()
        {
        }
    }
}
