using System;
using System.Reflection;
using pb = Google.Protobuf;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 消息分发中心：按消息 ID 注册处理委托并反序列化回调。
    /// 处理表不区分 Protobuf 与 FlatBuffers，编码方式由注册时创建的处理器决定，
    /// 因此两种编码可以在同一进程内共存。组包与发送见 MsgModule_Send.cs，实例由 GameWorld 注册。
    /// </summary>
    public partial class MsgModule : ILogicModule
    {
        /// <summary>创建发送缓冲并解析网络模块，供发送消息使用。</summary>
        public override void DoInit()
        {
            bool isSendBufferMissing = m_SendBuffer == null;
            if (isSendBufferMissing)
            {
                m_SendBuffer = new ByteBuffer(NetworkDefine.SEND_BUFFER_INITIAL_CAPACITY);
            }

            ResolveNetModule();
            bool isNetModuleResolved = m_NetModule != null;
            if (!isNetModuleResolved)
            {
                STLogger.LogWarning("MsgModule 初始化时未找到 NetModule，发送时会再次尝试解析。");
            }
        }

        /// <summary>清空处理表并释放发送缓冲与 FlatBuffers 构建器。</summary>
        public override void DoClose()
        {
            m_MsgProcDict.Clear();
            m_NetModule = null;
            m_IsFlatBufferBuilding = false;

            // 构建器持有整块 byte 数组，关闭时置空交回 GC，下次使用时重新创建。
            m_FlatBufferBuilder = null;

            // 先释放 Protobuf 写入器，它绑定发送缓冲的流，需要在流关闭前 Dispose。
            Google.Protobuf.CodedOutputStream pbOutputStream = m_PbOutputStream;
            bool isPbOutputStreamValid = pbOutputStream != null;
            if (isPbOutputStreamValid)
            {
                pbOutputStream.Dispose();
                m_PbOutputStream = null;
            }

            ByteBuffer sendBuffer = m_SendBuffer;
            bool isSendBufferValid = sendBuffer != null;
            if (isSendBufferValid)
            {
                sendBuffer.Close();
                m_SendBuffer = null;
            }
        }

        /// <summary>供测试或手动装配时直接指定网络模块。</summary>
        /// <param name="netModule">网络模块。</param>
        internal void SetNetModule(NetModule netModule)
        {
            m_NetModule = netModule;
        }

        /// <summary>
        /// 根据消息 ID 把二进制负载交给已注册的处理器。
        /// 帧里只有消息 ID，处理器自己知道该按 Protobuf 还是 FlatBuffers 解析。
        /// </summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bytearray">消息体字节。</param>
        public void Dispatcher(ulong msgid, byte[] bytearray)
        {
            MsgProcFunBase procfunc;
            bool isFound = m_MsgProcDict.TryGetValue(msgid, out procfunc);
            if (!isFound)
            {
                STLogger.LogErrorF("未注册的消息，msgid:{0}", msgid);
                return;
            }

            try
            {
                procfunc.Invoke(bytearray);
            }
            catch (Exception e)
            {
                STLogger.LogErrorF("处理消息失败，msgid:{0}，异常:{1}", msgid, e);
            }
        }

        /// <summary>通过类型的静态 <c>HashID</c> 注册 FlatBuffers 处理委托。</summary>
        /// <typeparam name="T">实现 <see cref="FlatBuffers.IFlatbufferObject"/> 的表结构类型。</typeparam>
        /// <param name="fbfunc">解析后的消息回调。</param>
        public void RegisterFBMsg<T>(Run<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject
        {
            bool isCallbackValid = fbfunc != null;
            if (!isCallbackValid)
            {
                STLogger.LogErrorF("FlatBuffers 消息 {0} 的回调为 null，注册被忽略。", typeof(T).Name);
                return;
            }

            ulong hashid;
            bool isHashIdFound = TryGetHashId(typeof(T), out hashid);
            if (!isHashIdFound)
            {
                return;
            }

            bool isRegistered = m_MsgProcDict.ContainsKey(hashid);
            if (isRegistered)
            {
                STLogger.LogErrorF("消息 {0} 重复注册，hashid:{1}", typeof(T).Name, hashid);
                return;
            }

            m_MsgProcDict.Add(hashid, new FlatBufferProcFun<T>(fbfunc));
        }

        /// <summary>按类型的静态 <c>HashID</c> 移除 FlatBuffers 注册。</summary>
        /// <typeparam name="T">表类型。</typeparam>
        /// <param name="fbfunc">保留参数，用于与注册接口保持对称。</param>
        public void UnRegisterFBMsg<T>(Run<T> fbfunc) where T : struct, FlatBuffers.IFlatbufferObject
        {
            UnRegisterMsg(typeof(T));
        }

        /// <summary>通过类型的静态 <c>HashID</c> 注册 Protobuf 处理委托。</summary>
        /// <typeparam name="T">实现 <see cref="pb.IMessage"/> 的消息类型。</typeparam>
        /// <param name="pbfunc">反序列化后的消息回调。</param>
        public void RegisterPBMsg<T>(Run<T> pbfunc) where T : pb::IMessage
        {
            bool isCallbackValid = pbfunc != null;
            if (!isCallbackValid)
            {
                STLogger.LogErrorF("Protobuf 消息 {0} 的回调为 null，注册被忽略。", typeof(T).Name);
                return;
            }

            ulong hashid;
            bool isHashIdFound = TryGetHashId(typeof(T), out hashid);
            if (!isHashIdFound)
            {
                return;
            }

            bool isRegistered = m_MsgProcDict.ContainsKey(hashid);
            if (isRegistered)
            {
                STLogger.LogErrorF("消息 {0} 重复注册，hashid:{1}", typeof(T).Name, hashid);
                return;
            }

            m_MsgProcDict.Add(hashid, new ProtobufProcFun<T>(pbfunc));
        }

        /// <summary>按类型的静态 <c>HashID</c> 移除 Protobuf 注册。</summary>
        /// <typeparam name="T">消息类型。</typeparam>
        /// <param name="pbfunc">保留参数，用于与注册接口保持对称。</param>
        public void UnRegisterPBMsg<T>(Run<T> pbfunc) where T : pb::IMessage
        {
            UnRegisterMsg(typeof(T));
        }

        /// <summary>从内核解析网络模块，发送时找不到会再次尝试。</summary>
        void ResolveNetModule()
        {
            bool isResolved = m_NetModule != null;
            if (isResolved)
            {
                return;
            }

            IKernel kernel = m_Kernel;
            bool hasKernel = kernel != null;
            if (!hasKernel)
            {
                return;
            }

            m_NetModule = kernel.GetModule<NetModule>();
        }

        /// <summary>按消息类型的静态 <c>HashID</c> 从统一处理表移除。</summary>
        /// <param name="type">消息类型。</param>
        void UnRegisterMsg(Type type)
        {
            ulong hashid;
            bool isHashIdFound = TryGetHashId(type, out hashid);
            if (!isHashIdFound)
            {
                return;
            }

            m_MsgProcDict.Remove(hashid);
        }

        /// <summary>读取类型上的静态 <c>HashID</c>。</summary>
        /// <param name="type">消息类型。</param>
        /// <param name="hashid">读到的 HashID。</param>
        /// <returns>字段存在、值非空且能转换为 ulong 时返回 true。</returns>
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

            bool isConverted = TryConvertHashId(value, out hashid);
            if (!isConverted)
            {
                STLogger.LogErrorF("类型 {0} 的 HashID 无法转换为 ulong，消息无法注册。", type.Name);
            }

            return isConverted;
        }

        /// <summary>把 HashID 字段值转换为 ulong，非法值只记录失败，不向注册流程抛异常。</summary>
        /// <param name="value">HashID 字段值。</param>
        /// <param name="hashid">转换结果。</param>
        /// <returns>转换成功时返回 true。</returns>
        static bool TryConvertHashId(object value, out ulong hashid)
        {
            hashid = 0;
            try
            {
                ulong converted = Convert.ToUInt64(value);
                hashid = converted;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }
    }
}

