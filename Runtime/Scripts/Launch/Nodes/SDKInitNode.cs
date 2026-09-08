namespace ST.Core.Launch
{
    /// <summary>SDK 初始化占位节点。</summary>
    public sealed class SDKInitNode : LaunchNode
    {
        /// <summary>创建 SDK 初始化节点。</summary>
        public SDKInitNode() : base("SDKInit") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
