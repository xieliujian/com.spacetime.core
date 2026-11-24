using System;

namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏内核实现，作为模块查找的统一入口，内部转发给 <see cref="GameWorld"/> 维护的模块表。
    /// <para>后续需要向业务模块暴露世界接口时，在这里扩展并同步补充 <see cref="IKernel"/>。</para>
    /// </summary>
    internal sealed class Kernel : IKernel
    {
        /// <summary>所属游戏世界，模块表由它持有。</summary>
        readonly GameWorld m_GameWorld;

        /// <summary>创建内核并绑定所属游戏世界。</summary>
        /// <param name="gameWorld">持有模块表的游戏世界，不可为 null。</param>
        public Kernel(GameWorld gameWorld)
        {
            m_GameWorld = gameWorld;
        }

        /// <summary>按运行时类型精确查找模块。</summary>
        /// <typeparam name="T">模块的运行时类型。</typeparam>
        /// <returns>对应模块实例；未注册时返回 null。</returns>
        public T GetModule<T>() where T : ILogicModule
        {
            return m_GameWorld.GetModule<T>();
        }

        /// <summary>按运行时类型对象精确查找模块。</summary>
        /// <param name="type">模块的运行时类型。</param>
        /// <returns>对应模块实例；type 为 null 或未注册时返回 null。</returns>
        public ILogicModule GetModule(Type type)
        {
            return m_GameWorld.GetModule(type);
        }

        /// <summary>按模块类型名查找模块。</summary>
        /// <param name="name">模块类型名。</param>
        /// <returns>对应模块实例；name 为空或未注册时返回 null。</returns>
        public ILogicModule GetModule(string name)
        {
            return m_GameWorld.GetModule(name);
        }

        /// <summary>获取全部已注册模块。</summary>
        /// <returns>按注册顺序排列的模块数组。</returns>
        public ILogicModule[] GetAllModule()
        {
            return m_GameWorld.GetAllModule();
        }

        /// <summary>注册动态命令回调。</summary>
        /// <param name="commandID">命令 ID，不能为负数。</param>
        /// <param name="cb">命令回调，不能为 null。</param>
        /// <param name="prior">优先级，数值越小越先执行。</param>
        public void AddDynamicCommand(int commandID, LogicEventCallBack cb, int prior = 0)
        {
            m_GameWorld.AddDynamicCommand(commandID, cb, prior);
        }

        /// <summary>注销动态命令回调，未注册时忽略。</summary>
        /// <param name="commandID">命令 ID。</param>
        /// <param name="cb">要注销的回调。</param>
        public void RemoveDynamicCommand(int commandID, LogicEventCallBack cb)
        {
            m_GameWorld.RemoveDynamicCommand(commandID, cb);
        }

        /// <summary>派发动态命令，未注册监听时安全返回。</summary>
        /// <param name="commandID">命令 ID，不能为负数。</param>
        /// <param name="args">命令参数，可为 null。</param>
        public void CommandDynamic(int commandID, IVarList args = null)
        {
            m_GameWorld.CommandDynamic(commandID, args);
        }
    }
}
