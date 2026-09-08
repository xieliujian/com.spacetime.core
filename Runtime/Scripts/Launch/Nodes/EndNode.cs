namespace ST.Core.Launch
{
    /// <summary>启动流程结束占位节点。</summary>
    public sealed class EndNode : LaunchNode
    {
        /// <summary>创建启动流程结束节点。</summary>
        public EndNode() : base("End") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
