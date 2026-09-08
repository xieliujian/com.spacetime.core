using System;
using System.Collections.Generic;

namespace ST.Core.Launch
{
    /// <summary>
    /// 同时驱动多个子节点，并在全部成功后完成。
    /// </summary>
    public sealed class ParallelNode : LaunchNode
    {
        /// <summary>并行执行的子节点。</summary>
        readonly List<LaunchNode> m_Children = new List<LaunchNode>(8);

        /// <summary>
        /// 创建并行节点。
        /// </summary>
        /// <param name="name">非空的稳定节点名称。</param>
        public ParallelNode(string name) : base(name) { }

        /// <summary>
        /// 追加一个尚未执行的子节点。
        /// </summary>
        /// <param name="node">非空子节点。</param>
        public void Append(LaunchNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (this.state != LaunchNodeState.Ready)
            {
                throw new InvalidOperationException("Can not append after parallel node started.");
            }

            m_Children.Add(node);
        }

        /// <summary>按追加顺序启动全部子节点。</summary>
        protected override void OnStart()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                LaunchNode child = m_Children[i];
                child.Start();

                if (!child.isDone || child.isSucceeded)
                {
                    continue;
                }

                StopRunningChildren();
                Fail(child.error);
                return;
            }

            CheckComplete();
        }

        /// <summary>驱动所有运行中的子节点。</summary>
        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                if (m_Children[i].state == LaunchNodeState.Running)
                {
                    m_Children[i].DoUpdate();
                }
            }

            for (int i = 0; i < m_Children.Count; ++i)
            {
                LaunchNode child = m_Children[i];
                if (!child.isDone || child.isSucceeded)
                {
                    continue;
                }

                StopRunningChildren();
                Fail(child.error);
                return;
            }

            CheckComplete();
        }

        /// <summary>停止全部运行中的子节点。</summary>
        protected override void OnStop()
        {
            StopRunningChildren();
        }

        /// <summary>全部子节点成功时完成当前节点。</summary>
        void CheckComplete()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                if (!m_Children[i].isSucceeded)
                {
                    return;
                }
            }

            Complete();
        }

        /// <summary>停止全部仍在运行的子节点。</summary>
        void StopRunningChildren()
        {
            for (int i = 0; i < m_Children.Count; ++i)
            {
                if (m_Children[i].state == LaunchNodeState.Running)
                {
                    m_Children[i].Stop();
                }
            }
        }
    }
}
