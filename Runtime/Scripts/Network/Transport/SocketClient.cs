using System;
using System.Net.Sockets;
using System.Threading;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 基于 <see cref="TcpClient"/> 的 TCP 客户端，负责连接生命周期、接收与批量发送。
    /// 连接建立见 SocketClient_Connect.cs，读取见 SocketClient_Read.cs，拆包见 SocketClient_Frame.cs，
    /// 发送见 SocketClient_Send.cs，统计见 SocketClient_Stat.cs。
    /// </summary>
    public partial class SocketClient
    {
        /// <summary>连接状态。</summary>
        enum SocketState
        {
            WaitConnect,
            Connecting,
            Connected,
            Closed,
        }

        /// <summary>所属网络模块，用于投递消息与连接事件。</summary>
        NetModule m_NetModule;

        /// <summary>底层 TCP 连接句柄。</summary>
        TcpClient m_Client;

        /// <summary>当前连接的数据流，重连后会被替换。</summary>
        NetworkStream m_NetStream;

        /// <summary>缓存的读回调，避免每次 BeginRead 重新分配委托。</summary>
        AsyncCallback m_ReadCallback;

        /// <summary>缓存的连接回调。</summary>
        AsyncCallback m_ConnectCallback;

        /// <summary>当前连接状态，跨线程读写。</summary>
        volatile SocketState m_State = SocketState.WaitConnect;

        /// <summary>连接会话序号，每次发起连接自增，用于隔离上一会话的残留消息。</summary>
        volatile int m_SessionID;

        /// <summary>连接超时计时，单位秒。</summary>
        float m_ConnectWaitTime;

        /// <summary>服务器地址，仅用于日志。</summary>
        string m_Host;

        /// <summary>服务器端口，仅用于日志。</summary>
        int m_Port;

        /// <summary>创建客户端并绑定所属网络模块。</summary>
        /// <param name="netModule">消息与连接事件的接收方。</param>
        public SocketClient(NetModule netModule)
        {
            m_NetModule = netModule;
            m_ReadCallback = OnRead;
            m_ConnectCallback = OnConnect;
            m_MainThreadID = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>返回底层连接是否处于已连接状态。</summary>
        public bool IsConnected()
        {
            return m_State == SocketState.Connected;
        }

        /// <summary>每帧驱动连接超时检测，由 <see cref="NetModule.DoUpdate"/> 调用。</summary>
        public void Update()
        {
            ApplyDnsResult();
            UpdateConnectTimeout();
        }

        /// <summary>主动关闭连接，并向业务层投递断线事件。</summary>
        /// <param name="reason">关闭原因。</param>
        public void Close(string reason)
        {
            SocketState oldState = m_State;
            bool isAlreadyClosed = oldState == SocketState.Closed;
            if (isAlreadyClosed)
                return;

            bool isConnectionActive = oldState == SocketState.Connected || oldState == SocketState.Connecting;
            if (isConnectionActive)
            {
                bool isExpected = IsExpectedCloseReason(reason);
                if (isExpected)
                {
                    STLogger.LogInfoF(NetworkDefine.LOG_CLOSE_REASON, m_Host, m_Port, reason);
                }
                else
                {
                    STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON, m_Host, m_Port, reason);
                }
            }

            CloseInternal(reason, notify: true);
        }

        /// <summary>关闭连接本体，调用前已完成日志记录。</summary>
        /// <param name="reason">关闭原因。</param>
        /// <param name="notify">是否向业务投递断线事件，主动重连时传 false。</param>
        void CloseInternal(string reason, bool notify)
        {
            SocketState oldState = m_State;
            bool isAlreadyClosed = oldState == SocketState.Closed;
            if (isAlreadyClosed)
                return;

            m_State = SocketState.Closed;
            ResetRecvBuffer();
            ResetSendBuffer();
            CloseSocket();

            bool hasConnection = oldState == SocketState.Connected || oldState == SocketState.Connecting;
            bool isNeedNotify = notify && hasConnection;
            if (isNeedNotify)
            {
                m_NetModule.PostDisconnected(reason, m_SessionID);
            }
        }

        /// <summary>业务主动关闭与重连属于预期行为，日志使用 Info 级别。</summary>
        /// <param name="reason">关闭原因。</param>
        /// <returns>属于预期关闭时返回 true。</returns>
        static bool IsExpectedCloseReason(string reason)
        {
            bool isClientClosed = reason == NetworkDefine.REASON_CLIENT_CLOSED;
            bool isModuleClosed = reason == NetworkDefine.REASON_NET_MODULE_CLOSED;
            bool isReconnect = reason == NetworkDefine.REASON_NEW_CONNECT;
            bool isExpected = isClientClosed || isModuleClosed || isReconnect;
            return isExpected;
        }

        /// <summary>记录原因后关闭连接。</summary>
        /// <param name="reason">关闭原因。</param>
        void CloseWithReason(string reason)
        {
            STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON, m_Host, m_Port, reason);
            CloseInternal(reason, notify: true);
        }

        /// <summary>记录原因与整型细节后关闭连接，细节只写入日志，避免拼接字符串产生 GC。</summary>
        /// <param name="reason">关闭原因常量。</param>
        /// <param name="detail">随日志输出的整型细节。</param>
        void CloseWithReason(string reason, int detail)
        {
            STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON_VALUE, m_Host, m_Port, reason, detail);
            CloseInternal(reason, notify: true);
        }

        /// <summary>记录原因与文本细节后关闭连接，细节只写入日志，避免拼接字符串产生 GC。</summary>
        /// <param name="reason">关闭原因常量。</param>
        /// <param name="detail">随日志输出的文本细节，通常为异常信息。</param>
        void CloseWithReason(string reason, string detail)
        {
            STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON_DETAIL, m_Host, m_Port, reason, detail);
            CloseInternal(reason, notify: true);
        }

        /// <summary>释放数据流与 TCP 句柄，不改变对外连接状态。</summary>
        void CloseSocket()
        {
            if (m_NetStream != null)
            {
                try
                {
                    m_NetStream.Close();
                }
                catch (Exception e)
                {
                    STLogger.LogWarning(e.Message);
                }

                m_NetStream = null;
            }

            if (m_Client != null)
            {
                try
                {
                    m_Client.Close();
                }
                catch (Exception e)
                {
                    STLogger.LogWarning(e.Message);
                }

                m_Client = null;
            }
        }
    }
}
