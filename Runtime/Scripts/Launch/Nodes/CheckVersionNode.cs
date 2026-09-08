namespace ST.Core.Launch
{
    /// <summary>版本检查占位节点。</summary>
    public sealed class CheckVersionNode : LaunchNode
    {
        /// <summary>创建版本检查节点。</summary>
        public CheckVersionNode() : base("CheckVersion") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
