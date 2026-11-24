using System;
using System.Net.Sockets;

namespace ST.Core.Network
{
    /// <summary>
    /// TCP 客户端的发送部分：所有帧先写入复用发送缓冲，由主线程每帧批量阻塞发送。
    /// 同一时刻只有一个写操作，避免上一笔异步写未完成时再次 BeginWrite 抛出 NotSupportedException。
    /// </summary>
    public partial class SocketClient
    {
        /// <summary>待发送数据，跨帧复用，发送成功后清空。</summary>
        ByteBuffer m_PendingSendBuffer = new ByteBuffer(NetworkDefine.SEND_BUFFER_SIZE);

        /// <summary>把一段帧数据追加到发送缓冲，等待本帧统一发送。</summary>
        /// <param name="data">源字节。</param>
        /// <param name="offset">起始下标。</param>
        /// <param name="length">字节数。</param>
        public void Send(byte[] data, int offset, int length)
        {
            bool canBuffer = m_State == SocketState.Connected && data != null && length > 0;
            if (!canBuffer)
                return;

            ByteBuffer buffer = m_PendingSendBuffer;
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
                return;

            buffer.WriteBytes(data, offset, length);
        }

        /// <summary>发送 <see cref="ByteBuffer"/> 中已写入的内容，数据会复制进发送缓冲，调用方缓冲不会被持有。</summary>
        /// <param name="buffer">已写好包体的缓冲实例。</param>
        public void SendMessage(ByteBuffer buffer)
        {
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
                return;

            int length = buffer.length;
            bool hasData = length > 0;
            if (!hasData)
                return;

            bool canSend = m_State == SocketState.Connected;
            if (!canSend)
                return;

            Send(buffer.GetBuffer(), 0, length);
            AddUpMessageStat(length);
        }

        /// <summary>
        /// 把缓冲中的待发数据一次性阻塞写出，由 <see cref="NetModule.DoUpdate"/> 每帧调用。
        /// 只能在主线程调用，阻塞上限由 <see cref="NetworkDefine.SEND_TIMEOUT_MS"/> 控制。
        /// </summary>
        public void FlushSend()
        {
            ByteBuffer buffer = m_PendingSendBuffer;
            bool hasPendingData = buffer != null && buffer.length > 0;
            if (!hasPendingData)
                return;

            NetworkStream stream = m_NetStream;
            bool canWrite = m_State == SocketState.Connected && stream != null;
            if (!canWrite)
            {
                ResetSendBuffer();
                return;
            }

            int length = buffer.length;
            try
            {
                stream.Write(buffer.GetBuffer(), 0, length);
                buffer.Reset();
            }
            catch (Exception e)
            {
                ResetSendBuffer();
                CloseWithReason(NetworkDefine.REASON_WRITE_FAILED, e.Message);
            }
        }

        /// <summary>丢弃未发送数据，重连或关闭时调用。</summary>
        void ResetSendBuffer()
        {
            ByteBuffer buffer = m_PendingSendBuffer;
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
                return;

            buffer.Reset();
        }
    }
}
