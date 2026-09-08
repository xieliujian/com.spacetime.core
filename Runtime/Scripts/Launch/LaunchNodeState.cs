namespace ST.Core.Launch
{
    /// <summary>
    /// 启动节点生命周期状态。
    /// </summary>
    public enum LaunchNodeState : byte
    {
        /// <summary>尚未启动。</summary>
        Ready,

        /// <summary>正在执行。</summary>
        Running,

        /// <summary>执行成功。</summary>
        Succeeded,

        /// <summary>执行失败。</summary>
        Failed,

        /// <summary>已被停止。</summary>
        Stopped,
    }
}
