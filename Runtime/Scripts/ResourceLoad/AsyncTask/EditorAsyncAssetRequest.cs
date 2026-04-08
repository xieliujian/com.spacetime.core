using System;
using UnityEngine;

namespace ST.Core
{
    /// <summary>编辑器下在构造时同步 <c>LoadAssetAtPath</c>，通过任务管线统一回调（单帧完成）。</summary>
    public class EditorAsyncAssetRequest : AsyncTask
    {
        /// <inheritdoc />
        public override float progress { get { return 1.0f; } }

        /// <param name="respath">工程内资源路径。</param>
        /// <param name="type">资产类型。</param>
        public EditorAsyncAssetRequest(string respath, Type type)
        {
#if UNITY_EDITOR
            m_Asset = UnityEditor.AssetDatabase.LoadAssetAtPath(respath, type);
#endif
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            m_CompleteEvent?.Invoke(m_Asset);
        }

        /// <inheritdoc />
        protected override void OnStart() { }

        /// <inheritdoc />
        protected override ETaskState OnUpdate()
        {
            return ETaskState.Completed;
        }
    }
}
