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
    }
}
