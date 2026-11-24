using System;
using System.Net.Sockets;
using ST.Core.Logging;
using UnityEngine;

namespace ST.Core.Network
{
    /// <summary>
    /// 基于 <see cref="TcpClient"/> 的 TCP 客户端，负责连接生命周期、接收与批量发送。
    /// 粘包拆包在回调线程完成，消息通过 <see cref="NetModule.AddEvent"/> 切回主线程分发；发送实现见 SocketClient_Send.cs。
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

        /// <summary>当前连接状态。</summary>
        SocketState m_State = SocketState.WaitConnect;

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
        }

        /// <summary>返回底层连接是否处于已连接状态。</summary>
        public bool IsConnected()
        {
            return m_State == SocketState.Connected;
        }

        /// <summary>每帧驱动连接超时检测，由 <see cref="NetModule.DoUpdate"/> 调用。</summary>
        public void Update()
        {
            UpdateConnectTimeout();
        }

        /// <summary>关闭旧连接后向指定地址发起连接。</summary>
        /// <param name="host">服务器 IP 或域名。</param>
        /// <param name="port">端口号。</param>
        public void Connect(string host, int port)
        {
            Close(NetworkDefine.REASON_NEW_CONNECT);

            m_Host = host;
            m_Port = port;
            STLogger.LogInfoF(NetworkDefine.LOG_CONNECT_START, host, port);
            m_ConnectWaitTime = 0f;
            m_State = SocketState.Connecting;
            ResetRecvBuffer();

            TcpClient client = new TcpClient();
            client.SendTimeout = NetworkDefine.SEND_TIMEOUT_MS;
            client.ReceiveTimeout = 1000;
            client.NoDelay = true;
            m_Client = client;

            try
            {
                client.BeginConnect(host, port, m_ConnectCallback, client);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_CONNECT_FAILED, e.Message);
            }
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
                STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON, m_Host, m_Port, reason);
            }

            CloseInternal(reason);
        }

        /// <summary>关闭连接本体，调用前已完成日志记录。</summary>
        /// <param name="reason">关闭原因。</param>
        void CloseInternal(string reason)
        {
            SocketState oldState = m_State;
            bool isAlreadyClosed = oldState == SocketState.Closed;
            if (isAlreadyClosed)
                return;

            m_State = SocketState.Closed;
            ResetRecvBuffer();
            ResetSendBuffer();
            CloseSocket();

            bool isNeedNotify = oldState == SocketState.Connected || oldState == SocketState.Connecting;
            if (isNeedNotify)
            {
                m_NetModule.PostDisconnected(reason);
            }
        }

        /// <summary>连接建立回调，完成 <c>EndConnect</c> 后启动首次异步读取。</summary>
        void OnConnect(IAsyncResult asr)
        {
            TcpClient client = m_Client;
            bool isCurrentClient = client != null && ReferenceEquals(asr.AsyncState, client);
            if (!isCurrentClient)
                return;

            try
            {
                client.EndConnect(asr);
                m_NetStream = client.GetStream();
                m_State = SocketState.Connected;
                STLogger.LogInfoF(NetworkDefine.LOG_CONNECT_SUCCESS, m_Host, m_Port);
                m_NetModule.PostConnected();
                BeginRead(m_NetStream);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_CONNECT_CALLBACK_FAILED, e.Message);
            }
        }

        /// <summary>读取回调，把收到的字节送入拆包缓冲后继续监听下一次读取。</summary>
        void OnRead(IAsyncResult asr)
        {
            NetworkStream stream = m_NetStream;
            bool isCurrentStream = stream != null && ReferenceEquals(asr.AsyncState, stream);
            if (!isCurrentStream)
                return;

            int bytesRead = 0;
            try
            {
                bytesRead = stream.EndRead(asr);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_READ_FAILED, e.Message);
                return;
            }

            bool isConnectionAlive = bytesRead > 0;
            if (!isConnectionAlive)
            {
                CloseWithReason(NetworkDefine.REASON_SERVER_CLOSED);
                return;
            }

            AddDownBytesStat(bytesRead);

            bool isAppended = AppendRecvData(bytesRead);
            if (isAppended)
            {
                ParseFrames();
            }

            bool isStillConnected = m_State == SocketState.Connected;
            if (isStillConnected)
            {
                BeginRead(stream);
            }
        }

        /// <summary>向当前数据流投递一次异步读取。</summary>
        void BeginRead(NetworkStream stream)
        {
            bool isStreamValid = stream != null;
            if (!isStreamValid)
                return;

            try
            {
                stream.BeginRead(GetReadBuffer(), 0, GetReadBufferLength(), m_ReadCallback, stream);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_BEGIN_READ_FAILED, e.Message);
            }
        }

        /// <summary>连接超时检测，只在连接建立阶段生效。</summary>
        void UpdateConnectTimeout()
        {
            bool isConnecting = m_State == SocketState.Connecting;
            if (!isConnecting)
                return;

            m_ConnectWaitTime += Time.unscaledDeltaTime;
            float elapsedMilliseconds = m_ConnectWaitTime * 1000f;
            bool isTimeout = elapsedMilliseconds >= NetworkDefine.CONNECT_TIMEOUT_MS;
            if (!isTimeout)
                return;

            CloseWithReason(NetworkDefine.REASON_CONNECT_TIMEOUT);
        }

        /// <summary>记录原因后关闭连接。</summary>
        /// <param name="reason">关闭原因。</param>
        void CloseWithReason(string reason)
        {
            STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON, m_Host, m_Port, reason);
            CloseInternal(reason);
        }

        /// <summary>记录原因与整型细节后关闭连接，细节只写入日志，避免拼接字符串产生 GC。</summary>
        /// <param name="reason">关闭原因常量。</param>
        /// <param name="detail">随日志输出的整型细节。</param>
        void CloseWithReason(string reason, int detail)
        {
            STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON_VALUE, m_Host, m_Port, reason, detail);
            CloseInternal(reason);
        }

        /// <summary>记录原因与文本细节后关闭连接，细节只写入日志，避免拼接字符串产生 GC。</summary>
        /// <param name="reason">关闭原因常量。</param>
        /// <param name="detail">随日志输出的文本细节，通常为异常信息。</param>
        void CloseWithReason(string reason, string detail)
        {
            STLogger.LogWarningF(NetworkDefine.LOG_CLOSE_REASON_DETAIL, m_Host, m_Port, reason, detail);
            CloseInternal(reason);
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
