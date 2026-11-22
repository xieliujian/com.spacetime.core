using System;
using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动流程总入口，负责构建标准节点树、每帧驱动它并同步终态。
    /// </summary>
    public sealed partial class LaunchModule : IManager
    {
        /// <summary>IManager 初始化入口；节点树在 Launch 时才创建，这里不做事先准备。</summary>
        public override void DoInit()
        {
        }

        /// <summary>仅在运行状态驱动节点树，并在每帧结束后同步根节点终态。</summary>
        public override void DoUpdate()
        {
            if (m_State != LaunchNodeState.Running || m_RootNode == null)
            {
                return;
            }

            m_RootNode.DoUpdate();
            SyncRootState();
        }

        /// <summary>启动树没有滞后一帧的逻辑，保持空实现。</summary>
        public override void DoLateUpdate()
        {
        }

        /// <summary>按当前状态停止流程；已进入终态的流程不会被改写。</summary>
        public override void DoClose()
        {
            if (m_State == LaunchNodeState.Ready)
            {
                m_State = LaunchNodeState.Stopped;
                return;
            }

            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            if (m_RootNode != null)
            {
                m_RootNode.Stop();
            }

            m_State = LaunchNodeState.Stopped;
        }

        /// <summary>仅在 Ready 状态构建并启动节点树，重复调用直接忽略；构建异常会把模块置为 Failed。</summary>
        public void Launch()
        {
            if (m_State != LaunchNodeState.Ready)
            {
                return;
            }

            try
            {
                m_RootNode = CreateRootNode();
                m_State = LaunchNodeState.Running;
                m_RootNode.Start();
                SyncRootState();
            }
            catch (Exception exception)
            {
                m_Error = exception.ToString();
                m_State = LaunchNodeState.Failed;
                m_IsFailureLogged = true;
                Logger.LogException(exception);
            }
        }
    }
}
