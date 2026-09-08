namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏运行入口，负责持有并驱动启动模块。
    /// </summary>
    public sealed class GameApplication : IManager
    {
        /// <summary>标准启动模块。</summary>
        LaunchModule m_LaunchModule;

        /// <summary>是否已经初始化。</summary>
        bool m_IsInitialized;

        /// <summary>是否已经关闭。</summary>
        bool m_IsClosed;

        /// <summary>标准启动模块；初始化前为空。</summary>
        public LaunchModule launchModule { get { return m_LaunchModule; } }

        /// <summary>创建并启动唯一的启动模块。</summary>
        public override void DoInit()
        {
            if (m_IsInitialized || m_IsClosed)
            {
                return;
            }

            m_LaunchModule = new LaunchModule();
            m_LaunchModule.DoInit();
            m_LaunchModule.Launch();
            m_IsInitialized = true;
        }

        /// <summary>驱动启动模块的逐帧逻辑。</summary>
        public override void DoUpdate()
        {
            if (!m_IsInitialized || m_IsClosed)
            {
                return;
            }

            m_LaunchModule.DoUpdate();
        }

        /// <summary>驱动启动模块的滞后帧逻辑。</summary>
        public override void DoLateUpdate()
        {
            if (!m_IsInitialized || m_IsClosed)
            {
                return;
            }

            m_LaunchModule.DoLateUpdate();
        }

        /// <summary>关闭启动模块；重复调用会被忽略。</summary>
        public override void DoClose()
        {
            if (m_IsClosed)
            {
                return;
            }

            m_IsClosed = true;
            if (m_LaunchModule == null)
            {
                return;
            }

            m_LaunchModule.DoClose();
        }
    }
}
