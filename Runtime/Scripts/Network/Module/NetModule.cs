using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 网络模块：维护底层客户端与模块间依赖，实例由 GameWorld 注册。
    /// 主线程消息队列与连接事件队列见 NetModule_Event.cs，心跳与超时见 NetModule_Heartbeat.cs。
    /// </summary>
    public partial class NetModule : ILogicModule
    {
        /// <summary>创建网络模块并绑定底层客户端。</summary>
        public NetModule()
        {
            m_SocketClient = new SocketClient(this);
        }

        /// <summary>解析消息分发模块；未注册时仍可通过 <see cref="onMsgEvent"/> 取消息。</summary>
        public override void DoInit()
        {
            bool isMsgModuleResolved = m_MsgModule != null;
            if (isMsgModuleResolved)
                return;

            bool hasKernel = m_Kernel != null;
            if (!hasKernel)
                return;

            m_MsgModule = m_Kernel.GetModule<MsgModule>();
            bool isMsgModuleFound = m_MsgModule != null;
            if (!isMsgModuleFound)
            {
                STLogger.LogWarning("NetModule 未找到 MsgModule，收到消息时只派发 onMsgEvent。");
            }
        }

        /// <summary>驱动连接超时检测与主线程消息分发。</summary>
        public override void DoUpdate()
        {
            m_SocketClient.Update();
            UpdateEventQueue();
            UpdateSocketEventQueue();
            UpdateHeartbeat();
            m_SocketClient.FlushSend();
        }

        /// <summary>清空监听与队列并关闭连接。</summary>
        public override void DoClose()
        {
            m_OnConnectEvent.RemoveAllListeners();
            m_OnDisconnectEvent.RemoveAllListeners();
            onMsgEvent = null;
            m_MsgModule = null;
            ClearHeartbeat();

            m_SocketClient.Close(NetworkDefine.REASON_NET_MODULE_CLOSED);

            long upBytes;
            long downBytes;
            long upMessages;
            long downMessages;
            m_SocketClient.GetNetStat(out upBytes, out downBytes, out upMessages, out downMessages);
            STLogger.LogInfoF(NetworkDefine.LOG_CLOSE_SUMMARY, upBytes, upMessages, downBytes, downMessages);

            ClearEventQueue();
            ClearSocketEventQueue();
        }

        /// <summary>关闭旧连接后向指定地址发起连接。</summary>
        /// <param name="address">服务器 IP 或域名。</param>
        /// <param name="port">端口号。</param>
        public void SendConnect(string address, int port)
        {
            m_SocketClient.Connect(address, port);
        }

        /// <summary>主动关闭底层 TCP 连接。</summary>
        public void CloseSocket()
        {
            m_SocketClient.Close(NetworkDefine.REASON_CLIENT_CLOSED);
        }

        /// <summary>返回底层连接是否处于已连接状态。</summary>
        public bool IsConnected()
        {
            return m_SocketClient.IsConnected();
        }

        /// <summary>将 <see cref="ByteBuffer"/> 中已写入的内容追加到发送缓冲，随本帧批量发出。</summary>
        /// <param name="buffer">已写好包体的缓冲实例。</param>
        public void SendMessage(ByteBuffer buffer)
        {
            m_SocketClient.SendMessage(buffer);
        }

        /// <summary>立即把发送缓冲中的数据写出，供需要马上发包的场景调用。</summary>
        public void Flush()
        {
            m_SocketClient.FlushSend();
        }

        /// <summary>读取累计收发统计，用于诊断与网络监控。</summary>
        /// <param name="upBytes">累计发送字节数，含帧头。</param>
        /// <param name="downBytes">累计接收字节数，含帧头。</param>
        /// <param name="upMessages">累计发送消息条数。</param>
        /// <param name="downMessages">累计接收消息条数。</param>
        public void GetNetStat(out long upBytes, out long downBytes, out long upMessages, out long downMessages)
        {
            m_SocketClient.GetNetStat(out upBytes, out downBytes, out upMessages, out downMessages);
        }

        /// <summary>清零累计收发统计，统计值不会在重连时自动清零。</summary>
        public void ResetNetStat()
        {
            m_SocketClient.ResetNetStat();
        }

        /// <summary>供测试或手动装配时直接指定消息分发模块。</summary>
        /// <param name="msgModule">消息分发模块。</param>
        internal void SetMsgModule(MsgModule msgModule)
        {
            m_MsgModule = msgModule;
        }
    }
}
