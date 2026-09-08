using System;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 首批具体启动节点测试。
    /// </summary>
    public sealed class BootstrapLaunchNodeTest
    {
        /// <summary>验证立即完成的首批节点。</summary>
        [TestCase(typeof(AppInitNode), "AppInit")]
        [TestCase(typeof(OpenLaunchShellNode), "OpenLaunchShell")]
        [TestCase(typeof(SplashGlobalResLoadNode), "SplashGlobalResLoad")]
        [TestCase(typeof(DownloadServerConfigNode), "DownloadServerConfig")]
        [TestCase(typeof(TableLoadNode), "TableLoad")]
        public void ImmediateNodeSucceeds(Type nodeType, string expectedName)
        {
            LaunchNode node = (LaunchNode)Activator.CreateInstance(nodeType);
            node.Start();

            Assert.AreEqual(expectedName, node.name);
            Assert.IsTrue(node.isSucceeded);
        }

        /// <summary>验证等待帧节点在首个更新完成。</summary>
        [Test]
        public void WaitFrameCompletesOnFirstUpdate()
        {
            WaitFrameNode node = new WaitFrameNode();
            node.Start();
            Assert.AreEqual(LaunchNodeState.Running, node.state);
            node.DoUpdate();
            Assert.IsTrue(node.isSucceeded);
        }

        /// <summary>验证节点枚举顺序。</summary>
        [Test]
        public void NodeTypeOrderIsStable()
        {
            LaunchNodeType[] expected =
            {
                LaunchNodeType.None, LaunchNodeType.WaitFrame, LaunchNodeType.AppInit,
                LaunchNodeType.OpenLaunchShell, LaunchNodeType.SplashGlobalResLoad,
                LaunchNodeType.DownloadServerConfig, LaunchNodeType.TableLoad,
                LaunchNodeType.CheckDownloadServerConfigSuccess,
                LaunchNodeType.BeforeLoginGlobalResLoad, LaunchNodeType.DownloadServerDynamic,
                LaunchNodeType.CheckDownloadServerDynamicSuccess, LaunchNodeType.SDKInit,
                LaunchNodeType.SDKInitCheck, LaunchNodeType.CheckVersion, LaunchNodeType.Update,
                LaunchNodeType.ReloadRes, LaunchNodeType.CompileShader,
                LaunchNodeType.CheckSubpackUpdate, LaunchNodeType.LaunchGC,
                LaunchNodeType.OpenLoginPanel, LaunchNodeType.SDKLogin, LaunchNodeType.End,
                LaunchNodeType.Max,
            };

            CollectionAssert.AreEqual(expected, Enum.GetValues(typeof(LaunchNodeType)));
        }
    }
}
