using System;
using System.Collections.Generic;
using ST.Core.Logging;

namespace ST.Core
{
    /// <summary>单条动态命令监听记录。</summary>
    internal sealed class CommandListener
    {
        /// <summary>注册的回调。</summary>
        public LogicEventCallBack callback;

        /// <summary>优先级，数值越小越先执行。</summary>
        public int prior;

        /// <summary>当前是否正在执行，用于阻止同一条命令的递归回调。</summary>
        public bool isInvoking;

        /// <summary>派发过程中被注销的标记，派发结束后统一移除。</summary>
        public bool isRemoved;
    }

    /// <summary>
    /// 单个动态命令的回调集合，按优先级升序执行并隔离单个回调异常。
    /// 派发期间新增的监听在本次派发结束后生效，派发期间注销的监听立即跳过。
    /// </summary>
    internal sealed class CommandListenerSet
    {
        /// <summary>当前生效的监听列表。</summary>
        readonly List<CommandListener> m_Listeners = new List<CommandListener>(CommonDefine.s_ListConst_8);

        /// <summary>派发期间新增的监听，派发结束后合并。</summary>
        readonly List<CommandListener> m_PendingListeners =
            new List<CommandListener>(CommonDefine.s_ListConst_8);

        /// <summary>是否正在派发当前命令。</summary>
        bool m_IsDispatching;

        /// <summary>当前监听数量，包含等待合并的新监听。</summary>
        public int count
        {
            get
            {
                int result = m_Listeners.Count + m_PendingListeners.Count;
                return result;
            }
        }

        /// <summary>注册回调，重复注册同一回调会被忽略。</summary>
        /// <param name="callback">要注册的回调。</param>
        /// <param name="prior">优先级，数值越小越先执行。</param>
        public void Add(LogicEventCallBack callback, int prior)
        {
            bool isCallbackNull = callback == null;
            if (isCallbackNull)
            {
                STLogger.LogError("动态命令回调为 null，注册被忽略。");
                return;
            }

            bool isDuplicated = Contains(callback);
            if (isDuplicated)
                return;

            CommandListener listener = new CommandListener();
            listener.callback = callback;
            listener.prior = prior;

            bool isDispatching = m_IsDispatching;
            if (isDispatching)
            {
                m_PendingListeners.Add(listener);
                return;
            }

            InsertListener(listener);
        }

        /// <summary>注销回调。</summary>
        /// <param name="callback">要注销的回调。</param>
        /// <returns>找到并注销时返回 true。</returns>
        public bool Remove(LogicEventCallBack callback)
        {
            bool isCallbackNull = callback == null;
            if (isCallbackNull)
                return false;

            int pendingIndex = FindPending(callback);
            bool isInPending = pendingIndex >= 0;
            if (isInPending)
            {
                m_PendingListeners.RemoveAt(pendingIndex);
                return true;
            }

            int index = FindActive(callback);
            bool isInActive = index >= 0;
            if (!isInActive)
                return false;

            CommandListener listener = m_Listeners[index];
            bool isDispatching = m_IsDispatching;
            if (isDispatching)
            {
                listener.isRemoved = true;
                return true;
            }

            m_Listeners.RemoveAt(index);
            return true;
        }

        /// <summary>依次触发全部回调，单个回调异常不会中断后续回调。</summary>
        /// <param name="commandID">触发的动态命令 ID。</param>
        /// <param name="args">命令参数。</param>
        public void Invoke(int commandID, IVarList args)
        {
            bool wasDispatching = m_IsDispatching;
            m_IsDispatching = true;

            for (int i = 0; i < m_Listeners.Count; i++)
            {
                CommandListener listener = m_Listeners[i];
                bool canInvoke = !listener.isRemoved && !listener.isInvoking;
                if (!canInvoke)
                    continue;

                listener.isInvoking = true;
                try
                {
                    LogicEventCallBack callback = listener.callback;
                    callback(commandID, args);
                }
                catch (Exception exception)
                {
                    STLogger.LogErrorF("动态命令 {0} 的回调抛出异常：{1}", commandID, exception);
                }
                finally
                {
                    listener.isInvoking = false;
                }
            }

            m_IsDispatching = wasDispatching;
            bool isTopLevel = !m_IsDispatching;
            if (isTopLevel)
            {
                ApplyPendingListeners();
                RemoveMarkedListeners();
            }
        }

        /// <summary>清空全部监听与待合并记录。</summary>
        public void Clear()
        {
            m_Listeners.Clear();
            m_PendingListeners.Clear();
            m_IsDispatching = false;
        }

        /// <summary>按优先级插入监听，相同优先级保持注册顺序。</summary>
        /// <param name="listener">待插入的监听。</param>
        void InsertListener(CommandListener listener)
        {
            int insertIndex = m_Listeners.Count;
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                bool isLater = m_Listeners[i].prior > listener.prior;
                if (isLater)
                {
                    insertIndex = i;
                    break;
                }
            }

            m_Listeners.Insert(insertIndex, listener);
        }

        /// <summary>把派发期间新增的监听合并到生效列表。</summary>
        void ApplyPendingListeners()
        {
            for (int i = 0; i < m_PendingListeners.Count; i++)
            {
                InsertListener(m_PendingListeners[i]);
            }

            m_PendingListeners.Clear();
        }

        /// <summary>移除派发期间标记为注销的监听。</summary>
        void RemoveMarkedListeners()
        {
            for (int i = m_Listeners.Count - 1; i >= 0; i--)
            {
                bool isRemoved = m_Listeners[i].isRemoved;
                if (isRemoved)
                {
                    m_Listeners.RemoveAt(i);
                }
            }
        }

        /// <summary>判断回调是否已经注册。</summary>
        /// <param name="callback">待检查的回调。</param>
        /// <returns>已注册时返回 true。</returns>
        bool Contains(LogicEventCallBack callback)
        {
            int activeIndex = FindActive(callback);
            bool isActive = activeIndex >= 0;
            if (isActive)
                return true;

            int pendingIndex = FindPending(callback);
            bool isPending = pendingIndex >= 0;
            return isPending;
        }

        /// <summary>在生效列表中查找回调下标。</summary>
        /// <param name="callback">待查找的回调。</param>
        /// <returns>找到时返回下标，否则返回 -1。</returns>
        int FindActive(LogicEventCallBack callback)
        {
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                bool isSame = m_Listeners[i].callback == callback;
                if (isSame)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>在待合并列表中查找回调下标。</summary>
        /// <param name="callback">待查找的回调。</param>
        /// <returns>找到时返回下标，否则返回 -1。</returns>
        int FindPending(LogicEventCallBack callback)
        {
            for (int i = 0; i < m_PendingListeners.Count; i++)
            {
                bool isSame = m_PendingListeners[i].callback == callback;
                if (isSame)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
