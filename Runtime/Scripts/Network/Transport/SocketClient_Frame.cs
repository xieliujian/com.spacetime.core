using System;

namespace ST.Core.Network
{
    /// <summary>
    /// TCP 客户端的拆包部分：维护接收缓冲，按“2 字节长度 + 8 字节消息 ID + 负载”切出完整消息。
    /// 连接见 SocketClient.cs，发送见 SocketClient_Send.cs。
    /// </summary>
    public partial class SocketClient
    {
        /// <summary>单次异步读取的接收缓冲，复用避免每次分配。</summary>
        byte[] m_ReadBuffer = new byte[NetworkDefine.MAX_READ_NUM];

        /// <summary>粘包拆包缓冲，按需扩容并在解析后前移残留数据。</summary>
        byte[] m_RecvBuffer = new byte[NetworkDefine.INITIAL_RECV_BUFFER_SIZE];

        /// <summary>拆包缓冲中尚未解析的字节数。</summary>
        int m_RecvLength;

        /// <summary>返回异步读取使用的缓冲。</summary>
        /// <returns>接收缓冲实例。</returns>
        byte[] GetReadBuffer()
        {
            return m_ReadBuffer;
        }

        /// <summary>返回单次异步读取的最大字节数。</summary>
        /// <returns>接收缓冲长度。</returns>
        int GetReadBufferLength()
        {
            return m_ReadBuffer.Length;
        }

        /// <summary>清空未解析数据，重连或关闭时调用。</summary>
        void ResetRecvBuffer()
        {
            m_RecvLength = 0;
        }

        /// <summary>把本次读取到的字节追加到拆包缓冲，必要时扩容。</summary>
        /// <param name="length">本次读取的字节数。</param>
        /// <returns>追加成功返回 true，超过缓冲上限时返回 false 并已断开连接。</returns>
        bool AppendRecvData(int length)
        {
            int needSize = m_RecvLength + length;
            bool isOverflow = needSize > NetworkDefine.MAX_RECV_BUFFER_SIZE;
            if (isOverflow)
            {
                CloseWithReason(NetworkDefine.REASON_RECV_BUFFER_OVERFLOW, needSize);
                return false;
            }

            bool isNeedGrow = needSize > m_RecvBuffer.Length;
            if (isNeedGrow)
            {
                GrowRecvBuffer(needSize);
            }

            Buffer.BlockCopy(m_ReadBuffer, 0, m_RecvBuffer, m_RecvLength, length);
            m_RecvLength = needSize;
            return true;
        }

        /// <summary>按 2 倍扩容拆包缓冲，不改变已有数据。</summary>
        /// <param name="needSize">至少需要的容量。</param>
        void GrowRecvBuffer(int needSize)
        {
            int newSize = m_RecvBuffer.Length;
            while (newSize < needSize)
            {
                newSize *= 2;
            }

            bool isOverMax = newSize > NetworkDefine.MAX_RECV_BUFFER_SIZE;
            if (isOverMax)
            {
                newSize = NetworkDefine.MAX_RECV_BUFFER_SIZE;
            }

            byte[] newBuffer = new byte[newSize];
            Buffer.BlockCopy(m_RecvBuffer, 0, newBuffer, 0, m_RecvLength);
            m_RecvBuffer = newBuffer;
        }

        /// <summary>
        /// 按“2 字节长度 + 8 字节消息 ID + 负载”循环拆包。
        /// 分帧格式与协议保持一致，改动会影响服务端兼容性。
        /// </summary>
        /// <param name="sessionID">解析时所属的会话序号，用于丢弃跨会话残留。</param>
        void ParseFrames(int sessionID)
        {
            int offset = 0;
            while (true)
            {
                int remaining = m_RecvLength - offset;
                bool hasLengthField = remaining >= NetworkDefine.MSG_LENGTH_SIZE;
                if (!hasLengthField)
                {
                    break;
                }

                int msgLength = m_RecvBuffer[offset] | (m_RecvBuffer[offset + 1] << 8);
                bool isLengthValid = msgLength >= NetworkDefine.MSG_ID_SIZE;
                if (!isLengthValid)
                {
                    CloseWithReason(NetworkDefine.REASON_INVALID_MESSAGE_LENGTH, msgLength);
                    return;
                }

                int frameLength = NetworkDefine.MSG_LENGTH_SIZE + msgLength;
                bool hasFullFrame = remaining >= frameLength;
                if (!hasFullFrame)
                {
                    break;
                }

                int msgIdOffset = offset + NetworkDefine.MSG_LENGTH_SIZE;
                ulong msgId = ReadUInt64(m_RecvBuffer, msgIdOffset);
                int payloadLength = msgLength - NetworkDefine.MSG_ID_SIZE;
                int payloadOffset = msgIdOffset + NetworkDefine.MSG_ID_SIZE;

                // 分发在主线程执行，拆包缓冲随后会被新数据覆盖，这里必须拷贝一份独立负载。
                byte[] payload = new byte[payloadLength];
                bool hasPayload = payloadLength > 0;
                if (hasPayload)
                {
                    Buffer.BlockCopy(m_RecvBuffer, payloadOffset, payload, 0, payloadLength);
                }

                offset += frameLength;
                m_NetModule.AddEvent(msgId, payload, sessionID);
                AddDownMessageStat();
            }

            RemoveParsedBytes(offset);
        }

        /// <summary>把未解析的残留字节前移到缓冲头部。</summary>
        /// <param name="count">已解析的字节数。</param>
        void RemoveParsedBytes(int count)
        {
            bool hasParsedBytes = count > 0;
            if (!hasParsedBytes)
                return;

            int remaining = m_RecvLength - count;
            bool hasRemaining = remaining > 0;
            if (hasRemaining)
            {
                Buffer.BlockCopy(m_RecvBuffer, count, m_RecvBuffer, 0, remaining);
            }

            m_RecvLength = remaining;
        }

        /// <summary>按小端读取 64 位消息 ID。</summary>
        /// <param name="buffer">源字节。</param>
        /// <param name="offset">起始下标。</param>
        /// <returns>消息 ID。</returns>
        static ulong ReadUInt64(byte[] buffer, int offset)
        {
            ulong value = 0;
            for (int i = 0; i < NetworkDefine.MSG_ID_SIZE; i++)
            {
                value |= (ulong)buffer[offset + i] << (i * 8);
            }

            return value;
        }
    }
}
