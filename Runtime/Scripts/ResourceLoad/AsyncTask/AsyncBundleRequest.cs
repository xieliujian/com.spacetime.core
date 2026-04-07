using UnityEngine;

namespace ST.Core
{
    public class AsyncBundleRequest : AsyncTask
    {
        Bundle m_Bundle;
        string m_Path;
        FilePathHelper m_FilePathHelper;
        AssetBundleCreateRequest m_CreateRequest;

        public override float progress
        {
            get { return m_CreateRequest != null ? m_CreateRequest.progress : 0f; }
        }

        public AssetBundle assetBundle
        {
            get { return m_CreateRequest != null ? m_CreateRequest.assetBundle : null; }
        }

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
