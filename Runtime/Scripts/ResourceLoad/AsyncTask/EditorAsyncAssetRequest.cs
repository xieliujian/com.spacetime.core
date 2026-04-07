using System;
using UnityEngine;

namespace ST.Core
{
    public class EditorAsyncAssetRequest : AsyncTask
    {
        public override float progress { get { return 1.0f; } }

        public EditorAsyncAssetRequest(string respath, Type type)
        {
#if UNITY_EDITOR
            m_Asset = UnityEditor.AssetDatabase.LoadAssetAtPath(respath, type);
#endif
        }

        protected override void OnEnd()
        {
            m_CompleteEvent?.Invoke(m_Asset);
        }

        protected override void OnStart() { }

        protected override ETaskState OnUpdate()
        {
            return ETaskState.Completed;
        }
    }
}
