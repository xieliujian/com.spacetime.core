using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏模块内核的动态命令部分，负责注册、注销与派发全局业务事件。
    /// <para>
    /// 动态命令按 ID 建立独立回调表，允许 UI 面板与逻辑模块在任意时刻订阅。
    /// 同一回调重复注册会被忽略，回调按优先级升序执行。
    /// </para>
    /// </summary>
    public sealed partial class GameWorld
    {
        /// <summary>注册动态命令回调。</summary>
        /// <param name="commandID">命令 ID，不能为负数。</param>
        /// <param name="cb">命令回调，不能为 null。</param>
        /// <param name="prior">优先级，数值越小越先执行。</param>
        public void AddDynamicCommand(int commandID, LogicEventCallBack cb, int prior = 0)
        {
            bool isCommandIDValid = commandID >= 0;
            if (!isCommandIDValid)
            {
                STLogger.LogErrorF("动态命令 ID {0} 非法，注册被忽略。", commandID);
                return;
            }

            bool isCallbackValid = cb != null;
            if (!isCallbackValid)
            {
                STLogger.LogError("动态命令回调为 null，注册被忽略。");
                return;
            }

            CommandListenerSet listenerSet;
            bool isListenerSetFound = m_DynamicCommandListeners.TryGetValue(commandID, out listenerSet);
            if (!isListenerSetFound)
            {
                listenerSet = new CommandListenerSet();
                m_DynamicCommandListeners.Add(commandID, listenerSet);
            }

            listenerSet.Add(cb, prior);
        }

        /// <summary>注销动态命令回调，未注册时忽略。</summary>
        /// <param name="commandID">命令 ID。</param>
        /// <param name="cb">要注销的回调。</param>
        public void RemoveDynamicCommand(int commandID, LogicEventCallBack cb)
        {
            bool isCommandIDValid = commandID >= 0;
            if (!isCommandIDValid)
                return;

            CommandListenerSet listenerSet;
            bool isListenerSetFound = m_DynamicCommandListeners.TryGetValue(commandID, out listenerSet);
            if (!isListenerSetFound)
                return;

            bool isRemoved = listenerSet.Remove(cb);
            if (!isRemoved)
                return;

            bool isListenerSetEmpty = listenerSet.count <= 0;
            if (isListenerSetEmpty)
            {
                m_DynamicCommandListeners.Remove(commandID);
            }
        }

        /// <summary>派发动态命令，未注册监听时安全返回。</summary>
        /// <param name="commandID">命令 ID，不能为负数。</param>
        /// <param name="args">命令参数，可为 null。</param>
        public void CommandDynamic(int commandID, IVarList args = null)
        {
            bool isCommandIDValid = commandID >= 0;
            if (!isCommandIDValid)
            {
                STLogger.LogErrorF("动态命令 ID {0} 非法，派发被忽略。", commandID);
                return;
            }

            CommandListenerSet listenerSet;
            bool isListenerSetFound = m_DynamicCommandListeners.TryGetValue(commandID, out listenerSet);
            if (!isListenerSetFound)
                return;

            listenerSet.Invoke(commandID, args);
        }

        /// <summary>清空全部动态命令监听，关闭游戏世界时调用。</summary>
        void ClearDynamicCommands()
        {
            m_DynamicCommandListeners.Clear();
        }
    }
}
