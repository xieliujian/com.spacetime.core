using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 游戏运行入口与场景启动组件测试。
    /// </summary>
    public sealed class GameApplicationTest
    {
        /// <summary>验证初始化前帧调用安全。</summary>
        [Test]
        public void BeforeInitIsSafe()
        {
            GameApplication application = new GameApplication();
            application.DoUpdate();
            application.DoLateUpdate();
            Assert.IsNull(application.launchModule);
        }

        /// <summary>验证初始化创建并启动唯一模块。</summary>
        [Test]
        public void InitCreatesOneLaunchModule()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            LaunchModule module = application.launchModule;
            application.DoInit();

            Assert.IsNotNull(module);
            Assert.AreSame(module, application.launchModule);
            Assert.AreEqual(LaunchNodeState.Running, module.state);
        }

        /// <summary>验证应用逐帧驱动启动完成。</summary>
        [Test]
        public void UpdateCompletesLaunch()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            application.DoUpdate();
            Assert.IsTrue(application.launchModule.isSucceeded);
        }

        /// <summary>验证运行中关闭和重复关闭安全。</summary>
        [Test]
        public void CloseRunningApplicationIsIdempotent()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            application.DoClose();
            application.DoClose();
            application.DoUpdate();
            application.DoLateUpdate();
            Assert.AreEqual(LaunchNodeState.Stopped, application.launchModule.state);
        }

        /// <summary>验证成功后关闭保留成功状态。</summary>
        [Test]
        public void CloseAfterSuccessPreservesSuccess()
        {
            GameApplication application = new GameApplication();
            application.DoInit();
            application.DoUpdate();
            application.DoClose();
            Assert.AreEqual(LaunchNodeState.Succeeded, application.launchModule.state);
        }

        /// <summary>验证场景入口的公开类型和生命周期方法。</summary>
        [Test]
        public void GameLauncherHasUnityLifecycle()
        {
            TypeInfo typeInfo = typeof(GameLauncher).GetTypeInfo();
            Assert.IsTrue(typeInfo.IsPublic);
            Assert.IsTrue(typeInfo.IsSealed);
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(GameLauncher)));
            Assert.IsNotNull(GetLifecycleMethod("Awake"));
            Assert.IsNotNull(GetLifecycleMethod("Update"));
            Assert.IsNotNull(GetLifecycleMethod("LateUpdate"));
            Assert.IsNotNull(GetLifecycleMethod("OnDestroy"));
        }

        /// <summary>读取指定 Unity 生命周期方法。</summary>
        static MethodInfo GetLifecycleMethod(string methodName)
        {
            return typeof(GameLauncher).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
