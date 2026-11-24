using System.Collections.Generic;
using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏模块内核，负责按顺序初始化、逐帧驱动并关闭全部逻辑模块。
    /// <para>
    /// 变量定义位于 GameWorld_Define，模块注册与查找位于 GameWorld_Module。
    /// <see cref="GameLauncher"/> 负责组装模块后交给本类。
    /// </para>
    /// </summary>
    public sealed partial class GameWorld
    {
        /// <summary>创建内核并登记为全局内核；本构造函数不注册任何业务模块。</summary>
        public GameWorld()
        {
            m_Kernel = new Kernel(this);
            s_GameKernel = m_Kernel;
        }

        /// <summary>只启动标准启动模块，供不经过 <see cref="GameLauncher"/> 的场景与测试直接调用。</summary>
        public void DoInit()
        {
            List<ILogicModule> modules = new List<ILogicModule>(1);
            DoInit(modules);
        }

        /// <summary>
        /// 按给定顺序初始化全部模块，随后启动 <see cref="LaunchModule"/>。
        /// 列表中没有启动模块时会自动追加一个，保证启动流程仍会执行。
        /// </summary>
        /// <param name="modules">基础与业务模块列表；传入 null 时记录错误并忽略本次初始化。</param>
        public void DoInit(List<ILogicModule> modules)
        {
            if (m_IsInitialized || m_IsClosed)
                return;

            if (modules == null)
            {
                STLogger.LogError("GameWorld.DoInit 收到 null 模块列表，初始化被忽略。");
                return;
            }

            AddModules(modules);
            CollectLaunchModule();
            InitModules();

            m_LaunchModule.Launch();
            m_IsInitialized = true;
        }

        /// <summary>按注册顺序驱动全部模块的逐帧逻辑，启动流程结束后业务模块仍会继续更新。</summary>
        public void DoUpdate()
        {
            if (!m_IsInitialized || m_IsClosed)
                return;

            UpdateModules();
        }

        /// <summary>按注册顺序驱动全部模块的滞后帧逻辑。</summary>
        public void DoLateUpdate()
        {
            if (!m_IsInitialized || m_IsClosed)
                return;

            LateUpdateModules();
        }

        /// <summary>按初始化逆序关闭全部模块；重复调用会被忽略。</summary>
        public void DoClose()
        {
            if (m_IsClosed)
                return;

            m_IsClosed = true;
            CloseModules();
            ClearDynamicCommands();
        }
    }
}
