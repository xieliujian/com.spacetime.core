using System;
using Google.Protobuf;
using pb = Google.Protobuf;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 消息模块的发送部分：复用发送缓冲完成组包，消息注册与分发见 MsgModule.cs。
    /// </summary>
    public partial class MsgModule
    {
        /// <summary>
        /// 清空并返回 FlatBuffers 构建器，写完根表后调用 <see cref="EndFlatBuffer"/> 发送。
        /// 构建器由本模块复用，构建中途再次调用会清空上一次未发送的数据。
        /// </summary>
        /// <returns>可继续写入的构建器。</returns>
        public FlatBuffers.FlatBufferBuilder BeginFlatBuffer()
        {
            bool isBuilding = m_IsFlatBufferBuilding;
            if (isBuilding)
            {
                STLogger.LogError("BeginFlatBuffer 在上一次构建结束前被再次调用，之前写入的 FlatBuffers 数据已被清空。");
            }

            FlatBuffers.FlatBufferBuilder builder = m_FlatBufferBuilder;
            bool isBuilderMissing = builder == null;
            if (isBuilderMissing)
            {
                builder = new FlatBuffers.FlatBufferBuilder(CommonDefine.s_ListConst_1024);
                m_FlatBufferBuilder = builder;
            }

            m_IsFlatBufferBuilding = true;
            builder.Clear();
            return builder;
        }

        /// <summary>
        /// 完成 FlatBuffers 构建并按帧发送。外部不需要再调用 <c>Finish</c>，
        /// 由本方法统一处理，避免发出未完成、把整块缓冲当包体的数据。
        /// </summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="rootTable">生成的 Create 方法返回的根表偏移。</param>
        /// <returns>数据已交给网络模块时返回 true。</returns>
        public bool EndFlatBuffer(ulong msgid, int rootTable)
        {
            bool isRootTableValid = rootTable != 0;
            if (!isRootTableValid)
            {
                STLogger.LogErrorF("EndFlatBuffer 收到空的根表偏移，消息未发送，msgid:{0}", msgid);
                return false;
            }

            FlatBuffers.FlatBufferBuilder builder = m_FlatBufferBuilder;
            bool isBuilding = (builder != null) && m_IsFlatBufferBuilding;
            if (!isBuilding)
            {
                STLogger.LogErrorF("EndFlatBuffer 没有对应的 BeginFlatBuffer，消息未发送，msgid:{0}", msgid);
                return false;
            }

            try
            {
                builder.Finish(rootTable);
            }
            catch (Exception e)
            {
                m_IsFlatBufferBuilding = false;
                STLogger.LogErrorF("FlatBuffers Finish 失败，消息未发送，msgid:{0}，异常:{1}", msgid, e);
                return false;
            }

            m_IsFlatBufferBuilding = false;
            bool isSent = SendFlatBufferBody(msgid, builder);
            return isSent;
        }

        /// <summary>
        /// 发送一个已 <c>Finish</c> 的外部构建器。未完成构建的构建器会被拒绝，
        /// 因为此时数据长度无法确定，直接发送会把整块缓冲当作包体。
        /// </summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="builder">已完成 <c>Finish</c> 的构建器。</param>
        /// <returns>数据已交给网络模块时返回 true。</returns>
        public bool SendFBMsg(ulong msgid, FlatBuffers.FlatBufferBuilder builder)
        {
            bool isBuilderValid = builder != null;
            if (!isBuilderValid)
            {
                STLogger.LogErrorF("SendFBMsg 收到 null 构建器，msgid:{0}", msgid);
                return false;
            }

            // 本工程使用的 FlatBuffers 版本只在 Finish 时把 Position 设成数据起点，
            // 未 Finish 时 Position 恒为 0，此时 Length - Position 等于整块容量。
            FlatBuffers.ByteBuffer flatBuffer = builder.DataBuffer;
            int bodyPosition = flatBuffer.Position;
            bool isFinished = bodyPosition > 0;
            if (!isFinished)
            {
                STLogger.LogErrorF("SendFBMsg 收到未 Finish 的构建器，消息未发送，msgid:{0}", msgid);
                return false;
            }

            bool isOwnBuilder = ReferenceEquals(builder, m_FlatBufferBuilder);
            if (isOwnBuilder)
            {
                m_IsFlatBufferBuilding = false;
            }

            bool isSent = SendFlatBufferBody(msgid, builder);
            return isSent;
        }

        /// <summary>将 Protobuf 消息写入复用缓冲后加长度前缀与消息 ID 发送。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="message">已填充的消息实例。</param>
        /// <returns>数据已交给网络模块时返回 true。</returns>
        public bool SendPBMsg(ulong msgid, pb.IMessage message)
        {
            bool isMessageValid = message != null;
            if (!isMessageValid)
            {
                STLogger.LogErrorF("SendPBMsg 收到 null 消息，msgid:{0}", msgid);
                return false;
            }

            ByteBuffer buffer = m_SendBuffer;
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
            {
                STLogger.LogError("SendPBMsg 发送缓冲未初始化。");
                return false;
            }

            buffer.Reset();
            int bodyLength = message.CalculateSize();
            bool isHeaderWritten = WriteHeader(buffer, msgid, bodyLength);
            if (!isHeaderWritten)
            {
                return false;
            }

            bool isBodyWritten = TryWriteProtobufBody(msgid, buffer, message, bodyLength);
            if (!isBodyWritten)
            {
                return false;
            }

            bool isSent = SendBuffer(msgid, buffer);
            return isSent;
        }

        /// <summary>
        /// 用复用的写入器序列化消息体。写入器在 Flush 后会把内部缓冲偏移复位，可以跨消息使用，
        /// 避免每次 WriteTo(Stream) 都重新分配 4 KB 内部缓冲。
        /// </summary>
        /// <param name="msgid">消息 ID，仅用于日志。</param>
        /// <param name="buffer">已写入帧头的发送缓冲。</param>
        /// <param name="message">待序列化的消息。</param>
        /// <param name="bodyLength">CalculateSize 得到的负载长度。</param>
        /// <returns>写入长度与声明长度一致时返回 true。</returns>
        bool TryWriteProtobufBody(ulong msgid, ByteBuffer buffer, pb.IMessage message, int bodyLength)
        {
            Google.Protobuf.CodedOutputStream outputStream = EnsurePbOutputStream(buffer);
            bool isOutputStreamValid = outputStream != null;
            if (!isOutputStreamValid)
            {
                STLogger.LogErrorF("Protobuf 写入器未初始化，消息未发送，msgid:{0}", msgid);
                return false;
            }

            try
            {
                message.WriteTo(outputStream);
                outputStream.Flush();
            }
            catch (Exception e)
            {
                // 写入器内部可能残留半条消息，丢弃后下次发送重新创建。
                m_PbOutputStream = null;
                STLogger.LogErrorF("Protobuf 序列化失败，消息未发送，msgid:{0}，异常:{1}", msgid, e);
                return false;
            }

            int frameHeaderLength = NetworkDefine.MSG_LENGTH_SIZE + NetworkDefine.MSG_ID_SIZE;
            int writtenLength = buffer.length - frameHeaderLength;
            bool isLengthMatched = writtenLength == bodyLength;
            if (!isLengthMatched)
            {
                STLogger.LogErrorF(
                    "Protobuf 序列化长度不一致，消息未发送，msgid:{0}，期望 {1} 实际 {2}",
                    msgid, bodyLength, writtenLength);
            }

            return isLengthMatched;
        }

        /// <summary>取得绑定当前发送缓冲的 Protobuf 写入器，首次调用时创建。</summary>
        /// <param name="buffer">发送缓冲。</param>
        /// <returns>可用的写入器，底层流缺失时返回 null。</returns>
        Google.Protobuf.CodedOutputStream EnsurePbOutputStream(ByteBuffer buffer)
        {
            Google.Protobuf.CodedOutputStream outputStream = m_PbOutputStream;
            bool isOutputStreamValid = outputStream != null;
            if (isOutputStreamValid)
            {
                return outputStream;
            }

            System.IO.Stream rawStream = buffer.GetStream();
            bool hasRawStream = rawStream != null;
            if (!hasRawStream)
            {
                return null;
            }

            // leaveOpen 为 true，流的生命周期由 MsgModule 管理，关闭时先释放写入器再关流。
            outputStream = new Google.Protobuf.CodedOutputStream(rawStream, true);
            m_PbOutputStream = outputStream;
            return outputStream;
        }

        /// <summary>从 FlatBufferBuilder 当前数据区直接切片组包，避免额外 ToArray 拷贝。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="builder">已完成 <c>Finish</c> 的构建器。</param>
        /// <returns>数据已交给网络模块时返回 true。</returns>
        bool SendFlatBufferBody(ulong msgid, FlatBuffers.FlatBufferBuilder builder)
        {
            ByteBuffer buffer = m_SendBuffer;
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
            {
                STLogger.LogError("FlatBuffers 发送缓冲未初始化。");
                return false;
            }

            FlatBuffers.ByteBuffer flatBuffer = builder.DataBuffer;
            int bodyPosition = flatBuffer.Position;
            int bodyLength = flatBuffer.Length - bodyPosition;

            buffer.Reset();
            bool isHeaderWritten = WriteHeader(buffer, msgid, bodyLength);
            if (!isHeaderWritten)
            {
                return false;
            }

            // 这里做了优化处理，不从 flatbuffer 里面复制一份数据出来，而是直接取数据，减少一次拷贝。
            buffer.WriteBytes(flatBuffer.RawBuffer, bodyPosition, bodyLength);
            bool isSent = SendBuffer(msgid, buffer);
            return isSent;
        }

        /// <summary>
        /// 写入长度前缀与消息 ID。长度字段统计的是消息 ID 加负载，判断上限时不能只看负载。
        /// </summary>
        /// <param name="buffer">目标缓冲。</param>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bodyLength">负载长度，不含消息 ID。</param>
        /// <returns>长度合法并写入成功时返回 true。</returns>
        bool WriteHeader(ByteBuffer buffer, ulong msgid, int bodyLength)
        {
            bool isBodyLengthNegative = bodyLength < 0;
            if (isBodyLengthNegative)
            {
                STLogger.LogErrorF("消息负载长度为负数 {0}，msgid:{1}", bodyLength, msgid);
                return false;
            }

            int maxBodyLength = NetworkDefine.MAX_MSG_FRAME_LENGTH - NetworkDefine.MSG_ID_SIZE;
            bool isBodyTooLarge = bodyLength > maxBodyLength;
            if (isBodyTooLarge)
            {
                STLogger.LogErrorF("消息负载长度 {0} 超过协议上限 {1}，msgid:{2}", bodyLength, maxBodyLength, msgid);
                return false;
            }

            int totalLength = bodyLength + NetworkDefine.MSG_ID_SIZE;
            buffer.WriteShort((ushort)totalLength);
            buffer.WriteUlong(msgid);
            return true;
        }

        /// <summary>把缓冲交给网络模块发送；缓冲由本模块复用，调用方不要关闭。</summary>
        /// <param name="msgid">消息 ID，仅用于日志。</param>
        /// <param name="buffer">待发送缓冲。</param>
        /// <returns>网络模块已接收整帧时返回 true。</returns>
        bool SendBuffer(ulong msgid, ByteBuffer buffer)
        {
            NetModule netModule = m_NetModule;
            bool isNetModuleValid = netModule != null;
            if (!isNetModuleValid)
            {
                // 模块可能在 DoInit 之后才注册，发送时再解析一次，避免永久失联。
                ResolveNetModule();
                netModule = m_NetModule;
                isNetModuleValid = netModule != null;
            }

            if (!isNetModuleValid)
            {
                STLogger.LogErrorF("MsgModule 未绑定 NetModule，消息未发送，msgid:{0}", msgid);
                return false;
            }

            bool isSent = netModule.SendMessage(buffer);
            if (!isSent)
            {
                STLogger.LogWarningF("消息未发送，网络未连接或调用线程不合法，msgid:{0}", msgid);
            }

            return isSent;
        }
    }
}

