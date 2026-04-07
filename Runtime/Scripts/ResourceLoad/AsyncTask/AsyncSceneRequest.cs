using UnityEngine;
using UnityEngine.SceneManagement;

namespace ST.Core
{
    public class AsyncSceneRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_ResName;
        AsyncOperation m_Request;

        public override float progress
        {
            get { return m_Request != null ? m_Request.progress : 0f; }
        }

        public AsyncSceneRequest(Bundle bundle, string resname, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            m_Bundle = bundle;
            m_ResName = resname;
            m_ProgressEvent = progress;
            m_CompleteEvent = complete;
        }

        protected override void OnEnd()
        {
            object val = SceneManager.GetSceneByName(m_ResName);
            m_CompleteEvent?.Invoke(val);
        }

        protected override void OnStart()
        {
            m_Request = SceneManager.LoadSceneAsync(m_ResName, LoadSceneMode.Single);
        }

        protected override ETaskState OnUpdate()
        {
            m_ProgressEvent?.Invoke(progress);
            bool isdone = m_Request != null && m_Request.isDone;
            return isdone ? ETaskState.Completed : ETaskState.Running;
        }
    }
}
