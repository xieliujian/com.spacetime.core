using System;
using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动节点基类，统一管理执行状态与异常边界。
    /// </summary>
    public abstract class LaunchNode
    {
        /// <summary>节点稳定名称。</summary>
        readonly string m_Name;

        /// <summary>当前生命周期状态。</summary>
        LaunchNodeState m_State;

        /// <summary>失败信息；无错误时为空字符串。</summary>
        string m_Error = string.Empty;

        /// <summary>节点稳定名称。</summary>
        public string name { get { return m_Name; } }

        /// <summary>当前生命周期状态。</summary>
        public LaunchNodeState state { get { return m_State; } }

        /// <summary>失败信息；无错误时为空字符串。</summary>
        public string error { get { return m_Error; } }

        /// <summary>节点是否已经进入终态。</summary>
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

        /// <summary>节点是否成功完成。</summary>
        public bool isSucceeded { get { return m_State == LaunchNodeState.Succeeded; } }

        /// <summary>
        /// 创建指定名称的启动节点。
        /// </summary>
        /// <param name="name">非空的稳定节点名称。</param>
        protected LaunchNode(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Launch node name can not be null or empty.", nameof(name));
            }

            m_Name = name;
        }

        /// <summary>从就绪状态启动节点；重复调用会被忽略。</summary>
        public void Start()
        {
            if (m_State != LaunchNodeState.Ready)
            {
                return;
            }

            m_State = LaunchNodeState.Running;

            try
            {
                OnStart();
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        /// <summary>驱动运行中的节点执行一帧。</summary>
        public void DoUpdate()
        {
            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            try
            {
                OnUpdate();
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        /// <summary>停止就绪或运行中的节点；终态节点保持原状态。</summary>
        public void Stop()
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

            try
            {
                OnStop();
            }
            catch (Exception exception)
            {
                Logger.LogException(exception);
            }

            m_State = LaunchNodeState.Stopped;
        }

        /// <summary>派生节点启动逻辑。</summary>
        protected abstract void OnStart();

        /// <summary>派生节点逐帧逻辑。</summary>
        protected virtual void OnUpdate() { }

        /// <summary>派生节点停止逻辑。</summary>
        protected virtual void OnStop() { }

        /// <summary>将运行中的节点标记为成功。</summary>
        protected void Complete()
        {
            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            m_State = LaunchNodeState.Succeeded;
        }

        /// <summary>
        /// 将运行中的节点标记为失败。
        /// </summary>
        /// <param name="error">失败信息；为空时使用默认信息。</param>
        protected void Fail(string error)
        {
            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            m_Error = string.IsNullOrEmpty(error) ? "Launch node failed." : error;
            m_State = LaunchNodeState.Failed;
        }
    }
}
