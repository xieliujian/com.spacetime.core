using System.Collections.Generic;
using NUnit.Framework;

namespace ST.Core.Launch.Tests
{
    /// <summary>动态命令注册、注销与派发测试。</summary>
    public sealed class DynamicCommandTest
    {
        /// <summary>验证派发时回调能拿到命令 ID 与参数。</summary>
        [Test]
        public void CommandDynamicInvokesCallbackWithArguments()
        {
            GameWorld gameWorld = new GameWorld();
            int receivedCommandID = -1;
            int receivedValue = 0;
            LogicEventCallBack callback = (commandID, args) =>
            {
                receivedCommandID = commandID;
                receivedValue = args.GetInt32(0);
            };

            gameWorld.kernel.AddDynamicCommand(10, callback);

            VarList args = VarList.Create();
            args.AddInt(42);
            gameWorld.kernel.CommandDynamic(10, args);

            Assert.AreEqual(10, receivedCommandID);
            Assert.AreEqual(42, receivedValue);
        }

        /// <summary>验证回调按优先级升序执行。</summary>
        [Test]
        public void CommandDynamicInvokesCallbacksByPriority()
        {
            GameWorld gameWorld = new GameWorld();
            List<int> order = new List<int>(2);
            LogicEventCallBack laterCallback = (commandID, args) => order.Add(2);
            LogicEventCallBack earlierCallback = (commandID, args) => order.Add(1);

            gameWorld.kernel.AddDynamicCommand(11, laterCallback, 10);
            gameWorld.kernel.AddDynamicCommand(11, earlierCallback, -1);
            gameWorld.kernel.CommandDynamic(11);

            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
        }

        /// <summary>验证同一回调重复注册只执行一次。</summary>
        [Test]
        public void DuplicateCallbackRunsOnce()
        {
            GameWorld gameWorld = new GameWorld();
            int callCount = 0;
            LogicEventCallBack callback = (commandID, args) => callCount++;

            gameWorld.kernel.AddDynamicCommand(12, callback);
            gameWorld.kernel.AddDynamicCommand(12, callback);
            gameWorld.kernel.CommandDynamic(12);

            Assert.AreEqual(1, callCount);
        }

        /// <summary>验证注销后不再收到命令。</summary>
        [Test]
        public void RemovedCallbackStopsReceivingCommand()
        {
            GameWorld gameWorld = new GameWorld();
            int callCount = 0;
            LogicEventCallBack callback = (commandID, args) => callCount++;

            gameWorld.kernel.AddDynamicCommand(13, callback);
            gameWorld.kernel.RemoveDynamicCommand(13, callback);
            gameWorld.kernel.CommandDynamic(13);

            Assert.AreEqual(0, callCount);
        }

        /// <summary>验证单个回调异常不会中断后续回调。</summary>
        [Test]
        public void CallbackExceptionDoesNotStopOtherCallbacks()
        {
            GameWorld gameWorld = new GameWorld();
            int callCount = 0;
            LogicEventCallBack exceptionCallback = (commandID, args) =>
                throw new System.InvalidOperationException("test");
            LogicEventCallBack normalCallback = (commandID, args) => callCount++;

            gameWorld.kernel.AddDynamicCommand(14, exceptionCallback, -1);
            gameWorld.kernel.AddDynamicCommand(14, normalCallback);
            gameWorld.kernel.CommandDynamic(14);

            Assert.AreEqual(1, callCount);
        }

        /// <summary>验证派发期间新增的监听从下一次派发开始生效。</summary>
        [Test]
        public void CallbackAddedDuringDispatchRunsOnNextDispatch()
        {
            GameWorld gameWorld = new GameWorld();
            int lateCallCount = 0;
            LogicEventCallBack lateCallback = (commandID, args) => lateCallCount++;
            LogicEventCallBack firstCallback = (commandID, args) =>
                gameWorld.kernel.AddDynamicCommand(15, lateCallback);

            gameWorld.kernel.AddDynamicCommand(15, firstCallback);
            gameWorld.kernel.CommandDynamic(15);

            Assert.AreEqual(0, lateCallCount);

            gameWorld.kernel.CommandDynamic(15);

            Assert.AreEqual(1, lateCallCount);
        }

        /// <summary>验证关闭游戏世界会清空全部动态命令监听。</summary>
        [Test]
        public void CloseClearsDynamicCommands()
        {
            GameWorld gameWorld = new GameWorld();
            int callCount = 0;
            LogicEventCallBack callback = (commandID, args) => callCount++;

            gameWorld.kernel.AddDynamicCommand(16, callback);
            gameWorld.DoClose();
            gameWorld.kernel.CommandDynamic(16);

            Assert.AreEqual(0, callCount);
        }

        /// <summary>验证未注册命令与非法命令 ID 均安全返回。</summary>
        [Test]
        public void UnknownOrInvalidCommandIsSafe()
        {
            GameWorld gameWorld = new GameWorld();

            gameWorld.kernel.CommandDynamic(99);
            gameWorld.kernel.CommandDynamic(-1);
            gameWorld.kernel.AddDynamicCommand(-1, (commandID, args) => { });
            gameWorld.kernel.RemoveDynamicCommand(99, (commandID, args) => { });

            Assert.Pass();
        }
    }
}
