namespace ST.Core.Launch
{
    /// <summary>等待一个 Unity 更新帧。</summary>
    public sealed class WaitFrameNode : LaunchNode
    {
        /// <summary>创建等待帧节点。</summary>
        public WaitFrameNode() : base("WaitFrame") { }

        /// <summary>启动时保持运行，等待下一帧。</summary>
        protected override void OnStart() { }

        /// <summary>首个更新帧完成节点。</summary>
        protected override void OnUpdate()
        {
            Complete();
        }
    }
}
