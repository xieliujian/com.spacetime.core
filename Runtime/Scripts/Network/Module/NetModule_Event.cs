using System.Collections.Generic;

using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 网络模块的主线程队列部分：接收网络线程投递的消息与连接事件，在 DoUpdate 中按帧分发。
    /// </summary>
    public partial class NetModule
    {
        /// <summary>网络线程调用的消息入队入口。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bytearray">消息体字节。</param>
        /// <param name="sessionID">消息所属会话序号。</param>
        internal void AddEvent(ulong msgid, byte[] bytearray, int sessionID)
        {
            lock (m_EventQueue)
            {
                // 连接已关闭或正在重连时，读回调里的残留数据不再接收。
                bool isConnected = m_SocketClient != null && m_SocketClient.IsConnected();
                if (!isConnected)
                    return;

                // 上一会话的读回调可能晚于关闭到达，按会话序号丢弃，避免新会话收到旧数据。
                bool isCurrentSession = sessionID == m_AcceptedSessionID;
                if (!isCurrentSession)
                    return;

                m_EventQueue.Enqueue(new KeyValuePair<ulong, byte[]>(msgid, bytearray));
            }
        }

        /// <summary>开始一个新会话，之后的残留数据按会话序号过滤。</summary>
        /// <param name="sessionID">新会话序号。</param>
        internal void BeginSession(int sessionID)
        {
            m_AcceptedSessionID = sessionID;
        }

        /// <summary>结束当前会话，之后到达的旧数据全部丢弃。</summary>
        internal void EndSession()
        {
            m_AcceptedSessionID = 0;
        }

        /// <summary>网络线程调用的连接成功入口。</summary>
        /// <param name="sessionID">连接成功时所属会话序号。</param>
        internal void PostConnected(int sessionID)
        {
            NetSocketEvent socketEvent = new NetSocketEvent();
            socketEvent.type = NetSocketEventType.Connected;
            socketEvent.reason = string.Empty;
            socketEvent.sessionID = sessionID;
            EnqueueSocketEvent(socketEvent);
        }

        /// <summary>网络线程调用的断线入口。</summary>
        /// <param name="reason">断线原因。</param>
        /// <param name="sessionID">断线时所属会话序号。</param>
        internal void PostDisconnected(string reason, int sessionID)
        {
            NetSocketEvent socketEvent = new NetSocketEvent();
            socketEvent.type = NetSocketEventType.Disconnected;
            socketEvent.reason = reason;
            socketEvent.sessionID = sessionID;
            EnqueueSocketEvent(socketEvent);
        }

        /// <summary>把套接字事件放入主线程待处理队列。</summary>
        /// <param name="socketEvent">套接字事件。</param>
        void EnqueueSocketEvent(NetSocketEvent socketEvent)
        {
            lock (m_SocketEventQueue)
            {
                m_SocketEventQueue.Enqueue(socketEvent);
            }
        }

        /// <summary>每帧最多取出固定条数消息，避免消息突发造成单帧卡顿。</summary>
        void UpdateEventQueue()
        {
            lock (m_EventQueue)
            {
                while (m_ProcessList.Count < NetworkDefine.MAX_PROCESS_MSG_PER_FRAME && m_EventQueue.Count > 0)
                {
                    m_ProcessList.Add(m_EventQueue.Dequeue());
                }
            }

            for (int i = 0; i < m_ProcessList.Count; i++)
            {
                KeyValuePair<ulong, byte[]> message = m_ProcessList[i];
                DispatchMessage(message.Key, message.Value);

                bool isStillConnected = m_SocketClient.IsConnected();
                if (!isStillConnected)
                {
                    // 连接已断开，本帧剩余消息不再交给业务，随列表清空丢弃。
                    int remainingCount = m_ProcessList.Count - i - 1;
                    STLogger.LogDebugF(NetworkDefine.LOG_DISPATCH_STOPPED, remainingCount);
                    break;
                }
            }

            m_ProcessList.Clear();
        }

        /// <summary>先分发给 <see cref="MsgModule"/>，再派发可选的 <see cref="onMsgEvent"/>。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bytearray">消息体字节。</param>
        void DispatchMessage(ulong msgid, byte[] bytearray)
        {
            m_ReceiveIdleElapsed = 0f;

            MsgModule msgModule = m_MsgModule;
            bool hasMsgModule = msgModule != null;
            if (hasMsgModule)
            {
                msgModule.Dispatcher(msgid, bytearray);
            }

            Run<ulong, byte[]> msgEvent = onMsgEvent;
            bool hasMsgEvent = msgEvent != null;
            if (hasMsgEvent)
            {
                ERunner.Run(msgEvent, msgid, bytearray);
            }
        }

        /// <summary>在主线程按入队顺序派发连接与断线事件。</summary>
        void UpdateSocketEventQueue()
        {
            int processedCount = 0;
            while (processedCount < NetworkDefine.MAX_PROCESS_SOCKET_EVENT_PER_FRAME)
            {
                NetSocketEvent socketEvent;
                bool hasEvent;
                lock (m_SocketEventQueue)
                {
                    hasEvent = m_SocketEventQueue.Count > 0;
                    socketEvent = hasEvent ? m_SocketEventQueue.Dequeue() : default(NetSocketEvent);
                }

                bool hasNoEvent = !hasEvent;
                if (hasNoEvent)
                {
                    break;
                }

                processedCount++;
                DispatchSocketEvent(socketEvent);
            }
        }

        /// <summary>派发单个套接字事件。</summary>
        /// <param name="socketEvent">套接字事件。</param>
        void DispatchSocketEvent(NetSocketEvent socketEvent)
        {
            bool isCurrentSession = socketEvent.sessionID == m_AcceptedSessionID;
            if (!isCurrentSession)
                return;

            bool isConnectedEvent = socketEvent.type == NetSocketEventType.Connected;
            if (isConnectedEvent)
            {
                m_OnConnectEvent.Invoke();
                return;
            }

            m_OnDisconnectEvent.Invoke(socketEvent.reason);
        }

        /// <summary>清空待分发消息与临时列表。</summary>
        void ClearEventQueue()
        {
            lock (m_EventQueue)
            {
                m_EventQueue.Clear();
            }

            m_ProcessList.Clear();
        }

        /// <summary>清空待派发的连接事件。</summary>
        void ClearSocketEventQueue()
        {
            lock (m_SocketEventQueue)
            {
                m_SocketEventQueue.Clear();
            }
        }
    }
}
