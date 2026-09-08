namespace ST.Core.Launch
{
    /// <summary>SDK 初始化结果检查占位节点。</summary>
    public sealed class SDKInitCheckNode : LaunchNode
    {
        /// <summary>创建 SDK 初始化结果检查节点。</summary>
        public SDKInitCheckNode() : base("SDKInitCheck") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
