namespace ST.Core.Launch
{
    /// <summary>着色器预编译占位节点。</summary>
    public sealed class CompileShaderNode : LaunchNode
    {
        /// <summary>创建着色器预编译节点。</summary>
        public CompileShaderNode() : base("CompileShader") { }

        /// <summary>完成占位操作。</summary>
        protected override void OnStart()
        {
            Complete();
        }
    }
}
