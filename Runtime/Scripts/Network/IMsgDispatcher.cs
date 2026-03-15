using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FlatBuffers;
using pb = global::Google.Protobuf;

namespace ST.Core.Network
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class IMsgDispatcher : IManager
    {
        /// <summary>
        /// 
        /// </summary>
        protected IMsgType m_MsgType = IMsgType.Protobuf;

        /// <summary>
        /// 
        /// </summary>
        protected static IMsgDispatcher s_Instance;

        /// <summary>
        /// 
        /// </summary>
        public static IMsgDispatcher S
        {
            get { return s_Instance; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IMsgDispatcher()
        {
            s_Instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual FlatBuffers.FlatBufferBuilder flatBufferBuilder
        {
            get { return null; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgtype"></param>
        public void RegisterMsgType(IMsgType msgtype)
        {
            m_MsgType = msgtype;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="bytearray"></param>
        public abstract void Dispatcher(ulong msgid, byte[] bytearray);
        public virtual void RegisterFBMsg<T>(MsgProcDelegate<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject { }
        public virtual void UnRegisterFBMsg<T>(MsgProcDelegate<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject { }
        public virtual void RegisterPBMsg<T>(MsgProcDelegate<T> pbfunc) where T : pb::IMessage { }
        public virtual void UnRegisterPBMsg<T>(MsgProcDelegate<T> pbfunc) where T : pb::IMessage { }
        public virtual void SendFBMsg(ulong msgid, FlatBufferBuilder builder) { }
        public virtual void SendPBMsg(ulong msgid, pb.IMessage message) { }
    }
}
