using System;
using UnityEngine;

namespace ST.Core
{
    /// <summary>从已加载的 <see cref="Bundle"/> 中异步请求单个资源（<see cref="AssetBundleRequest"/>）。</summary>
    public class AsyncAssetRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_ResName;
        AssetBundleRequest m_Request;
        Type m_Type;

        /// <inheritdoc />
        public override float progress
        {
            get { return m_Request != null ? m_Request.progress : 0f; }
        }

        /// <summary>创建资产异步加载任务。</summary>
        /// <param name="bundle">已关联 <see cref="AssetBundle"/> 的包封装。</param>
        /// <param name="resname">Bundle 内资源名。</param>
        /// <param name="type">资源类型。</param>
        /// <param name="callback">完成回调。</param>
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
