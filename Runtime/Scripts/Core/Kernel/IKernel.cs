using System;

namespace ST.Core
{
    /// <summary>
    /// 逻辑模块内核接口，提供按类型、类型对象或名称查找已注册模块的能力。
    /// <para>
    /// 业务代码通过 <see cref="ST.Core.Launch.GameWorld.gameKernel"/> 获取该接口，
    /// 模块内部通过基类的 <c>m_Kernel</c> 字段获取同一实例。
    /// </para>
    /// </summary>
    public interface IKernel
    {
        /// <summary>按运行时类型精确查找模块。</summary>
        /// <typeparam name="T">模块的运行时类型。</typeparam>
        /// <returns>对应模块实例；未注册时返回 null。派生类型不会匹配基类查询。</returns>
        T GetModule<T>() where T : ILogicModule;

        /// <summary>按运行时类型对象精确查找模块。</summary>
        /// <param name="type">模块的运行时类型。</param>
        /// <returns>对应模块实例；type 为 null 或未注册时返回 null。</returns>
        ILogicModule GetModule(Type type);

        /// <summary>按模块类型名或完整类型名查找模块。</summary>
        /// <param name="name">模块的 <see cref="Type.Name"/> 或 <see cref="Type.FullName"/>。</param>
        /// <returns>对应模块实例；name 为空或未注册时返回 null。</returns>
        ILogicModule GetModule(string name);

        /// <summary>获取全部已注册模块。</summary>
        /// <returns>按注册顺序排列的模块数组，每次调用都会分配新数组。</returns>
        ILogicModule[] GetAllModule();

        /// <summary>注册动态命令回调。</summary>
        /// <param name="commandID">命令 ID，不能为负数。</param>
        /// <param name="cb">命令回调，不能为 null。</param>
        /// <param name="prior">优先级，数值越小越先执行。</param>
        void AddDynamicCommand(int commandID, LogicEventCallBack cb, int prior = 0);

        /// <summary>注销动态命令回调，未注册时忽略。</summary>
        /// <param name="commandID">命令 ID。</param>
        /// <param name="cb">要注销的回调。</param>
        void RemoveDynamicCommand(int commandID, LogicEventCallBack cb);

        /// <summary>派发动态命令，未注册监听时安全返回。</summary>
        /// <param name="commandID">命令 ID，不能为负数。</param>
        /// <param name="args">命令参数，可为 null。</param>
        void CommandDynamic(int commandID, IVarList args = null);
    }
}
