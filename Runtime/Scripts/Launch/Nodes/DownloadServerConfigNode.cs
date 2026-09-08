namespace ST.Core.Launch
{
    /// <summary>服务器配置下载占位节点。</summary>
    public sealed class DownloadServerConfigNode : LaunchNode
    {
        /// <summary>创建服务器配置下载节点。</summary>
        public DownloadServerConfigNode() : base("DownloadServerConfig") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
