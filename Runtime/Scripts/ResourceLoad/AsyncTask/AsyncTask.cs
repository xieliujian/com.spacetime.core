namespace ST.Core
{
    public abstract class AsyncTask
    {
        public enum ETaskState
        {
            NotStart,
            Running,
            Completed,
            End,
        }

        protected ETaskState m_State = ETaskState.NotStart;
        protected object m_Asset = null;
        protected ResourceLoadProgress m_ProgressEvent;
        protected ResourceLoadComplete m_CompleteEvent;

        public abstract float progress { get; }

        public ResourceLoadProgress progressEvent
        {
            get { return m_ProgressEvent; }
            set { m_ProgressEvent = value; }
        }

        public ResourceLoadComplete completeEvent
        {
            get { return m_CompleteEvent; }
            set { m_CompleteEvent = value; }
        }

        public bool isEnd
        {
            get { return m_State == ETaskState.End; }
        }

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

        protected abstract void OnStart();
        protected abstract ETaskState OnUpdate();
        protected abstract void OnEnd();
    }
}
