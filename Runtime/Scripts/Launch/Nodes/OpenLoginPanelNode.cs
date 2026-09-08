namespace ST.Core.Launch
{
    /// <summary>登录界面打开占位节点。</summary>
    public sealed class OpenLoginPanelNode : LaunchNode
    {
        /// <summary>创建登录界面打开节点。</summary>
        public OpenLoginPanelNode() : base("OpenLoginPanel") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
