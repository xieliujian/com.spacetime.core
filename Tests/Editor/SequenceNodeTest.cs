using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 顺序启动节点调度测试。
    /// </summary>
    public sealed class SequenceNodeTest
    {
        /// <summary>验证空顺序节点立即成功。</summary>
        [Test]
        public void EmptySequenceSucceeds()
        {
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Start();
            Assert.IsTrue(sequence.isSucceeded);
        }

        /// <summary>验证同步节点按顺序执行。</summary>
        [Test]
        public void SynchronousChildrenRunInOrder()
        {
            List<string> order = new List<string>();
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(new ProbeLaunchNode("First", order) { completeOnStart = true });
            sequence.Append(new ProbeLaunchNode("Second", order) { completeOnStart = true });

            sequence.Start();

            CollectionAssert.AreEqual(new[] { "First", "Second" }, order);
            Assert.IsTrue(sequence.isSucceeded);
        }

        /// <summary>验证运行节点阻塞后续节点。</summary>
        [Test]
        public void RunningChildBlocksUntilUpdate()
        {
            ProbeLaunchNode first = new ProbeLaunchNode("First") { completeOnUpdate = true };
            ProbeLaunchNode second = new ProbeLaunchNode("Second") { completeOnStart = true };
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(first);
            sequence.Append(second);

            sequence.Start();
            Assert.AreEqual(0, second.startCount);
            sequence.DoUpdate();

            Assert.AreEqual(1, second.startCount);
            Assert.IsTrue(sequence.isSucceeded);
        }

        /// <summary>验证异步失败阻断后续节点并保留错误。</summary>
        [Test]
        public void AsynchronousFailureStopsSequence()
        {
            ProbeLaunchNode first = new ProbeLaunchNode("First")
            {
                failOnUpdate = true,
                failureMessage = "First failed.",
            };
            ProbeLaunchNode second = new ProbeLaunchNode("Second") { completeOnStart = true };
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(first);
            sequence.Append(second);

            sequence.Start();
            sequence.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Failed, sequence.state);
            Assert.AreEqual("First failed.", sequence.error);
            Assert.AreEqual(0, second.startCount);
        }

        /// <summary>验证停止传播到当前运行节点。</summary>
        [Test]
        public void StopPropagatesToRunningChild()
        {
            ProbeLaunchNode child = new ProbeLaunchNode("Child");
            SequenceNode sequence = new SequenceNode("Sequence");
            sequence.Append(child);
            sequence.Start();
            sequence.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, child.state);
            Assert.AreEqual(1, child.stopCount);
        }

        /// <summary>验证追加参数和状态检查。</summary>
        [Test]
        public void AppendValidatesInputAndState()
        {
            SequenceNode sequence = new SequenceNode("Sequence");
            Assert.Throws<ArgumentNullException>(() => sequence.Append(null));
            sequence.Start();
            Assert.Throws<InvalidOperationException>(
                () => sequence.Append(new ProbeLaunchNode("Late")));
        }
    }
}
