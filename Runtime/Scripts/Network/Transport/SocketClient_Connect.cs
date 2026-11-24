using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// TCP 客户端的连接建立部分：域名解析放到线程池，解析结果回到主线程再发起连接，避免 DNS 卡住主线程。
    /// 连接状态与关闭逻辑见 SocketClient.cs。
    /// </summary>
    public partial class SocketClient
    {
        /// <summary>域名解析结果，回调线程创建后不再修改，主线程读取。</summary>
        sealed class DnsResult
        {
            public int sessionID;
            public IPAddress address;
            public string error;
        }

        /// <summary>一次域名解析请求的上下文，只用于把会话序号带回主线程。</summary>
        sealed class DnsRequest
        {
            public int sessionID;
        }

        /// <summary>最近一次解析结果，主线程消费后置空。</summary>
        volatile DnsResult m_DnsResult;

        /// <summary>向线程池发起域名解析，结果在主线程 Update 中生效。</summary>
        /// <param name="host">服务器域名。</param>
        /// <param name="sessionID">发起解析时的会话序号，用于丢弃过期结果。</param>
        void BeginDnsResolve(string host, int sessionID)
        {
            DnsRequest request = new DnsRequest();
            request.sessionID = sessionID;

            try
            {
                Dns.BeginGetHostAddresses(host, OnDnsResolved, request);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_CONNECT_FAILED, e.Message);
            }
        }

        /// <summary>域名解析回调，只保存结果，不直接操作连接。</summary>
        /// <param name="asr">异步解析结果。</param>
        void OnDnsResolved(IAsyncResult asr)
        {
            DnsRequest request = (DnsRequest)asr.AsyncState;
            DnsResult result = new DnsResult();
            result.sessionID = request.sessionID;

            try
            {
                IPAddress[] addresses = Dns.EndGetHostAddresses(asr);
                result.address = SelectAddress(addresses);
                bool hasAddress = result.address != null;
                if (!hasAddress)
                {
                    result.error = "domain resolved no address";
                }
            }
            catch (Exception e)
            {
                result.error = e.Message;
            }

            m_DnsResult = result;
        }

        /// <summary>主线程处理域名解析结果，会话不匹配或已不在连接中时丢弃。</summary>
        void ApplyDnsResult()
        {
            DnsResult result = m_DnsResult;
            bool hasResult = result != null;
            if (!hasResult)
                return;

            m_DnsResult = null;

            // DNS 返回时用户可能已经重连或断开，按会话与当前状态过滤过期结果。
            bool isConnecting = m_State == SocketState.Connecting;
            bool isCurrentSession = result.sessionID == m_SessionID;
            bool canApply = isConnecting && isCurrentSession;
            if (!canApply)
                return;

            bool hasAddress = result.address != null;
            if (!hasAddress)
            {
                CloseWithReason(NetworkDefine.REASON_CONNECT_FAILED, result.error);
                return;
            }

            StartConnect(result.address);
        }

        /// <summary>关闭旧连接后向指定地址发起连接。</summary>
        /// <param name="host">服务器 IP 或域名。</param>
        /// <param name="port">端口号。</param>
        public void Connect(string host, int port)
        {
            // 主动重连不投递断线事件，避免业务在 onDisconnectEvent 里重连造成事件自激。
            CloseInternal(NetworkDefine.REASON_NEW_CONNECT, notify: false);

            m_Host = host;
            m_Port = port;
            STLogger.LogInfoF(NetworkDefine.LOG_CONNECT_START, host, port);

            m_SessionID++;
            m_NetModule.BeginSession(m_SessionID);
            m_ConnectWaitTime = 0f;
            m_State = SocketState.Connecting;
            ResetRecvBuffer();
            ResetSendBuffer();

            IPAddress address;
            bool isIpAddress = IPAddress.TryParse(host, out address);
            if (isIpAddress)
            {
                StartConnect(address);
                return;
            }

            BeginDnsResolve(host, m_SessionID);
        }

        /// <summary>连接建立回调，完成 <c>EndConnect</c> 后启动首次异步读取。</summary>
        /// <param name="asr">异步连接结果。</param>
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
                m_NetModule.PostConnected(m_SessionID);
                BeginRead(m_NetStream);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_CONNECT_CALLBACK_FAILED, e.Message);
            }
        }

        /// <summary>连接超时检测，只在连接建立阶段生效，DNS 解析时间也计入超时。</summary>
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

        /// <summary>用解析后的地址创建 TcpClient 并发起异步连接。</summary>
        /// <param name="address">服务器地址。</param>
        void StartConnect(IPAddress address)
        {
            TcpClient client = new TcpClient(address.AddressFamily);
            client.SendTimeout = NetworkDefine.SEND_TIMEOUT_MS;
            client.ReceiveTimeout = 1000;
            client.NoDelay = true;
            m_Client = client;

            try
            {
                client.BeginConnect(address, m_Port, m_ConnectCallback, client);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_CONNECT_FAILED, e.Message);
            }
        }

        /// <summary>优先选择 IPv4 地址，没有 IPv4 时退回第一个地址。</summary>
        /// <param name="addresses">解析得到的地址列表。</param>
        /// <returns>选中的地址，列表为空时返回 null。</returns>
        static IPAddress SelectAddress(IPAddress[] addresses)
        {
            bool hasAddresses = addresses != null && addresses.Length > 0;
            if (!hasAddresses)
                return null;

            for (int i = 0; i < addresses.Length; i++)
            {
                bool isIpv4 = addresses[i].AddressFamily == AddressFamily.InterNetwork;
                if (isIpv4)
                {
                    return addresses[i];
                }
            }

            return addresses[0];
        }
    }
}
