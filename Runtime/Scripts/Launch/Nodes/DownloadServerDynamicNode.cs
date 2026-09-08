namespace ST.Core.Launch
{
    /// <summary>服务器动态配置下载占位节点。</summary>
    public sealed class DownloadServerDynamicNode : LaunchNode
    {
        /// <summary>创建服务器动态配置下载节点。</summary>
        public DownloadServerDynamicNode() : base("DownloadServerDynamic") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
