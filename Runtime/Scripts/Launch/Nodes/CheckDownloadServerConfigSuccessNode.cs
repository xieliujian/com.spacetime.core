namespace ST.Core.Launch
{
    /// <summary>服务器配置下载结果检查占位节点。</summary>
    public sealed class CheckDownloadServerConfigSuccessNode : LaunchNode
    {
        /// <summary>创建服务器配置下载结果检查节点。</summary>
        public CheckDownloadServerConfigSuccessNode() : base("CheckDownloadServerConfigSuccess") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
