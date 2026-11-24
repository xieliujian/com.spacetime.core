using System.Collections.Generic;

namespace ST.Core.Network
{
    /// <summary>
    /// 网络模块定义部分，集中存放底层依赖、对外事件与主线程队列。
    /// </summary>
    public partial class NetModule
    {
        // 依赖模块定义
        /// <summary>底层 TCP 客户端实现。</summary>
        SocketClient m_SocketClient;

        /// <summary>消息分发模块，在 <see cref="DoInit"/> 中从内核解析。</summary>
        MsgModule m_MsgModule;

        // 事件定义
        /// <summary>TCP 连接成功后触发的事件。</summary>
        RunEvent m_OnConnectEvent = new RunEvent();

        /// <summary>连接断开或建立失败时触发的事件，参数为原因。</summary>
        RunEvent<string> m_OnDisconnectEvent = new RunEvent<string>();

        // 队列定义
        /// <summary>网络线程写入、主线程消费的待分发消息队列。</summary>
        Queue<KeyValuePair<ulong, byte[]>> m_EventQueue =
            new Queue<KeyValuePair<ulong, byte[]>>(CommonDefine.s_ListConst_100);

        /// <summary>当帧取出的消息，复用列表避免每帧分配。</summary>
        List<KeyValuePair<ulong, byte[]>> m_ProcessList =
            new List<KeyValuePair<ulong, byte[]>>(NetworkDefine.MAX_PROCESS_MSG_PER_FRAME);

        /// <summary>网络线程产生的连接事件，按入队顺序在主线程处理。</summary>
        Queue<NetSocketEvent> m_SocketEventQueue = new Queue<NetSocketEvent>(CommonDefine.s_ListConst_16);

        // 心跳定义
        /// <summary>业务心跳发送委托，未设置时不启用心跳检测。</summary>
        Run m_HeartbeatSender;

        /// <summary>心跳发送间隔，单位秒。</summary>
        float m_HeartbeatIntervalSeconds;

        /// <summary>心跳超时时间，单位秒。</summary>
        float m_HeartbeatTimeoutSeconds;

        /// <summary>距离上次发送心跳的累计时间，单位秒。</summary>
        float m_HeartbeatSendElapsed;

        /// <summary>距离上次收到消息的累计时间，单位秒。</summary>
        float m_ReceiveIdleElapsed;

        // 公有字段定义
        /// <summary>主线程上、在 <see cref="MsgModule.Dispatcher"/> 之后调用的可选回调，参数为消息 ID 与负载字节。</summary>
        public Run<ulong, byte[]> onMsgEvent;

        // 公有属性定义
        /// <summary>TCP 建立成功后触发的事件（主线程派发）。</summary>
        public RunEvent onConnectEvent
        {
            get
            {
                return m_OnConnectEvent;
            }
        }

        /// <summary>连接断开或建立失败时触发的事件（主线程派发）。</summary>
        public RunEvent<string> onDisconnectEvent
        {
            get
            {
                return m_OnDisconnectEvent;
            }
        }
    }
}
