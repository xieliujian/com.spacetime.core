using System;
using System.Collections.Generic;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 可配置的启动节点测试替身。
    /// </summary>
    public sealed class ProbeLaunchNode : LaunchNode
    {
        /// <summary>可选的执行顺序记录容器。</summary>
        readonly List<string> m_ExecutionOrder;

        /// <summary>启动调用次数。</summary>
        public int startCount { get; private set; }

        /// <summary>更新调用次数。</summary>
        public int updateCount { get; private set; }

        /// <summary>停止调用次数。</summary>
        public int stopCount { get; private set; }

        /// <summary>是否在启动时成功。</summary>
        public bool completeOnStart { get; set; }

        /// <summary>是否在启动时失败。</summary>
        public bool failOnStart { get; set; }

        /// <summary>是否在更新时成功。</summary>
        public bool completeOnUpdate { get; set; }

        /// <summary>是否在更新时失败。</summary>
        public bool failOnUpdate { get; set; }

        /// <summary>是否在启动时抛出异常。</summary>
        public bool throwOnStart { get; set; }

        /// <summary>是否在更新时抛出异常。</summary>
        public bool throwOnUpdate { get; set; }

        /// <summary>是否在停止时抛出异常。</summary>
        public bool throwOnStop { get; set; }

        /// <summary>失败时传入的错误信息。</summary>
        public string failureMessage { get; set; } = "Probe failure.";

        /// <summary>
        /// 创建测试节点。
        /// </summary>
        /// <param name="name">节点名称。</param>
        /// <param name="executionOrder">可选执行顺序容器。</param>
        public ProbeLaunchNode(string name, List<string> executionOrder = null) : base(name)
        {
            m_ExecutionOrder = executionOrder;
        }

        /// <summary>执行配置的启动行为。</summary>
        protected override void OnStart()
        {
            ++this.startCount;
            if (m_ExecutionOrder != null)
            {
                m_ExecutionOrder.Add(this.name);
            }

            if (this.throwOnStart)
            {
                throw new InvalidOperationException("Start exception.");
            }

            if (this.failOnStart)
            {
                Fail(this.failureMessage);
                return;
            }

            if (this.completeOnStart)
            {
                Complete();
            }
        }

        /// <summary>执行配置的更新行为。</summary>
        protected override void OnUpdate()
        {
            ++this.updateCount;
            if (this.throwOnUpdate)
            {
                throw new InvalidOperationException("Update exception.");
            }

            if (this.failOnUpdate)
            {
                Fail(this.failureMessage);
                return;
            }

            if (this.completeOnUpdate)
            {
                Complete();
            }
        }

        /// <summary>执行配置的停止行为。</summary>
        protected override void OnStop()
        {
            ++this.stopCount;
            if (this.throwOnStop)
            {
                throw new InvalidOperationException("Stop exception.");
            }
        }
    }
}
