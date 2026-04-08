namespace ST.Core
{
    /// <summary>
    /// 资源加载异步任务基类：由 <see cref="AsyncTaskManager"/> 每帧驱动 <see cref="Update"/>，经 <see cref="OnStart"/> / <see cref="OnUpdate"/> / <see cref="OnEnd"/> 三阶段完成。
    /// </summary>
    public abstract class AsyncTask
    {
        /// <summary>任务生命周期状态。</summary>
        public enum ETaskState
        {
            /// <summary>尚未开始。</summary>
            NotStart,
            /// <summary>执行中。</summary>
            Running,
            /// <summary>已完成等待收尾。</summary>
            Completed,
            /// <summary>已结束并从管理器移除。</summary>
            End,
        }

        /// <summary>当前状态。</summary>
        protected ETaskState m_State = ETaskState.NotStart;
        /// <summary>部分任务在构造或加载阶段缓存的结果引用。</summary>
        protected object m_Asset = null;
        /// <summary>进度回调。</summary>
        protected ResourceLoadProgress m_ProgressEvent;
        /// <summary>完成回调。</summary>
        protected ResourceLoadComplete m_CompleteEvent;

        /// <summary>0~1 进度，由具体任务映射 Unity 异步操作。</summary>
        public abstract float progress { get; }

        /// <summary>可选进度事件（可在创建后赋值）。</summary>
        public ResourceLoadProgress progressEvent
        {
            get { return m_ProgressEvent; }
            set { m_ProgressEvent = value; }
        }

        /// <summary>完成事件（可在创建后赋值）。</summary>
        public ResourceLoadComplete completeEvent
        {
            get { return m_CompleteEvent; }
            set { m_CompleteEvent = value; }
        }

        /// <summary>是否已进入 <see cref="ETaskState.End"/>。</summary>
        public bool isEnd
        {
            get { return m_State == ETaskState.End; }
        }

        /// <summary>驱动状态机：首次调用进入 Running 并 <see cref="OnStart"/>，之后每帧 <see cref="OnUpdate"/>，Completed 时转 End 并 <see cref="OnEnd"/>。</summary>
        public void Update()
        {
            if (m_State == ETaskState.NotStart)
            {
                m_State = ETaskState.Running;
                OnStart();
            }

            m_State = OnUpdate();

            if (m_State == ETaskState.Completed)
            {
                m_State = ETaskState.End;
                OnEnd();
            }
        }

        /// <summary>任务开始时调用一次。</summary>
        protected abstract void OnStart();
        /// <summary>每帧返回下一状态。</summary>
        protected abstract ETaskState OnUpdate();
        /// <summary>进入 <see cref="ETaskState.End"/> 前调用一次。</summary>
        protected abstract void OnEnd();
    }
}
