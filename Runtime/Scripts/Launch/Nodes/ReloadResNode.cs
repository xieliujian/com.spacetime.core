namespace ST.Core.Launch
{
    /// <summary>资源重载占位节点。</summary>
    public sealed class ReloadResNode : LaunchNode
    {
        /// <summary>创建资源重载节点。</summary>
        public ReloadResNode() : base("ReloadRes") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
