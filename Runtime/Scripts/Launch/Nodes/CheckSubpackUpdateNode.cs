namespace ST.Core.Launch
{
    /// <summary>分包更新检查占位节点。</summary>
    public sealed class CheckSubpackUpdateNode : LaunchNode
    {
        /// <summary>创建分包更新检查节点。</summary>
        public CheckSubpackUpdateNode() : base("CheckSubpackUpdate") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
