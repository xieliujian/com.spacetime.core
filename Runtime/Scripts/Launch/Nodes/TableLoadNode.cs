namespace ST.Core.Launch
{
    /// <summary>数据表加载占位节点。</summary>
    public sealed class TableLoadNode : LaunchNode
    {
        /// <summary>创建数据表加载节点。</summary>
        public TableLoadNode() : base("TableLoad") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
