using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace ST.Core.Network
{  
    /// <summary>
    /// 
    /// </summary>
    public class NetManager : IManager
    {
        /// <summary>
        /// 事件队列
        /// </summary>
        static NetManager s_Instance;
        static Queue<KeyValuePair<ulong, byte[]>> m_EventQueue = new Queue<KeyValuePair<ulong, byte[]>>();

        /// <summary>
        /// Socket
        /// </summary>
        SocketClient m_SocketClient = new SocketClient();
        GameEvent m_OnConnectEvent = new GameEvent();

        /// <summary>
        /// 
        /// </summary>
        public Action<ulong, byte[]> onMsgEvent;

        /// <summary>
        /// 
        /// </summary>
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
        /// 
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
        /// 
        /// </summary>
        public override void DoLateUpdate()
        {
            
        }

        /// <summary>
        /// 
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


