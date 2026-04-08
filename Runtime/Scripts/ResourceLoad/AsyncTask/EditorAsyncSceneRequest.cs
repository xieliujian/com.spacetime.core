#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ST.Core
{
    /// <summary>编辑器 Play 模式下通过 <see cref="EditorSceneManager.LoadSceneAsyncInPlayMode"/> 异步加载场景。</summary>
    public class EditorAsyncSceneRequest : AsyncTask
    {
        AsyncOperation m_Operation;
        string m_ScenePath;

        /// <inheritdoc />
        public override float progress
        {
            get
            {
                if (m_Operation == null || m_Operation.isDone)
                    return 1f;
                return m_Operation.progress;
            }
        }

        /// <param name="scenepath">场景资源路径（Assets/...）。</param>
        public EditorAsyncSceneRequest(string scenepath)
        {
            m_ScenePath = scenepath;
            var param = new LoadSceneParameters(LoadSceneMode.Single);
            m_Operation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenepath, param);
        }

        protected override void OnEnd()
        {
            var asset = EditorSceneManager.GetSceneByPath(m_ScenePath);
            m_CompleteEvent?.Invoke(asset);
        }

        protected override void OnStart() { }

        protected override ETaskState OnUpdate()
        {
            m_ProgressEvent?.Invoke(progress);
            bool iscomplete = m_Operation != null && m_Operation.isDone;
            return iscomplete ? ETaskState.Completed : ETaskState.Running;
        }
    }
}

#endif
