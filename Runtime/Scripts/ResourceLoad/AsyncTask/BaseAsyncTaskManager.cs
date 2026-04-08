namespace ST.Core
{
    /// <summary>
    /// 异步任务管理器抽象基类：维护全局 <see cref="instance"/>，由 <see cref="AsyncTaskManager"/> 具体实现任务的增删与每帧更新。
    /// </summary>
    public abstract class BaseAsyncTaskManager : IManager
    {
        /// <summary>当前全局管理器引用。</summary>
        protected static BaseAsyncTaskManager s_Instance;

        /// <summary>全局异步任务管理器。</summary>
        public static BaseAsyncTaskManager instance
        {
            get { return s_Instance; }
        }

        /// <summary>构造时注册为全局 <see cref="instance"/>。</summary>
        public BaseAsyncTaskManager()
        {
            s_Instance = this;
        }

        /// <summary>将异步任务加入队列，由 <see cref="IManager.DoUpdate"/> 驱动。</summary>
        /// <param name="asynctask">非空任务实例。</param>
        public abstract void AddTask(AsyncTask asynctask);

        /// <summary>默认无滞后帧逻辑。</summary>
        public override void DoLateUpdate() { }
    }
}
