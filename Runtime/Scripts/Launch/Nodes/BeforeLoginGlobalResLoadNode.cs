namespace ST.Core.Launch
{
    /// <summary>登录前全局资源加载占位节点。</summary>
    public sealed class BeforeLoginGlobalResLoadNode : LaunchNode
    {
        /// <summary>创建登录前全局资源加载节点。</summary>
        public BeforeLoginGlobalResLoadNode() : base("BeforeLoginGlobalResLoad") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
