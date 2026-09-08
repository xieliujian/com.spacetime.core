using System;
using System.Collections.Generic;

namespace ST.Core.Launch
{
    /// <summary>
    /// 按追加顺序逐个执行子节点。
    /// </summary>
    public sealed class SequenceNode : LaunchNode
    {
        /// <summary>按执行顺序保存的子节点。</summary>
        readonly List<LaunchNode> m_Children = new List<LaunchNode>(16);

        /// <summary>当前正在处理的子节点索引。</summary>
        int m_CurrentIndex;

        /// <summary>
        /// 创建顺序节点。
        /// </summary>
        /// <param name="name">非空的稳定节点名称。</param>
        public SequenceNode(string name) : base(name) { }

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
                throw new InvalidOperationException("Can not append after sequence started.");
            }

            m_Children.Add(node);
        }

        /// <summary>启动并推进第一个子节点。</summary>
        protected override void OnStart()
        {
            Advance();
        }

        /// <summary>驱动当前子节点并继续推进。</summary>
        protected override void OnUpdate()
        {
            if (m_CurrentIndex < m_Children.Count)
            {
                m_Children[m_CurrentIndex].DoUpdate();
            }

            Advance();
        }

        /// <summary>停止当前仍在运行的子节点。</summary>
        protected override void OnStop()
        {
            if (m_CurrentIndex >= m_Children.Count)
            {
                return;
            }

            LaunchNode currentNode = m_Children[m_CurrentIndex];
            if (currentNode.state == LaunchNodeState.Running)
            {
                currentNode.Stop();
            }
        }

        /// <summary>推进到首个运行、失败或末尾节点。</summary>
        void Advance()
        {
            while (m_CurrentIndex < m_Children.Count)
            {
                LaunchNode currentNode = m_Children[m_CurrentIndex];
                if (currentNode.state == LaunchNodeState.Ready)
                {
                    currentNode.Start();
                }

                if (currentNode.state == LaunchNodeState.Running)
                {
                    return;
                }

                if (!currentNode.isSucceeded)
                {
                    Fail(currentNode.error);
                    return;
                }

                ++m_CurrentIndex;
            }

            Complete();
        }
    }
}
