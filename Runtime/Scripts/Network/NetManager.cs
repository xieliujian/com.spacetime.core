using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace ST.Core.Network
{
    /// <summary>
    /// 网络单例管理器：维护 <see cref="SocketClient"/>、连接事件，并在主线程轮询处理消息队列后分发给 <see cref="MsgDispatcher"/> 与 <see cref="onMsgEvent"/>。
    /// </summary>
    public class NetManager : IManager
    {
        /// <summary>全局 <see cref="NetManager"/> 单例引用。</summary>
        static NetManager s_Instance;
        /// <summary>主线程待处理的消息队列（消息 ID 与负载字节）。</summary>
        static Queue<KeyValuePair<ulong, byte[]>> m_EventQueue = new Queue<KeyValuePair<ulong, byte[]>>();

        /// <summary>底层 TCP 客户端实现。</summary>
        SocketClient m_SocketClient = new SocketClient();
        /// <summary>TCP 连接成功后触发的事件，由 <see cref="SocketClient"/> 回调到主线程。</summary>
        GameEvent m_OnConnectEvent = new GameEvent();

        /// <summary>
        /// 主线程上、在 <see cref="MsgDispatcher.Dispatcher"/> 之后调用的可选回调，参数为消息 ID 与负载字节。
        /// </summary>
        public Action<ulong, byte[]> onMsgEvent;

        /// <summary>全局网络管理器实例。</summary>
        public static NetManager S
        {
            get { return s_Instance; }
        }

        /// <summary>TCP 建立成功后触发的事件（主线程投递，由 <see cref="MainThreadTask"/> 派发）。</summary>
        public GameEvent onConnectEvent
        {
            get { return m_OnConnectEvent; }
        }

        //protected GameEvent<ulong, byte[]> m_onLuaMsgEvent = new GameEvent<ulong, byte[]>();
        ///// <summary>
        ///// lua事件回调
        ///// </summary>
        //public GameEvent<ulong, byte[]> onLuaMsgEvent
        //{
        //    get { return m_onLuaMsgEvent; }
        //}

        /// <summary>
        /// 构造时注册为全局 <see cref="S"/>，供套接字层等访问。
        /// </summary>
        public NetManager()
        {
            s_Instance = this;
        }

        /// <summary>初始化网络模块：向 <see cref="SocketClient"/> 注册读写流。</summary>
        public override void DoInit()
        {
            if (m_SocketClient == null)
                return;

            m_SocketClient.OnRegister();
        }

        /// <summary>每帧驱动 <see cref="UpdateEventQueue"/>，将消息队列分发给 <see cref="MsgDispatcher"/> 与 <see cref="onMsgEvent"/>。</summary>
        public override void DoUpdate()
        {
            UpdateEventQueue();
        }

        /// <summary>
        /// 当前无滞后帧逻辑，空实现。
        /// </summary>
        public override void DoLateUpdate()
        {
            
        }

        /// <summary>
        /// 清空连接事件监听并关闭套接字客户端。
        /// </summary>
        public override void DoClose()
        {
            //m_onLuaMsgEvent.RemoveAllListeners();
            //m_onLuaMsgEvent.Invoke(0, null);
            m_OnConnectEvent.RemoveAllListeners();
            m_OnConnectEvent.Invoke();

            if (m_SocketClient != null)
                m_SocketClient.OnRemove();
        }

        /// <summary>先关闭旧连接再发起新的 TCP 连接请求。</summary>
        /// <param name="address">服务器 IP 或域名。</param>
        /// <param name="port">端口号。</param>
        public void SendConnect(string address, int port)
        {
            m_SocketClient.Close();
            m_SocketClient.SendConnect(address, port);
        }

        /// <summary>主动关闭底层 TCP 连接。</summary>
        public void CloseSocket()
        {
            m_SocketClient.Close();
        }

        /// <summary>返回底层 TCP 连接是否处于已连接状态。</summary>
        public bool IsConnected()
        {
            if (m_SocketClient == null)
                return false;

            return m_SocketClient.IsConnected();
        }

        /// <summary>将 <see cref="ByteBuffer"/> 内容通过 <see cref="SocketClient"/> 异步发送到服务器。</summary>
        /// <param name="buffer">已写入包体的缓冲实例，发送后自动关闭。</param>
        public void SendMessage(ByteBuffer buffer)
        {
            m_SocketClient.SendMessage(buffer);
        }

        /// <summary>将网络线程收到的消息以线程安全方式入队，等待主线程在 <see cref="DoUpdate"/> 中消费。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bytearray">消息体字节。</param>
        public void AddEvent(ulong msgid, byte[] bytearray)
        {
            lock (m_EventQueue)
            {
                m_EventQueue.Enqueue(new KeyValuePair<ulong, byte[]>(msgid, bytearray));
            }
        }

        /// <summary>逐条出队并依次调用 <see cref="MsgDispatcher.Dispatcher"/> 与 <see cref="onMsgEvent"/>。</summary>
        void UpdateEventQueue()
        {
            if (m_EventQueue.Count <= 0)
                return;

            while (m_EventQueue.Count > 0)
            {
                KeyValuePair<ulong, byte[]> keyvaleupair = m_EventQueue.Dequeue();

                if (MsgDispatcher.S != null)
                {
                    MsgDispatcher.S.Dispatcher(keyvaleupair.Key, keyvaleupair.Value);
                }

                onMsgEvent?.Invoke(keyvaleupair.Key, keyvaleupair.Value);
            }
        }
    }
}


