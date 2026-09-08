namespace ST.Core.Launch
{
    /// <summary>应用初始化占位节点。</summary>
    public sealed class AppInitNode : LaunchNode
    {
        /// <summary>创建应用初始化节点。</summary>
        public AppInitNode() : base("AppInit") { }

        /// <summary>完成占位初始化。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
