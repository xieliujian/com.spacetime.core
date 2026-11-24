using System;
using System.Reflection;
using pb = Google.Protobuf;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 消息分发中心：按 <see cref="MsgCodecType"/> 注册 Protobuf / FlatBuffers 处理器并反序列化回调。
    /// 组包与发送见 MsgModule_Send.cs，实例由 GameWorld 注册。
    /// </summary>
    public partial class MsgModule : ILogicModule
    {
        /// <summary>解析网络模块，供发送消息使用。</summary>
        public override void DoInit()
        {
            bool isNetModuleResolved = m_NetModule != null;
            if (isNetModuleResolved)
                return;

            bool isSendBufferMissing = m_SendBuffer == null;
            if (isSendBufferMissing)
            {
                m_SendBuffer = new ByteBuffer();
            }

            bool hasKernel = m_Kernel != null;
            if (!hasKernel)
                return;

            m_NetModule = m_Kernel.GetModule<NetModule>();
            bool isNetModuleFound = m_NetModule != null;
            if (!isNetModuleFound)
            {
                STLogger.LogError("MsgModule 未找到 NetModule，发送消息将失败。");
            }
        }

        /// <summary>清空处理表并释放发送缓冲。</summary>
        public override void DoClose()
        {
            m_FbMsgProcDict.Clear();
            m_PbMsgProcDict.Clear();
            m_NetModule = null;

            bool isSendBufferValid = m_SendBuffer != null;
            if (isSendBufferValid)
            {
                m_SendBuffer.Close();
                m_SendBuffer = null;
            }
        }

        /// <summary>供测试或手动装配时直接指定网络模块。</summary>
        /// <param name="netModule">网络模块。</param>
        internal void SetNetModule(NetModule netModule)
        {
            m_NetModule = netModule;
        }

        /// <summary>设置后续 <see cref="Dispatcher"/> 与注册接口所使用的序列化类型。</summary>
        /// <param name="msgType">Protobuf 或 FlatBuffer。</param>
        public void RegisterMsgType(MsgCodecType msgType)
        {
            m_MsgType = msgType;
        }

        /// <summary>根据当前序列化类型把二进制负载分发给已注册的处理器。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bytearray">消息体字节。</param>
        public void Dispatcher(ulong msgid, byte[] bytearray)
        {
            bool isFlatBuffer = m_MsgType == MsgCodecType.FlatBuffer;
            if (isFlatBuffer)
            {
                DispatcherFbMsg(msgid, bytearray);
                return;
            }

            bool isProtobuf = m_MsgType == MsgCodecType.Protobuf;
            if (isProtobuf)
            {
                DispatcherPbMsg(msgid, bytearray);
            }
        }

        /// <summary>通过消息类型的静态 <c>HashID</c> 注册 FlatBuffers 处理委托。</summary>
        /// <typeparam name="T">实现 <see cref="FlatBuffers.IFlatbufferObject"/> 的表结构类型。</typeparam>
        /// <param name="fbfunc">解析后的消息回调。</param>
        public void RegisterFBMsg<T>(Run<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject
        {
            ulong hashid;
            bool isHashIdFound = TryGetHashId(typeof(T), out hashid);
            if (!isHashIdFound)
                return;

            bool isRegistered = m_FbMsgProcDict.ContainsKey(hashid);
            if (isRegistered)
            {
                STLogger.LogErrorF("FlatBuffers 消息 {0} 重复注册，hashid:{1}", typeof(T).Name, hashid);
                return;
            }

            m_FbMsgProcDict.Add(hashid, new FlatBufferProcFun<T>(fbfunc));
        }

        /// <summary>按类型静态 <c>HashID</c> 移除 FlatBuffers 注册。</summary>
        /// <typeparam name="T">表类型。</typeparam>
        /// <param name="fbfunc">未使用，仅为签名一致保留。</param>
        public void UnRegisterFBMsg<T>(Run<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject
        {
            ulong hashid;
            bool isHashIdFound = TryGetHashId(typeof(T), out hashid);
            if (!isHashIdFound)
                return;

            m_FbMsgProcDict.Remove(hashid);
        }

        /// <summary>通过类型静态 <c>HashID</c> 注册 Protobuf 处理委托。</summary>
        /// <typeparam name="T">实现 <see cref="pb.IMessage"/> 的消息类型。</typeparam>
        /// <param name="pbfunc">反序列化后的消息回调。</param>
        public void RegisterPBMsg<T>(Run<T> pbfunc) where T : pb::IMessage
        {
            ulong hashid;
            bool isHashIdFound = TryGetHashId(typeof(T), out hashid);
            if (!isHashIdFound)
                return;

            bool isRegistered = m_PbMsgProcDict.ContainsKey(hashid);
            if (isRegistered)
            {
                STLogger.LogErrorF("Protobuf 消息 {0} 重复注册，hashid:{1}", typeof(T).Name, hashid);
                return;
            }

            m_PbMsgProcDict.Add(hashid, new ProtobufProcFun<T>(pbfunc));
        }

        /// <summary>按类型静态 <c>HashID</c> 移除 Protobuf 注册。</summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <param name="pbfunc">未使用，仅为签名一致保留。</param>
        public void UnRegisterPBMsg<T>(Run<T> pbfunc) where T : pb::IMessage
        {
            ulong hashid;
            bool isHashIdFound = TryGetHashId(typeof(T), out hashid);
            if (!isHashIdFound)
                return;

            m_PbMsgProcDict.Remove(hashid);
        }

        /// <summary>查找 FlatBuffers 处理器并调用，异常按消息 ID 记录。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bytearray">消息体字节。</param>
        void DispatcherFbMsg(ulong msgid, byte[] bytearray)
        {
            FlatBufferProcFunBase procfunc;
            bool isFound = m_FbMsgProcDict.TryGetValue(msgid, out procfunc);
            if (!isFound)
            {
                STLogger.LogErrorF("未注册的 FlatBuffers 消息，msgid:{0}", msgid);
                return;
            }

            try
            {
                procfunc.Invoke(bytearray);
            }
            catch (Exception e)
            {
                STLogger.LogErrorF("处理 FlatBuffers 消息失败，msgid:{0}", msgid);
                STLogger.LogError(e.ToString());
            }
        }

        /// <summary>查找 Protobuf 处理器并调用，异常按消息 ID 记录。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bytearray">消息体字节。</param>
        void DispatcherPbMsg(ulong msgid, byte[] bytearray)
        {
            ProtobufProcFunBase procfunc;
            bool isFound = m_PbMsgProcDict.TryGetValue(msgid, out procfunc);
            if (!isFound)
            {
                STLogger.LogErrorF("未注册的 Protobuf 消息，msgid:{0}", msgid);
                return;
            }

            try
            {
                procfunc.Invoke(bytearray);
            }
            catch (Exception e)
            {
                STLogger.LogErrorF("处理 Protobuf 消息失败，msgid:{0}", msgid);
                STLogger.LogError(e.ToString());
            }
        }

        /// <summary>读取类型上的静态 <c>HashID</c>。</summary>
        /// <param name="type">消息类型。</param>
        /// <param name="hashid">读到的 HashID。</param>
        /// <returns>字段存在且值有效时返回 true。</returns>
        static bool TryGetHashId(Type type, out ulong hashid)
        {
            hashid = 0;
            FieldInfo fieldInfo = type.GetField("HashID", BindingFlags.Static | BindingFlags.Public);
            bool isFieldFound = fieldInfo != null;
            if (!isFieldFound)
            {
                STLogger.LogErrorF("类型 {0} 缺少静态 HashID 字段，消息无法注册。", type.Name);
                return false;
            }

            object value = fieldInfo.GetValue(null);
            bool isValueValid = value != null;
            if (!isValueValid)
            {
                STLogger.LogErrorF("类型 {0} 的 HashID 为 null，消息无法注册。", type.Name);
                return false;
            }

            hashid = Convert.ToUInt64(value);
            return true;
        }
    }
}
