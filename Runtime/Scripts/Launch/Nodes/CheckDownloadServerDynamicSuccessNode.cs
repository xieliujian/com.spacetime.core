namespace ST.Core.Launch
{
    /// <summary>服务器动态配置下载结果检查占位节点。</summary>
    public sealed class CheckDownloadServerDynamicSuccessNode : LaunchNode
    {
        /// <summary>创建服务器动态配置下载结果检查节点。</summary>
        public CheckDownloadServerDynamicSuccessNode() : base("CheckDownloadServerDynamicSuccess") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
