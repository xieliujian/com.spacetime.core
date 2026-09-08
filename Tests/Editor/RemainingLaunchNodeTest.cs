using System;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 后续具体启动节点测试。
    /// </summary>
    public sealed class RemainingLaunchNodeTest
    {
        /// <summary>验证空业务节点具有稳定名称并立即成功。</summary>
        [TestCase("CheckDownloadServerConfigSuccessNode", "CheckDownloadServerConfigSuccess")]
        [TestCase("BeforeLoginGlobalResLoadNode", "BeforeLoginGlobalResLoad")]
        [TestCase("DownloadServerDynamicNode", "DownloadServerDynamic")]
        [TestCase("CheckDownloadServerDynamicSuccessNode", "CheckDownloadServerDynamicSuccess")]
        [TestCase("SDKInitNode", "SDKInit")]
        [TestCase("SDKInitCheckNode", "SDKInitCheck")]
        [TestCase("CheckVersionNode", "CheckVersion")]
        [TestCase("UpdateNode", "Update")]
        [TestCase("ReloadResNode", "ReloadRes")]
        [TestCase("CompileShaderNode", "CompileShader")]
        [TestCase("CheckSubpackUpdateNode", "CheckSubpackUpdate")]
        [TestCase("LaunchGCNode", "LaunchGC")]
        [TestCase("OpenLoginPanelNode", "OpenLoginPanel")]
        [TestCase("SDKLoginNode", "SDKLogin")]
        [TestCase("EndNode", "End")]
        public void ImmediateNodeSucceeds(string nodeTypeName, string expectedName)
        {
            Type nodeType = typeof(LaunchNode).Assembly.GetType("ST.Core.Launch." + nodeTypeName);
            Assert.IsNotNull(nodeType, nodeTypeName);
            LaunchNode node = (LaunchNode)Activator.CreateInstance(nodeType);
            node.Start();

            Assert.AreEqual(expectedName, node.name);
            Assert.IsTrue(node.isSucceeded);
        }
    }
}
