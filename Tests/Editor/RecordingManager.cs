using System.Collections.Generic;

namespace ST.Core.Launch.Tests
{
    /// <summary>测试用的模块替身，按调用顺序把生命周期名称写入共享列表。</summary>
    public sealed class RecordingManager : ILogicModule
    {
        /// <summary>用于区分不同替身的名称。</summary>
        readonly string m_Name;

        /// <summary>共享的生命周期调用记录。</summary>
        readonly List<string> m_Calls;

        /// <summary>注册时注入的内核，供测试确认内核注入是否生效。</summary>
        public IKernel kernel { get { return m_Kernel; } }

        /// <summary>创建替身并绑定名称与共享记录列表。</summary>
        /// <param name="name">写入记录时使用的替身名称。</param>
        /// <param name="calls">调用顺序记录列表。</param>
        public RecordingManager(string name, List<string> calls)
        {
            m_Name = name;
            m_Calls = calls;
        }

        /// <summary>记录初始化调用。</summary>
        public override void DoInit()
        {
            Record("init");
        }

        /// <summary>记录逐帧调用。</summary>
        public override void DoUpdate()
        {
            Record("update");
        }

        /// <summary>记录滞后帧调用。</summary>
        public override void DoLateUpdate()
        {
            Record("late");
        }

        /// <summary>记录关闭调用。</summary>
        public override void DoClose()
        {
            Record("close");
        }

        /// <summary>把“名称:阶段”写入共享列表。</summary>
        /// <param name="stage">生命周期阶段名。</param>
        void Record(string stage)
        {
            m_Calls.Add(m_Name + ":" + stage);
        }
    }
}
