using UnityEngine;

namespace ST.Core
{
    /// <summary>使用 <see cref="FilePathHelper"/> 解析磁盘路径并 <see cref="AssetBundle.LoadFromFileAsync"/> 异步加载包文件。</summary>
    public class AsyncBundleRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_Path;
        FilePathHelper m_FilePathHelper;
        AssetBundleCreateRequest m_CreateRequest;

        /// <inheritdoc />
        public override float progress
        {
            get { return m_CreateRequest != null ? m_CreateRequest.progress : 0f; }
        }

        /// <summary>加载完成后的 <see cref="AssetBundle"/>，未完成时可能为 <c>null</c>。</summary>
        public AssetBundle assetBundle
        {
            get { return m_CreateRequest != null ? m_CreateRequest.assetBundle : null; }
        }

        /// <param name="bundle">所属逻辑包，用于依赖是否已加载判断。</param>
        /// <param name="path">Bundle 相对路径（不含 StreamingAssets 根）。</param>
        /// <param name="filePathHelper">路径拼接工具。</param>
        /// <param name="callback">完成时回传 <see cref="assetBundle"/>。</param>
        public AsyncBundleRequest(Bundle bundle, string path, FilePathHelper filePathHelper, ResourceLoadComplete callback)
        {
            m_Bundle = bundle;
            m_Path = path;
            m_FilePathHelper = filePathHelper;
            m_CompleteEvent = callback;
        }

        protected override void OnEnd()
        {
            m_CompleteEvent?.Invoke(assetBundle);
        }

        protected override void OnStart()
        {
            var fullpath = m_FilePathHelper.GetBundleFullPath(m_Path);
            m_CreateRequest = AssetBundle.LoadFromFileAsync(fullpath);
        }

        protected override ETaskState OnUpdate()
        {
            bool isdone = m_CreateRequest != null && m_Bundle != null
                          && m_CreateRequest.isDone && m_Bundle.dependIsLoaded;
            return isdone ? ETaskState.Completed : ETaskState.Running;
        }
    }
}
