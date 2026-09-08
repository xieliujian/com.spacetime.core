using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 启动节点基础状态测试。
    /// </summary>
    public sealed class LaunchNodeTest
    {
        /// <summary>验证空名称被拒绝。</summary>
        [Test]
        public void ConstructorRejectsEmptyName()
        {
            Assert.Throws<ArgumentException>(() => new ProbeLaunchNode(null));
            Assert.Throws<ArgumentException>(() => new ProbeLaunchNode(string.Empty));
        }

        /// <summary>验证初始状态与更新保护。</summary>
        [Test]
        public void NewNodeIsReadyAndIgnoresUpdate()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.DoUpdate();

            Assert.AreEqual("Probe", node.name);
            Assert.AreEqual(LaunchNodeState.Ready, node.state);
            Assert.AreEqual(string.Empty, node.error);
            Assert.IsFalse(node.isDone);
            Assert.IsFalse(node.isSucceeded);
            Assert.AreEqual(0, node.updateCount);
        }

        /// <summary>验证启动幂等。</summary>
        [Test]
        public void StartExecutesOnce()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.Start();
            node.Start();

            Assert.AreEqual(LaunchNodeState.Running, node.state);
            Assert.AreEqual(1, node.startCount);
        }

        /// <summary>验证启动异常转换为失败。</summary>
        [Test]
        public void StartExceptionFailsNode()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { throwOnStart = true };
            node.Start();

            Assert.AreEqual(LaunchNodeState.Failed, node.state);
            StringAssert.Contains("Start exception.", node.error);
            Assert.IsTrue(node.isDone);
            Assert.IsFalse(node.isSucceeded);
        }

        /// <summary>验证更新异常转换为失败。</summary>
        [Test]
        public void UpdateExceptionFailsNode()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { throwOnUpdate = true };
            node.Start();
            node.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Failed, node.state);
            StringAssert.Contains("Update exception.", node.error);
        }

        /// <summary>验证运行中的节点可以在更新时完成。</summary>
        [Test]
        public void UpdateCanCompleteRunningNode()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { completeOnUpdate = true };
            node.Start();
            node.DoUpdate();
            node.DoUpdate();

            Assert.AreEqual(LaunchNodeState.Succeeded, node.state);
            Assert.AreEqual(1, node.updateCount);
            Assert.IsTrue(node.isDone);
            Assert.IsTrue(node.isSucceeded);
        }

        /// <summary>验证空失败文本被标准化。</summary>
        [TestCase(null)]
        [TestCase("")]
        public void EmptyFailureUsesDefaultMessage(string error)
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe")
            {
                failOnStart = true,
                failureMessage = error,
            };
            node.Start();

            Assert.AreEqual("Launch node failed.", node.error);
        }

        /// <summary>验证就绪状态停止不触发回调。</summary>
        [Test]
        public void StopReadyNodeSkipsCallback()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, node.state);
            Assert.AreEqual(0, node.stopCount);
        }

        /// <summary>验证运行状态停止触发一次回调。</summary>
        [Test]
        public void StopRunningNodeExecutesOnce()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe");
            node.Start();
            node.Stop();
            node.Stop();

            Assert.AreEqual(LaunchNodeState.Stopped, node.state);
            Assert.AreEqual(1, node.stopCount);
        }

        /// <summary>验证停止异常不会逃逸。</summary>
        [Test]
        public void StopExceptionStillStopsNode()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { throwOnStop = true };
            node.Start();

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Stop exception.");
            Assert.DoesNotThrow(node.Stop);
            Assert.AreEqual(LaunchNodeState.Stopped, node.state);
        }

        /// <summary>验证成功终态忽略后续生命周期调用。</summary>
        [Test]
        public void TerminalNodeIgnoresFurtherCalls()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { completeOnStart = true };
            node.Start();
            node.Start();
            node.DoUpdate();
            node.Stop();

            Assert.AreEqual(LaunchNodeState.Succeeded, node.state);
            Assert.AreEqual(1, node.startCount);
            Assert.AreEqual(0, node.updateCount);
            Assert.AreEqual(0, node.stopCount);
        }

        /// <summary>验证失败终态忽略后续生命周期调用。</summary>
        [Test]
        public void FailedNodeIgnoresFurtherCalls()
        {
            ProbeLaunchNode node = new ProbeLaunchNode("Probe") { failOnStart = true };
            node.Start();
            node.Start();
            node.DoUpdate();
            node.Stop();

            Assert.AreEqual(LaunchNodeState.Failed, node.state);
            Assert.AreEqual("Probe failure.", node.error);
            Assert.AreEqual(1, node.startCount);
            Assert.AreEqual(0, node.updateCount);
            Assert.AreEqual(0, node.stopCount);
        }
    }
}
