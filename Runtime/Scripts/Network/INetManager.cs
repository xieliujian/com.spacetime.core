using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace ST.Core.Network
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class INetManager : IManager
    {
        public INetManager()
        {
            s_Instance = this;
        }


        protected static INetManager s_Instance = null;


        public static INetManager S
        {
            get { return s_Instance; }
        }



        //protected GameEvent<ulong, byte[]> m_onLuaMsgEvent = new GameEvent<ulong, byte[]>();
        ///// <summary>
        ///// lua事件回调
        ///// </summary>
        //public GameEvent<ulong, byte[]> onLuaMsgEvent
        //{
        //    get { return m_onLuaMsgEvent; }
        //}

        public Action<ulong, byte[]> onMsgEvent;


        protected GameEvent m_onConnectEvent = new GameEvent();
        /// <summary>
        /// 连接事件
        /// </summary>
        public GameEvent onConnectEvent
        {
            get { return m_onConnectEvent; }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public override void DoClose()
        {
            //m_onLuaMsgEvent.RemoveAllListeners();
            //m_onLuaMsgEvent.Invoke(0, null);

            m_onConnectEvent.RemoveAllListeners();
            m_onConnectEvent.Invoke();
        }
        
        /// <summary>
        /// 发送链接请求
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public abstract void SendConnect(string address, int port);

        /// <summary>
        /// 关闭连接
        /// </summary>
        public abstract void CloseSocket();

        /// <summary>
        /// 是否连接
        /// </summary>
        /// <returns></returns>
        public abstract bool IsConnected();

        /// <summary>
        /// 发送SOCKET消息
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void SendMessage(ByteBuffer bytebuf);

        /// <summary>
        /// 增加事件
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="bytearray"></param>
        public abstract void AddEvent(ulong msgid, byte[] bytearray);

    }
}
