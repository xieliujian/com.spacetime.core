using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动模块定义部分，集中存放运行状态与对外状态属性。
    /// </summary>
    public sealed partial class LaunchModule
    {
        /// <summary>标准启动根顺序节点；Launch 之前为 null。</summary>
        LaunchNode m_RootNode;

        /// <summary>启动流程当前所处的生命周期状态。</summary>
        LaunchNodeState m_State;

        /// <summary>启动失败信息；无错误时为空字符串。</summary>
        string m_Error = string.Empty;

        /// <summary>根节点失败是否已经写入日志，避免每帧重复记录。</summary>
        bool m_IsFailureLogged;

        /// <summary>对外暴露的当前状态，供调用方轮询启动进度。</summary>
        public LaunchNodeState state
        {
            get
            {
                return m_State;
            }
        }

        /// <summary>对外暴露的失败信息；流程成功或尚未失败时为空字符串。</summary>
        public string error
        {
            get
            {
                return m_Error;
            }
        }

        /// <summary>启动流程是否已经进入成功、失败或停止的终态。</summary>
        public bool isDone
        {
            get
            {
                bool isTerminal = m_State == LaunchNodeState.Succeeded
                                  || m_State == LaunchNodeState.Failed
                                  || m_State == LaunchNodeState.Stopped;
                return isTerminal;
            }
        }

        /// <summary>启动流程是否以成功状态结束。</summary>
        public bool isSucceeded
        {
            get
            {
                return m_State == LaunchNodeState.Succeeded;
            }
        }

        /// <summary>把根节点终态同步到模块状态；失败信息只写入一次日志。</summary>
        void SyncRootState()
        {
            if (m_RootNode == null || !m_RootNode.isDone)
            {
                return;
            }

            m_State = m_RootNode.state;
            m_Error = m_RootNode.error;

            if (m_State != LaunchNodeState.Failed || m_IsFailureLogged)
            {
                return;
            }

            m_IsFailureLogged = true;
            STLogger.LogError(m_Error);
        }
    }
}