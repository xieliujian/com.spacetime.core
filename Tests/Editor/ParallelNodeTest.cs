using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 并行启动节点调度测试。
    /// </summary>
    public sealed class ParallelNodeTest
    {
        /// <summary>验证空并行节点立即成功。</summary>
        [Test]
        public void EmptyParallelSucceeds()
        {
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Start();
            Assert.IsTrue(parallel.isSucceeded);
        }

        /// <summary>验证全部子节点按追加顺序启动。</summary>
        [Test]
        public void ChildrenStartInOrder()
        {
            List<string> order = new List<string>();
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(new ProbeLaunchNode("First", order) { completeOnStart = true });
            parallel.Append(new ProbeLaunchNode("Second", order) { completeOnStart = true });
            parallel.Start();

            CollectionAssert.AreEqual(new[] { "First", "Second" }, order);
            Assert.IsTrue(parallel.isSucceeded);
        }

        /// <summary>验证并行节点等待全部子节点。</summary>
        [Test]
        public void ParallelWaitsForEveryChild()
        {
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(new ProbeLaunchNode("First") { completeOnStart = true });
            parallel.Append(new ProbeLaunchNode("Second") { completeOnUpdate = true });
            parallel.Start();
            Assert.AreEqual(LaunchNodeState.Running, parallel.state);
            parallel.DoUpdate();
            Assert.IsTrue(parallel.isSucceeded);
        }

        /// <summary>验证异步失败停止运行中的兄弟节点。</summary>
        [Test]
        public void AsynchronousFailureStopsSibling()
        {
            ProbeLaunchNode running = new ProbeLaunchNode("Running");
            ProbeLaunchNode failing = new ProbeLaunchNode("Failing")
            {
                failOnUpdate = true,
                failureMessage = "Parallel failed.",
            };
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(running);
            parallel.Append(failing);
            parallel.Start();
            parallel.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Failed, parallel.state);
            Assert.AreEqual("Parallel failed.", parallel.error);
            Assert.AreEqual(LaunchNodeState.Stopped, running.state);
        }

        /// <summary>验证同步失败阻止后续节点启动。</summary>
        [Test]
        public void SynchronousFailurePreventsLaterStart()
        {
            ProbeLaunchNode running = new ProbeLaunchNode("Running");
            ProbeLaunchNode failing = new ProbeLaunchNode("Failing") { failOnStart = true };
            ProbeLaunchNode later = new ProbeLaunchNode("Later") { completeOnStart = true };
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(running);
            parallel.Append(failing);
            parallel.Append(later);
            parallel.Start();

            Assert.AreEqual(LaunchNodeState.Failed, parallel.state);
            Assert.AreEqual(LaunchNodeState.Stopped, running.state);
            Assert.AreEqual(0, later.startCount);
        }

        /// <summary>验证父节点停止传播到全部运行节点。</summary>
        [Test]
        public void StopPropagatesToRunningChildren()
        {
            ProbeLaunchNode first = new ProbeLaunchNode("First");
            ProbeLaunchNode second = new ProbeLaunchNode("Second");
            ParallelNode parallel = new ParallelNode("Parallel");
            parallel.Append(first);
            parallel.Append(second);
            parallel.Start();
            parallel.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, first.state);
            Assert.AreEqual(LaunchNodeState.Stopped, second.state);
        }

        /// <summary>验证追加参数和状态检查。</summary>
        [Test]
        public void AppendValidatesInputAndState()
        {
            ParallelNode parallel = new ParallelNode("Parallel");
            Assert.Throws<ArgumentNullException>(() => parallel.Append(null));
            parallel.Start();
            Assert.Throws<InvalidOperationException>(
                () => parallel.Append(new ProbeLaunchNode("Late")));
        }
    }
}
