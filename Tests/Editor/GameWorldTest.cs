using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ST.Core.Launch.Tests
{
    /// <summary>
    /// 游戏模块内核与场景启动组件测试。
    /// </summary>
    public sealed class GameWorldTest
    {
        /// <summary>验证初始化前帧调用安全。</summary>
        [Test]
        public void BeforeInitIsSafe()
        {
            GameWorld gameWorld = new GameWorld();
            gameWorld.DoUpdate();
            gameWorld.DoLateUpdate();
            Assert.IsNull(gameWorld.launchModule);
        }

        /// <summary>验证初始化创建并启动唯一模块。</summary>
        [Test]
        public void InitCreatesOneLaunchModule()
        {
            GameWorld gameWorld = new GameWorld();
            gameWorld.DoInit();
            LaunchModule module = gameWorld.launchModule;
            gameWorld.DoInit();

            Assert.IsNotNull(module);
            Assert.AreSame(module, gameWorld.launchModule);
            Assert.AreEqual(LaunchNodeState.Running, module.state);
        }

        /// <summary>验证应用逐帧驱动启动完成。</summary>
        [Test]
        public void UpdateCompletesLaunch()
        {
            GameWorld gameWorld = new GameWorld();
            gameWorld.DoInit();
            gameWorld.DoUpdate();
            Assert.IsTrue(gameWorld.launchModule.isSucceeded);
        }

        /// <summary>验证运行中关闭和重复关闭安全。</summary>
        [Test]
        public void CloseRunningApplicationIsIdempotent()
        {
            GameWorld gameWorld = new GameWorld();
            gameWorld.DoInit();
            gameWorld.DoClose();
            gameWorld.DoClose();
            gameWorld.DoUpdate();
            gameWorld.DoLateUpdate();
            Assert.AreEqual(LaunchNodeState.Stopped, gameWorld.launchModule.state);
        }

        /// <summary>验证成功后关闭保留成功状态。</summary>
        [Test]
        public void CloseAfterSuccessPreservesSuccess()
        {
            GameWorld gameWorld = new GameWorld();
            gameWorld.DoInit();
            gameWorld.DoUpdate();
            gameWorld.DoClose();
            Assert.AreEqual(LaunchNodeState.Succeeded, gameWorld.launchModule.state);
        }

        /// <summary>验证场景入口的公开类型和生命周期方法。</summary>
        [Test]
        public void GameLauncherHasUnityLifecycle()
        {
            TypeInfo typeInfo = typeof(GameLauncher).GetTypeInfo();
            Assert.IsTrue(typeInfo.IsPublic);
            Assert.IsTrue(typeInfo.IsAbstract);
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(GameLauncher)));
            Assert.IsNotNull(GetLifecycleMethod("Awake"));
            Assert.IsNotNull(GetLifecycleMethod("Update"));
            Assert.IsNotNull(GetLifecycleMethod("LateUpdate"));
            Assert.IsNotNull(GetLifecycleMethod("OnDestroy"));
        }

        /// <summary>验证模块按列表顺序初始化与逐帧驱动，并在关闭时逆序释放。</summary>
        [Test]
        public void ModulesFollowWorldLifecycleInOrder()
        {
            List<string> calls = new List<string>();
            RecordingManager first = new RecordingManager("first", calls);
            RecordingManager second = new RecordingManager("second", calls);
            List<IManager> modules = new List<IManager>(2);
            modules.Add(first);
            modules.Add(second);

            GameWorld gameWorld = new GameWorld();
            gameWorld.DoInit(modules);
            gameWorld.DoUpdate();
            gameWorld.DoUpdate();
            gameWorld.DoLateUpdate();
            gameWorld.DoClose();

            string expected = "first:init,second:init,first:update,second:update," +
                "first:update,second:update,first:late,second:late,second:close,first:close";
            string actual = string.Join(",", calls);

            Assert.IsNotNull(gameWorld.launchModule);
            Assert.AreEqual(expected, actual);
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
