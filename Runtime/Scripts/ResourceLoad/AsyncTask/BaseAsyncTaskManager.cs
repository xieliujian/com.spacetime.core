namespace ST.Core
{
    public abstract class BaseAsyncTaskManager : IManager
    {
        protected static BaseAsyncTaskManager s_Instance;

        public static BaseAsyncTaskManager instance
        {
            get { return s_Instance; }
        }

        public BaseAsyncTaskManager()
        {
            s_Instance = this;
        }

        public abstract void AddTask(AsyncTask asynctask);

        public override void DoLateUpdate() { }
    }
}
