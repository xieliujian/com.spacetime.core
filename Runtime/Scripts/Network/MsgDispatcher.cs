using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using pb = Google.Protobuf;
using System.IO;
using Google.Protobuf;
using FlatBuffers;

namespace ST.Core.Network
{
    public class MsgDispatcher : IManager
    {
        /// <summary>
        /// 
        /// </summary>
        static MsgDispatcher s_Instance;

        /// <summary>
        /// 
        /// </summary>
        IMsgType m_MsgType = IMsgType.Protobuf;

        /// <summary>
        /// fb消息句柄
        /// </summary>
        Dictionary<ulong, IFlatBufferProcFun> m_FbMsgProcDict = new Dictionary<ulong, IFlatBufferProcFun>(CommonDefine.s_ListConst_100);
        FlatBuffers.FlatBufferBuilder m_FlatBufferBuilder = new FlatBuffers.FlatBufferBuilder(CommonDefine.s_ListConst_1024);

        /// <summary>
        /// pb消息句柄
        /// </summary>
        Dictionary<ulong, IProtobufProcFun> m_PbMsgProcDict = new Dictionary<ulong, IProtobufProcFun>(CommonDefine.s_ListConst_100);

        /// <summary>
        /// 
        /// </summary>
        public static MsgDispatcher S
        {
            get { return s_Instance; }
        }

        /// <summary>
        /// flatBufferBuilder
        /// </summary>
        public FlatBuffers.FlatBufferBuilder flatBufferBuilder
        {
            get
            {
                m_FlatBufferBuilder.Clear();
                return m_FlatBufferBuilder;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public MsgDispatcher()
        {
            s_Instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void DoClose()
        {
            m_FbMsgProcDict.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void DoInit()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public override void DoUpdate()
        {

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
        public void Dispatcher(ulong msgid, byte[] bytearray)
        {
            if (m_MsgType == IMsgType.FlatBuffer)
            {
                DispatcherFbMsg(msgid, bytearray);
            }
            else if (m_MsgType == IMsgType.Protobuf)
            {
                DispatcherPbMsg(msgid, bytearray);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fbfunc"></param>
        public void RegisterFBMsg<T>(MsgProcDelegate<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID", BindingFlags.Static | BindingFlags.Public);
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            IFlatBufferProcFun exist;
            if (m_FbMsgProcDict.TryGetValue(hashid, out exist))
            {
                Debugger.Debugger.LogError("FBMsgProc Exist! " + type.Name);
            }
            else
            {
                m_FbMsgProcDict.Add(hashid, new FlatBufferProcFun<T>(fbfunc));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fbfunc"></param>
        public void UnRegisterFBMsg<T>(MsgProcDelegate<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject
{
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID");
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            m_FbMsgProcDict.Remove(hashid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pbfunc"></param>
        public void RegisterPBMsg<T>(MsgProcDelegate<T> pbfunc) where T : pb::IMessage
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID", BindingFlags.Static | BindingFlags.Public);
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            IProtobufProcFun exist;
            if (m_PbMsgProcDict.TryGetValue(hashid, out exist))
            {
                Debugger.Debugger.LogError("PBMsgProc Exist! " + type.Name);
            }
            else
            {
                m_PbMsgProcDict.Add(hashid, new ProtobufProcFun<T>(pbfunc));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pbfunc"></param>
        public void UnRegisterPBMsg<T>(MsgProcDelegate<T> pbfunc) where T : pb::IMessage
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID");
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            m_PbMsgProcDict.Remove(hashid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="message"></param>
        public void SendPBMsg(ulong msgid, pb.IMessage message)
        {
            ByteBuffer buff = new ByteBuffer();

            byte[] msgbytes;
            using (MemoryStream ms = new MemoryStream())
            {
                message.WriteTo(ms);
                msgbytes = ms.ToArray();
            }

            int msglen = msgbytes.Length;
            UInt16 lengh = (UInt16)(msglen + sizeof(ulong));

            //UInt16 biglen = Converter.GetBigEndian(lengh);
            //buff.WriteShort(biglen);

            buff.WriteShort(lengh);

            buff.WriteUlong(msgid);
            buff.WriteBytes(msgbytes);

            if (NetManager.S != null)
            {
                NetManager.S.SendMessage(buff);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="builder"></param>
        public void SendFBMsg(ulong msgid, FlatBufferBuilder builder)
        {
            // 这里做了优化处理，不从flatbuffer里面复制一份数据出来， 而是直接取数据, 减少一次拷贝
            int msgpos = builder.DataBuffer.Position;
            int msglen = builder.DataBuffer.Length - builder.DataBuffer.Position;

            ByteBuffer buff = new ByteBuffer();

            UInt16 lengh = (UInt16)(msglen + sizeof(ulong));

            //UInt16 biglen = Converter.GetBigEndian(lengh);
            //buff.WriteShort(biglen);

            buff.WriteShort(lengh);

            buff.WriteUlong(msgid);
            buff.WriteBytes(builder.DataBuffer.RawBuffer, msgpos, msglen);

            if (NetManager.S != null)
            {
                NetManager.S.SendMessage(buff);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="bytearray"></param>
        void DispatcherFbMsg(ulong msgid, byte[] bytearray)
        {
            IFlatBufferProcFun procfunc;
            if (m_FbMsgProcDict.TryGetValue(msgid, out procfunc))
            {
                procfunc.Invoke(bytearray);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgid"></param>
        /// <param name="bytearray"></param>
        void DispatcherPbMsg(ulong msgid, byte[] bytearray)
        {
            IProtobufProcFun procfunc;
            if (m_PbMsgProcDict.TryGetValue(msgid, out procfunc))
            {
                procfunc.Invoke(bytearray);
            }
        }
    }
}





// 这个是从flatbuffer里面复制一份数据出来
//byte[] bytearray = builder.DataBuffer.ToSizedArray();

//gtmInterface.ByteBuffer buff = new gtmInterface.ByteBuffer();
//UInt16 lengh = (UInt16)(bytearray.Length + sizeof(ulong));
//UInt16 biglen = Converter.GetBigEndian(lengh);
//buff.WriteShort(biglen);
//buff.WriteUlong(msgid);
//buff.WriteBytes(bytearray);
