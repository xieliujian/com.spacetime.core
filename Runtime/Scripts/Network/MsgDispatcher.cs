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
    /// 消息分发中心：按 <see cref="IMsgType"/> 注册 Protobuf / FlatBuffers 处理器，封装组包发送与主线程上的反序列化回调。
    /// </summary>
    public class MsgDispatcher : IManager
    {
        /// <summary>全局分发器实例。</summary>
        static MsgDispatcher s_Instance;

        /// <summary>当前使用的消息序列化后端（Protobuf 或 FlatBuffer）。</summary>
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

        /// <summary>全局消息分发器引用。</summary>
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

        /// <summary>构造时注册为 <see cref="S"/>。</summary>
        public MsgDispatcher()
        {
            s_Instance = this;
        }

        /// <summary>清空 FlatBuffers 消息处理表。</summary>
        public override void DoClose()
        {
            m_FbMsgProcDict.Clear();
        }

        /// <summary>当前无额外初始化逻辑。</summary>
        public override void DoInit()
        {

        }

        /// <summary>当前无每帧逻辑。</summary>
        public override void DoUpdate()
        {

        }

        /// <summary>当前无 LateUpdate 逻辑。</summary>
        public override void DoLateUpdate()
        {

        }

        /// <summary>
        /// 设置后续 <see cref="Dispatcher"/> 与注册接口所使用的序列化类型。
        /// </summary>
        /// <param name="msgtype">Protobuf 或 FlatBuffer</param>
        public void RegisterMsgType(IMsgType msgtype)
        {
            m_MsgType = msgtype;
        }

        /// <summary>
        /// 根据当前 <see cref="IMsgType"/> 将二进制负载分发给已注册的 PB 或 FB 处理器。
        /// </summary>
        /// <param name="msgid">消息 ID（与生成代码中静态 HashID 一致）</param>
        /// <param name="bytearray">消息体字节（不含外层长度与 msgid 时由上游保证）</param>
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
        /// 通过消息类型 <typeparamref name="T"/> 的静态 <c>HashID</c> 注册 FlatBuffers 处理委托。
        /// </summary>
        /// <typeparam name="T">实现 <see cref="FlatBuffers.IFlatbufferObject"/> 的表结构类型</typeparam>
        /// <param name="fbfunc">解析后的消息回调</param>
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

        /// <summary>按类型静态 HashID 移除 FlatBuffers 注册。</summary>
        /// <typeparam name="T">表类型</typeparam>
        /// <param name="fbfunc">未使用，仅为签名一致保留</param>
        public void UnRegisterFBMsg<T>(MsgProcDelegate<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject
{
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID");
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            m_FbMsgProcDict.Remove(hashid);
        }

        /// <summary>
        /// 通过 <typeparamref name="T"/> 的静态 <c>HashID</c> 注册 Protobuf 处理委托。
        /// </summary>
        /// <typeparam name="T">实现 <c>Google.Protobuf.IMessage</c> 的消息类型</typeparam>
        /// <param name="pbfunc">反序列化后的消息回调</param>
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

        /// <summary>按类型静态 HashID 移除 Protobuf 注册。</summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="pbfunc">未使用，仅为签名一致保留</param>
        public void UnRegisterPBMsg<T>(MsgProcDelegate<T> pbfunc) where T : pb::IMessage
        {
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("HashID");
            ulong hashid = (ulong)fieldInfo.GetValue(null);

            m_PbMsgProcDict.Remove(hashid);
        }

        /// <summary>
        /// 将 Protobuf 消息序列化后写入长度前缀与 <paramref name="msgid"/>，经 <see cref="NetManager.SendMessage"/> 发出。
        /// </summary>
        /// <param name="msgid">消息 ID</param>
        /// <param name="message">已填充的消息实例</param>
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
        /// 从 FlatBufferBuilder 当前数据区直接切片组包（避免额外 ToArray 拷贝），经网络层发送。
        /// </summary>
        /// <param name="msgid">消息 ID</param>
        /// <param name="builder">已完成 <c>Finish</c> 的构建器</param>
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

        /// <summary>查找 FB 处理器并反序列化调用。</summary>
        void DispatcherFbMsg(ulong msgid, byte[] bytearray)
        {
            IFlatBufferProcFun procfunc;
            if (m_FbMsgProcDict.TryGetValue(msgid, out procfunc))
            {
                procfunc.Invoke(bytearray);
            }
        }

        /// <summary>查找 PB 处理器并 <c>ParseFrom</c> 后调用。</summary>
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
