#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ST.Core
{
    public class EditorAsyncSceneRequest : AsyncTask
    {
        AsyncOperation m_Operation;
        string m_ScenePath;

        public override float progress
        {
            get
            {
                if (m_Operation == null || m_Operation.isDone)
                    return 1f;
                return m_Operation.progress;
            }
        }

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
