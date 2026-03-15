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
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ProtobufProcFun<T> : IProtobufProcFun where T : pb::IMessage
    {
        private MsgProcDelegate<T> m_dlg;

        private pb.MessageParser m_parser;

        private T m_obj;

        public ProtobufProcFun(MsgProcDelegate<T> dlg)
        {
            m_dlg = dlg;

            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("_parser", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                m_parser = (pb.MessageParser)fieldInfo.GetValue(null);
            }
        }

        public override void Invoke(byte[] bytearray)
        {
            m_obj = (T)m_parser.ParseFrom(bytearray);

            try
            {
                if (m_obj != null)
                {
                    m_dlg?.Invoke(m_obj);
                }
            }
            catch (Exception e)
            {
                Debugger.Debugger.LogError("process protobuf msg error!" + typeof(T).FullName);
                Debugger.Debugger.LogError(e.ToString());
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FlatBufferProcFun<T> : IFlatBufferProcFun where T : struct, FlatBuffers.IFlatbufferObject
    {
        private MsgProcDelegate<T> m_dlg;

        private T m_obj = default(T);

        public FlatBufferProcFun(MsgProcDelegate<T> dlg)
        {
            m_dlg = dlg;
        }

        public override void Invoke(byte[] bytearray)
        {
            FlatBuffers.ByteBuffer buf = new FlatBuffers.ByteBuffer(bytearray);
            m_obj.__init(buf.GetInt(buf.Position) + buf.Position, buf);
            
            try
            {
                m_dlg(m_obj);
            }
            catch (Exception e)
            {
                Debugger.Debugger.LogError("process flatbuffer msg error!" + typeof(T).FullName);
                Debugger.Debugger.LogError(e.ToString());
            }
        }
    }

    public class MsgDispatcher : IMsgDispatcher
    {
        #region 变量

        /// <summary>
        /// fb消息句柄
        /// </summary>
        private Dictionary<ulong, IFlatBufferProcFun> m_fbMsgProcDict = new Dictionary<ulong, IFlatBufferProcFun>(CommonDefine.ListConst_100);
        private FlatBuffers.FlatBufferBuilder m_flatBufferBuilder = new FlatBuffers.FlatBufferBuilder(CommonDefine.ListConst_1024);

        /// <summary>
        /// flatBufferBuilder
        /// </summary>
        public override FlatBuffers.FlatBufferBuilder flatBufferBuilder
        {
            get
            {
                m_flatBufferBuilder.Clear();
                return m_flatBufferBuilder;
            }
        }

        /// <summary>
        /// pb消息句柄
        /// </summary>
        private Dictionary<ulong, IProtobufProcFun> m_pbMsgProcDict = new Dictionary<ulong, IProtobufProcFun>(CommonDefine.ListConst_100);

        #endregion

        #region 继承

        public override void Dispatcher(ulong msgid, byte[] bytearray)
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

        public override void DoClose()
        {
            m_fbMsgProcDict.Clear();
        }

        public override void DoInit()
        {
            
        }

        public override void DoUpdate()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        public override void DoLateUpdate()
        {
            
        }

        public override void RegisterFBMsg<T>(MsgProcDelegate<T> fbfunc)
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID", BindingFlags.Static | BindingFlags.Public);
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            IFlatBufferProcFun exist;
            if (m_fbMsgProcDict.TryGetValue(hashid, out exist))
            {
                Debugger.Debugger.LogError("FBMsgProc Exist! " + type.Name);
            }
            else
            {
                m_fbMsgProcDict.Add(hashid, new FlatBufferProcFun<T>(fbfunc));
            }
        }

        public override void UnRegisterFBMsg<T>(MsgProcDelegate<T> fbfunc)
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID");
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            m_fbMsgProcDict.Remove(hashid);
        }

        public override void RegisterPBMsg<T>(MsgProcDelegate<T> pbfunc)
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID", BindingFlags.Static | BindingFlags.Public);
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            IProtobufProcFun exist;
            if (m_pbMsgProcDict.TryGetValue(hashid, out exist))
            {
                Debugger.Debugger.LogError("PBMsgProc Exist! " + type.Name);
            }
            else
            {
                m_pbMsgProcDict.Add(hashid, new ProtobufProcFun<T>(pbfunc));
            }
        }

        public override void UnRegisterPBMsg<T>(MsgProcDelegate<T> pbfunc)
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID");
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            m_pbMsgProcDict.Remove(hashid);
        }

        public override void SendPBMsg(ulong msgid, pb.IMessage message)
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

            if (NetManager.instance != null)
            {
                NetManager.instance.SendMessage(buff);
            }
        }

        public override void SendFBMsg(ulong msgid, FlatBufferBuilder builder)
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

            if (NetManager.instance != null)
            {
                NetManager.instance.SendMessage(buff);
            }
        }

        private void DispatcherFbMsg(ulong msgid, byte[] bytearray)
        {
            IFlatBufferProcFun procfunc;
            if (m_fbMsgProcDict.TryGetValue(msgid, out procfunc))
            {
                procfunc.Invoke(bytearray);
            }
        }

        private void DispatcherPbMsg(ulong msgid, byte[] bytearray)
        {
            IProtobufProcFun procfunc;
            if (m_pbMsgProcDict.TryGetValue(msgid, out procfunc))
            {
                procfunc.Invoke(bytearray);
            }
        }

        #endregion
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
