namespace ST.Core
{
    /// <summary>
    /// 异步任务模块抽象基类：维护全局 <see cref="instance"/>，由 <see cref="AsyncTaskModule"/> 具体实现任务的增删与每帧更新。
    /// </summary>
    public abstract class BaseAsyncTaskModule : ILogicModule
    {
        /// <summary>当前全局异步任务模块引用。</summary>
        protected static BaseAsyncTaskModule s_Instance;

        /// <summary>全局异步任务模块。</summary>
        public static BaseAsyncTaskModule instance
        {
            get { return s_Instance; }
        }

        /// <summary>构造时注册为全局 <see cref="instance"/>。</summary>
        public BaseAsyncTaskModule()
        {
            s_Instance = this;
        }

        /// <summary>将异步任务加入队列，由 <see cref="ILogicModule.DoUpdate"/> 驱动。</summary>
        /// <param name="asynctask">非空任务实例。</param>
        public abstract void AddTask(AsyncTask asynctask);

        /// <summary>默认无滞后帧逻辑。</summary>
        public override void DoLateUpdate() { }
    }
}
