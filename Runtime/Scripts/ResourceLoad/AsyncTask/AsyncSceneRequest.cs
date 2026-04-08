using UnityEngine;
using UnityEngine.SceneManagement;

namespace ST.Core
{
    /// <summary>运行时使用 <see cref="SceneManager.LoadSceneAsync"/> 异步加载场景。</summary>
    public class AsyncSceneRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_ResName;
        AsyncOperation m_Request;

        /// <inheritdoc />
        public override float progress
        {
            get { return m_Request != null ? m_Request.progress : 0f; }
        }

        /// <param name="bundle">用于触发依赖包加载（逻辑上关联）。</param>
        /// <param name="resname">场景名称（与 Build Settings / 包内场景名一致）。</param>
        /// <param name="progress">可选进度回调。</param>
        /// <param name="complete">完成时传入 <see cref="SceneManager.GetSceneByName"/> 结果。</param>
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
