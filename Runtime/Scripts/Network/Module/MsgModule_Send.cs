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
        /// <summary>协议中消息长度字段为 ushort，包体长度不能超过该值。</summary>
        const int MAX_MSG_BODY_LENGTH = ushort.MaxValue;

        /// <summary>FlatBuffers 构建器，跨消息复用。</summary>
        FlatBuffers.FlatBufferBuilder m_FlatBufferBuilder =
            new FlatBuffers.FlatBufferBuilder(CommonDefine.s_ListConst_1024);

        /// <summary>发送缓冲，跨消息复用，写出前先 <see cref="ByteBuffer.Reset"/>。</summary>
        ByteBuffer m_SendBuffer = new ByteBuffer();

        /// <summary>清空并返回 FlatBuffers 构建器。读取属性不再隐式重置状态。</summary>
        /// <returns>可继续写入的构建器。</returns>
        public FlatBuffers.FlatBufferBuilder BeginFlatBuffer()
        {
            m_FlatBufferBuilder.Clear();
            return m_FlatBufferBuilder;
        }

        /// <summary>将 Protobuf 消息写入复用缓冲后加长度前缀与消息 ID 发送。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="message">已填充的消息实例。</param>
        public void SendPBMsg(ulong msgid, pb.IMessage message)
        {
            bool isMessageValid = message != null;
            if (!isMessageValid)
            {
                STLogger.LogErrorF("SendPBMsg 收到 null 消息，msgid:{0}", msgid);
                return;
            }

            ByteBuffer buffer = m_SendBuffer;
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
            {
                STLogger.LogError("SendPBMsg 发送缓冲未初始化。");
                return;
            }

            buffer.Reset();
            int bodyLength = message.CalculateSize();
            bool isHeaderWritten = WriteHeader(buffer, msgid, bodyLength);
            if (!isHeaderWritten)
                return;

            message.WriteTo(buffer.GetStream());
            SendBuffer(buffer);
        }

        /// <summary>从 FlatBufferBuilder 当前数据区直接切片组包，避免额外 ToArray 拷贝。</summary>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="builder">已完成 <c>Finish</c> 的构建器。</param>
        public void SendFBMsg(ulong msgid, FlatBuffers.FlatBufferBuilder builder)
        {
            bool isBuilderValid = builder != null;
            if (!isBuilderValid)
            {
                STLogger.LogErrorF("SendFBMsg 收到 null 构建器，msgid:{0}", msgid);
                return;
            }

            ByteBuffer buffer = m_SendBuffer;
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
            {
                STLogger.LogError("SendFBMsg 发送缓冲未初始化。");
                return;
            }

            int bodyPosition = builder.DataBuffer.Position;
            int bodyLength = builder.DataBuffer.Length - bodyPosition;

            buffer.Reset();
            bool isHeaderWritten = WriteHeader(buffer, msgid, bodyLength);
            if (!isHeaderWritten)
                return;

            // 这里做了优化处理，不从 flatbuffer 里面复制一份数据出来，而是直接取数据，减少一次拷贝。
            buffer.WriteBytes(builder.DataBuffer.RawBuffer, bodyPosition, bodyLength);
            SendBuffer(buffer);
        }

        /// <summary>写入长度前缀与消息 ID。</summary>
        /// <param name="buffer">目标缓冲。</param>
        /// <param name="msgid">消息 ID。</param>
        /// <param name="bodyLength">消息体长度，不含消息 ID。</param>
        /// <returns>长度合法并写入成功时返回 true。</returns>
        bool WriteHeader(ByteBuffer buffer, ulong msgid, int bodyLength)
        {
            int totalLength = bodyLength + NetworkDefine.MSG_ID_SIZE;
            bool isTooLarge = totalLength > MAX_MSG_BODY_LENGTH;
            if (isTooLarge)
            {
                STLogger.LogErrorF("消息体长度 {0} 超过协议上限，msgid:{1}", totalLength, msgid);
                return false;
            }

            buffer.WriteShort((ushort)totalLength);
            buffer.WriteUlong(msgid);
            return true;
        }

        /// <summary>把缓冲交给网络模块发送；缓冲由本模块复用，调用方不要关闭。</summary>
        /// <param name="buffer">待发送缓冲。</param>
        void SendBuffer(ByteBuffer buffer)
        {
            NetModule netModule = m_NetModule;
            bool isNetModuleValid = netModule != null;
            if (!isNetModuleValid)
            {
                STLogger.LogError("MsgModule 未绑定 NetModule，消息未发送。");
                return;
            }

            netModule.SendMessage(buffer);
        }
    }
}
