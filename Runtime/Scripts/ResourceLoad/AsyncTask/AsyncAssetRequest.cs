using System;
using UnityEngine;

namespace ST.Core
{
    public class AsyncAssetRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_ResName;
        AssetBundleRequest m_Request;
        Type m_Type;

        public override float progress
        {
            get { return m_Request != null ? m_Request.progress : 0f; }
        }

        public AsyncAssetRequest(Bundle bundle, string resname, Type type, ResourceLoadComplete callback)
        {
            m_Bundle = bundle;
            m_ResName = resname;
            m_CompleteEvent = callback;
            m_Type = type;
        }

        protected override void OnEnd()
        {
            object val = m_Request != null ? m_Request.asset : null;
            m_CompleteEvent?.Invoke(val);
        }

        protected override void OnStart()
        {
            m_Request = m_Bundle.LoadAssetAsyncFromBundle(m_ResName, m_Type);
        }

        protected override ETaskState OnUpdate()
        {
            m_ProgressEvent?.Invoke(progress);
            bool isdone = m_Request != null && m_Request.isDone;
            return isdone ? ETaskState.Completed : ETaskState.Running;
        }
    }
}
