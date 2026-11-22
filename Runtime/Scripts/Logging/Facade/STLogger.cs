namespace ST.Core.Logging
{
    /// <summary>
    /// 日志系统的唯一对外入口（静态门面），类名使用 STLogger 以避开 UnityEngine.Logger 的同名冲突。
    /// <para>
    /// 调用链路：<br/>
    /// <c>STLogger.LogXxx()</c> → 内部 <c>Debugger</c>（私有嵌套类）→ <c>UnityEngine.Debug.LogXxx</c><br/>
    /// → <c>Application.logMessageReceived</c> 事件 → <see cref="LogManager"/> → 写入文件
    /// </para>
    /// <para>
    /// 使用步骤：<br/>
    /// 1. 调用 <see cref="Initialize"/> 传入配置，初始化文件写入系统；<br/>
    /// 2. 调用 <see cref="EnableUnityLogCapture"/> 开启 Unity 日志捕获，日志自动落地到文件；<br/>
    /// 3. 使用 <see cref="Log"/>、<see cref="LogWarning"/>、<see cref="LogError"/> 等方法输出日志；<br/>
    /// 4. 程序退出前调用 <see cref="Close"/> 刷新并释放文件资源。
    /// </para>
    /// <para>
    /// 未调用 <see cref="Initialize"/> 时，日志只经 <c>UnityEngine.Debug</c> 输出到控制台，不会写入文件。
    /// </para>
    /// </summary>
    public static partial class STLogger
    {
        /// <summary>日志管理器实例，负责文件写入；首次 <see cref="Initialize"/> 时创建，之后复用。</summary>
        static LogManager s_Manager;

        // ──────────────────────────────────────────
        // 初始化 / 文件系统
        // ──────────────────────────────────────────

        /// <summary>
        /// 初始化日志文件系统，首次调用会创建 <see cref="LogManager"/>。
        /// <para>重复调用会重新创建文件写入器，旧文件句柄不会关闭，因此整个进程只应初始化一次。</para>
        /// </summary>
        /// <param name="config">日志配置，不可为 null。</param>
        public static void Initialize(LogConfig config)
        {
            if (s_Manager == null)
            {
                s_Manager = new LogManager();
            }

            s_Manager.Initialize(config);
        }

        /// <summary>
        /// 统一设置所有级别的日志开关，立即影响后续输出。
        /// <para>开关保存在静态字段中，与是否已调用 <see cref="Initialize"/> 无关。</para>
        /// </summary>
        /// <param name="isLog"><c>true</c> 表示全部开启，<c>false</c> 表示全部关闭。</param>
        public static void SetAllIsLog(bool isLog)
        {
            Debugger.SetAllIsLog(isLog);
        }

        /// <summary>
        /// 开启或关闭 Unity 控制台日志捕获，开启后经由 <c>UnityEngine.Debug</c> 输出的日志会写入文件。
        /// <para>未初始化或状态未变化时调用会被直接忽略。</para>
        /// </summary>
        /// <param name="enable"><c>true</c> 表示开始监听，<c>false</c> 表示停止监听。</param>
        public static void EnableUnityLogCapture(bool enable)
        {
            if (s_Manager == null)
                return;

            s_Manager.EnableUnityLogCapture(enable);
        }

        /// <summary>把缓冲区中的日志立即写入文件，未初始化时调用会被忽略。</summary>
        public static void Flush()
        {
            if (s_Manager == null)
                return;

            s_Manager.Flush();
        }

        /// <summary>
        /// 停止 Unity 日志捕获，刷新缓冲并关闭文件句柄。
        /// <para>程序正常退出时应调用；未初始化时调用会被忽略。</para>
        /// </summary>
        public static void Close()
        {
            if (s_Manager == null)
                return;

            s_Manager.Close();
        }
    }
}
