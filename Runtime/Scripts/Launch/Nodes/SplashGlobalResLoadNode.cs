namespace ST.Core.Launch
{
    /// <summary>首屏全局资源加载占位节点。</summary>
    public sealed class SplashGlobalResLoadNode : LaunchNode
    {
        /// <summary>创建首屏全局资源加载节点。</summary>
        public SplashGlobalResLoadNode() : base("SplashGlobalResLoad") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
