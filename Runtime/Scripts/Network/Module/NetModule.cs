using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 网络模块：维护底层客户端与模块间依赖，实例由 GameWorld 注册。
    /// 主线程消息队列与连接事件队列见 NetModule_Event.cs。
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
        }

        /// <summary>清空监听与队列并关闭连接。</summary>
        public override void DoClose()
        {
            m_OnConnectEvent.RemoveAllListeners();
            m_OnDisconnectEvent.RemoveAllListeners();
            onMsgEvent = null;
            m_MsgModule = null;

            m_SocketClient.Close(NetworkDefine.REASON_NET_MODULE_CLOSED);
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

        /// <summary>将 <see cref="ByteBuffer"/> 中已写入的内容通过底层客户端发送，发送后不关闭缓冲。</summary>
        /// <param name="buffer">已写好包体的缓冲实例。</param>
        public void SendMessage(ByteBuffer buffer)
        {
            m_SocketClient.SendMessage(buffer);
        }

        /// <summary>供测试或手动装配时直接指定消息分发模块。</summary>
        /// <param name="msgModule">消息分发模块。</param>
        internal void SetMsgModule(MsgModule msgModule)
        {
            m_MsgModule = msgModule;
        }
    }
}
