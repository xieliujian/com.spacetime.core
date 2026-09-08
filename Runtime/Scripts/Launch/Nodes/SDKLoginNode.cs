namespace ST.Core.Launch
{
    /// <summary>SDK 登录占位节点。</summary>
    public sealed class SDKLoginNode : LaunchNode
    {
        /// <summary>创建 SDK 登录节点。</summary>
        public SDKLoginNode() : base("SDKLogin") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
