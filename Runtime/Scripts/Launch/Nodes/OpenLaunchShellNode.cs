namespace ST.Core.Launch
{
    /// <summary>打开启动界面占位节点。</summary>
    public sealed class OpenLaunchShellNode : LaunchNode
    {
        /// <summary>创建打开启动界面节点。</summary>
        public OpenLaunchShellNode() : base("OpenLaunchShell") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
