namespace ST.Core.Launch
{
    /// <summary>资源更新占位节点。</summary>
    public sealed class UpdateNode : LaunchNode
    {
        /// <summary>创建资源更新节点。</summary>
        public UpdateNode() : base("Update") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
