using System;
using System.Collections.Generic;

namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏模块内核的变量定义部分，集中存放内核、模块表与运行状态。
    /// </summary>
    public sealed partial class GameWorld
    {
        /// <summary>最近一次创建的游戏内核，供 <see cref="gameKernel"/> 返回。</summary>
        static IKernel s_GameKernel;

        /// <summary>当前游戏世界的模块查找内核。</summary>
        Kernel m_Kernel;

        /// <summary>标准启动模块，未注册或未初始化时为 null。</summary>
        LaunchModule m_LaunchModule;

        /// <summary>全部逻辑模块，列表顺序即初始化与逐帧驱动的执行顺序。</summary>
        readonly List<ILogicModule> m_Modules = new List<ILogicModule>(CommonDefine.s_ListConst_16);

        /// <summary>按模块运行时类型索引的模块表，用于按类型精确查找。</summary>
        readonly Dictionary<Type, ILogicModule> m_ModuleByType =
            new Dictionary<Type, ILogicModule>(CommonDefine.s_ListConst_16);

        /// <summary>按模块类型名索引的模块表，用于按名称查找。</summary>
        readonly Dictionary<string, ILogicModule> m_ModuleByName =
            new Dictionary<string, ILogicModule>(CommonDefine.s_ListConst_16);

        /// <summary>重写了 <see cref="ILogicModule.DoUpdate"/> 的模块，按注册顺序逐帧调用。</summary>
        readonly List<ILogicModule> m_UpdateModules = new List<ILogicModule>(CommonDefine.s_ListConst_16);

        /// <summary>重写了 <see cref="ILogicModule.DoLateUpdate"/> 的模块，按注册顺序逐帧调用。</summary>
        readonly List<ILogicModule> m_LateUpdateModules = new List<ILogicModule>(CommonDefine.s_ListConst_16);

        /// <summary>是否已经完成初始化。</summary>
        bool m_IsInitialized;

        /// <summary>是否已经关闭。</summary>
        bool m_IsClosed;

        /// <summary>当前游戏世界的模块查找内核，创建游戏世界后即可使用。</summary>
        public IKernel kernel
        {
            get
            {
                return m_Kernel;
            }
        }

        /// <summary>
        /// 最近一次创建的游戏内核，写法与旧工程 <c>GameWorld.gameKernel</c> 一致。
        /// <para>创建新游戏世界会覆盖该引用；多个游戏世界并存时以最后创建的为准。</para>
        /// </summary>
        public static IKernel gameKernel
        {
            get
            {
                return s_GameKernel;
            }
        }

        /// <summary>标准启动模块；初始化前为空。</summary>
        public LaunchModule launchModule
        {
            get
            {
                return m_LaunchModule;
            }
        }
    }
}
