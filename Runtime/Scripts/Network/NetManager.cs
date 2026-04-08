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

        /// <summary>
        /// 连接事件
        /// </summary>
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

        /// <summary>
        /// 初始化
        /// </summary>
        public override void DoInit()
        {
            if (m_SocketClient == null)
                return;

            m_SocketClient.OnRegister();
        }

        /// <summary>
        /// 刷新
        /// </summary>
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

        /// <summary>
        /// 发送链接请求
        /// </summary>
        public void SendConnect(string address, int port)
        {
            m_SocketClient.Close();
            m_SocketClient.SendConnect(address, port);
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void CloseSocket()
        {
            m_SocketClient.Close();
        }

        /// <summary>
        /// 是否连接
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            if (m_SocketClient == null)
                return false;

            return m_SocketClient.IsConnected();
        }

        /// <summary>
        /// 发送SOCKET消息
        /// </summary>
        public void SendMessage(ByteBuffer buffer)
        {
            m_SocketClient.SendMessage(buffer);
        }

        /// <summary>
        /// 增加事件
        /// </summary>
        /// <param name="bytearray"></param>
        public void AddEvent(ulong msgid, byte[] bytearray)
        {
            lock (m_EventQueue)
            {
                m_EventQueue.Enqueue(new KeyValuePair<ulong, byte[]>(msgid, bytearray));
            }
        }

        /// <summary>
        /// 刷新事件队列
        /// </summary>
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


