namespace ST.Core.Launch
{
    /// <summary>启动阶段垃圾回收占位节点。</summary>
    public sealed class LaunchGCNode : LaunchNode
    {
        /// <summary>创建启动阶段垃圾回收节点。</summary>
        public LaunchGCNode() : base("LaunchGC") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
