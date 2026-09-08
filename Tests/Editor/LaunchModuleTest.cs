using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 启动模块节点映射与树结构测试。
    /// </summary>
    public sealed class LaunchModuleTest
    {
        /// <summary>验证有效枚举映射到对应节点类型。</summary>
        [TestCase(LaunchNodeType.WaitFrame, typeof(WaitFrameNode))]
        [TestCase(LaunchNodeType.AppInit, typeof(AppInitNode))]
        [TestCase(LaunchNodeType.OpenLaunchShell, typeof(OpenLaunchShellNode))]
        [TestCase(LaunchNodeType.SplashGlobalResLoad, typeof(SplashGlobalResLoadNode))]
        [TestCase(LaunchNodeType.DownloadServerConfig, typeof(DownloadServerConfigNode))]
        [TestCase(LaunchNodeType.TableLoad, typeof(TableLoadNode))]
        [TestCase(LaunchNodeType.CheckDownloadServerConfigSuccess, typeof(CheckDownloadServerConfigSuccessNode))]
        [TestCase(LaunchNodeType.BeforeLoginGlobalResLoad, typeof(BeforeLoginGlobalResLoadNode))]
        [TestCase(LaunchNodeType.DownloadServerDynamic, typeof(DownloadServerDynamicNode))]
        [TestCase(LaunchNodeType.CheckDownloadServerDynamicSuccess, typeof(CheckDownloadServerDynamicSuccessNode))]
        [TestCase(LaunchNodeType.SDKInit, typeof(SDKInitNode))]
        [TestCase(LaunchNodeType.SDKInitCheck, typeof(SDKInitCheckNode))]
        [TestCase(LaunchNodeType.CheckVersion, typeof(CheckVersionNode))]
        [TestCase(LaunchNodeType.Update, typeof(UpdateNode))]
        [TestCase(LaunchNodeType.ReloadRes, typeof(ReloadResNode))]
        [TestCase(LaunchNodeType.CompileShader, typeof(CompileShaderNode))]
        [TestCase(LaunchNodeType.CheckSubpackUpdate, typeof(CheckSubpackUpdateNode))]
        [TestCase(LaunchNodeType.LaunchGC, typeof(LaunchGCNode))]
        [TestCase(LaunchNodeType.OpenLoginPanel, typeof(OpenLoginPanelNode))]
        [TestCase(LaunchNodeType.SDKLogin, typeof(SDKLoginNode))]
        [TestCase(LaunchNodeType.End, typeof(EndNode))]
        public void FactoryMapsNodeType(LaunchNodeType nodeType, Type expectedType)
        {
            LaunchNode node = InvokeCreateNode(new LaunchModule(), nodeType);
            Assert.AreEqual(expectedType, node.GetType());
        }

        /// <summary>验证无效枚举被拒绝。</summary>
        [TestCase(LaunchNodeType.None)]
        [TestCase(LaunchNodeType.Max)]
        [TestCase((LaunchNodeType)byte.MaxValue)]
        public void FactoryRejectsInvalidType(LaunchNodeType nodeType)
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => InvokeCreateNode(new LaunchModule(), nodeType));
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }

        /// <summary>验证标准根节点和首屏并行组顺序。</summary>
        [Test]
        public void LaunchBuildsExactTree()
        {
            LaunchModule module = new LaunchModule();
            module.Launch();

            SequenceNode root = (SequenceNode)GetField(module, "m_RootNode");
            List<LaunchNode> rootChildren = (List<LaunchNode>)GetField(root, "m_Children");
            Assert.AreEqual("LaunchNode", root.name);
            Assert.AreEqual(18, rootChildren.Count);
            Assert.AreEqual(typeof(WaitFrameNode), rootChildren[0].GetType());
            Assert.AreEqual(typeof(AppInitNode), rootChildren[1].GetType());
            Assert.AreEqual(typeof(ParallelNode), rootChildren[2].GetType());
            Assert.AreEqual(typeof(CheckDownloadServerConfigSuccessNode), rootChildren[3].GetType());
            Assert.AreEqual(typeof(BeforeLoginGlobalResLoadNode), rootChildren[4].GetType());
            Assert.AreEqual(typeof(DownloadServerDynamicNode), rootChildren[5].GetType());
            Assert.AreEqual(typeof(CheckDownloadServerDynamicSuccessNode), rootChildren[6].GetType());
            Assert.AreEqual(typeof(SDKInitNode), rootChildren[7].GetType());
            Assert.AreEqual(typeof(SDKInitCheckNode), rootChildren[8].GetType());
            Assert.AreEqual(typeof(CheckVersionNode), rootChildren[9].GetType());
            Assert.AreEqual(typeof(UpdateNode), rootChildren[10].GetType());
            Assert.AreEqual(typeof(ReloadResNode), rootChildren[11].GetType());
            Assert.AreEqual(typeof(CompileShaderNode), rootChildren[12].GetType());
            Assert.AreEqual(typeof(CheckSubpackUpdateNode), rootChildren[13].GetType());
            Assert.AreEqual(typeof(LaunchGCNode), rootChildren[14].GetType());
            Assert.AreEqual(typeof(OpenLoginPanelNode), rootChildren[15].GetType());
            Assert.AreEqual(typeof(SDKLoginNode), rootChildren[16].GetType());
            Assert.AreEqual(typeof(EndNode), rootChildren[17].GetType());

            ParallelNode splash = (ParallelNode)rootChildren[2];
            List<LaunchNode> splashChildren = (List<LaunchNode>)GetField(splash, "m_Children");
            Assert.AreEqual("Splash", splash.name);
            Assert.AreEqual(4, splashChildren.Count);
            Assert.AreEqual(typeof(OpenLaunchShellNode), splashChildren[0].GetType());
            Assert.AreEqual(typeof(SplashGlobalResLoadNode), splashChildren[1].GetType());
            Assert.AreEqual(typeof(DownloadServerConfigNode), splashChildren[2].GetType());
            Assert.AreEqual(typeof(TableLoadNode), splashChildren[3].GetType());
        }

        /// <summary>验证模块从启动到一帧后成功。</summary>
        [Test]
        public void LaunchCompletesAfterOneUpdate()
        {
            LaunchModule module = new LaunchModule();
            module.DoUpdate();
            Assert.AreEqual(LaunchNodeState.Ready, module.state);
            module.Launch();
            Assert.AreEqual(LaunchNodeState.Running, module.state);
            object root = GetField(module, "m_RootNode");
            module.Launch();
            Assert.AreSame(root, GetField(module, "m_RootNode"));
            module.DoUpdate();
            Assert.IsTrue(module.isSucceeded);
            module.DoUpdate();
            Assert.IsTrue(module.isSucceeded);
        }

        /// <summary>验证关闭状态转换和幂等。</summary>
        [Test]
        public void ClosePreservesTerminalState()
        {
            LaunchModule ready = new LaunchModule();
            ready.DoClose();
            ready.DoClose();
            Assert.AreEqual(LaunchNodeState.Stopped, ready.state);

            LaunchModule running = new LaunchModule();
            running.Launch();
            running.DoClose();
            Assert.AreEqual(LaunchNodeState.Stopped, running.state);

            LaunchModule succeeded = new LaunchModule();
            succeeded.Launch();
            succeeded.DoUpdate();
            succeeded.DoClose();
            Assert.AreEqual(LaunchNodeState.Succeeded, succeeded.state);
        }

        /// <summary>通过反射调用私有节点工厂。</summary>
        static LaunchNode InvokeCreateNode(LaunchModule module, LaunchNodeType nodeType)
        {
            MethodInfo method = typeof(LaunchModule).GetMethod(
                "CreateNode",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (LaunchNode)method.Invoke(module, new object[] { nodeType });
        }

        /// <summary>读取对象的非公开实例字段。</summary>
        static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return field.GetValue(target);
        }
    }
}
