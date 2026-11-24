using System;
using System.Net.Sockets;
using System.Threading;
using ST.Core.Logging;

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

        /// <summary>主线程 ID，发送入口只允许在主线程调用。</summary>
        int m_MainThreadID;

        /// <summary>线程错误已记录标志，避免持续打印同一错误。</summary>
        bool m_IsThreadErrorLogged;

        /// <summary>把一段帧数据追加到发送缓冲，等待本帧统一发送。</summary>
        /// <param name="data">源字节。</param>
        /// <param name="offset">起始下标。</param>
        /// <param name="length">字节数。</param>
        /// <returns>数据已进入发送缓冲时返回 true。</returns>
        public bool Send(byte[] data, int offset, int length)
        {
            bool isBuffered = TryBufferFrame(data, offset, length);
            return isBuffered;
        }

        /// <summary>发送 <see cref="ByteBuffer"/> 中已写入的内容，数据会复制进发送缓冲，调用方缓冲不会被持有。</summary>
        /// <param name="buffer">已写好包体的缓冲实例。</param>
        /// <returns>数据已进入发送缓冲时返回 true，未连接或超过上限时返回 false。</returns>
        public bool SendMessage(ByteBuffer buffer)
        {
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
                return false;

            int length = buffer.length;
            bool hasData = length > 0;
            if (!hasData)
                return false;

            bool isBuffered = TryBufferFrame(buffer.GetBuffer(), 0, length);
            if (!isBuffered)
                return false;

            AddUpMessageStat(length);
            return true;
        }

        /// <summary>追加一帧到发送缓冲，统一处理线程、连接状态与容量上限。</summary>
        /// <param name="data">源字节。</param>
        /// <param name="offset">起始下标。</param>
        /// <param name="length">字节数。</param>
        /// <returns>数据已进入发送缓冲时返回 true。</returns>
        bool TryBufferFrame(byte[] data, int offset, int length)
        {
            bool isMainThread = CheckMainThread();
            if (!isMainThread)
                return false;

            bool isConnected = m_State == SocketState.Connected;
            if (!isConnected)
            {
                AddDroppedMessageStat();
                return false;
            }

            bool hasValidInput = data != null && length > 0;
            if (!hasValidInput)
                return false;

            ByteBuffer buffer = m_PendingSendBuffer;
            bool isBufferValid = buffer != null;
            if (!isBufferValid)
                return false;

            int pendingLength = buffer.length;
            int nextLength = pendingLength + length;
            bool isOverLimit = nextLength > NetworkDefine.SEND_BUFFER_MAX_SIZE;
            if (isOverLimit)
            {
                // 超过上限说明业务发包失控或网络长期拥塞，断开比继续扩容更安全。
                STLogger.LogErrorF(NetworkDefine.LOG_SEND_BUFFER_OVERFLOW, nextLength);
                AddDroppedMessageStat();
                Close(NetworkDefine.REASON_SEND_BUFFER_OVERFLOW);
                return false;
            }

            buffer.WriteBytes(data, offset, length);
            return true;
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

        /// <summary>校验当前线程，子线程发送会记录一次错误并丢弃本次数据。</summary>
        /// <returns>当前处于主线程时返回 true。</returns>
        bool CheckMainThread()
        {
            int currentThreadID = Thread.CurrentThread.ManagedThreadId;
            bool isMainThread = currentThreadID == m_MainThreadID;
            if (isMainThread)
                return true;

            bool isThreadErrorLogged = m_IsThreadErrorLogged;
            if (!isThreadErrorLogged)
            {
                m_IsThreadErrorLogged = true;
                STLogger.LogErrorF(NetworkDefine.LOG_SEND_THREAD_INVALID, currentThreadID);
            }

            return false;
        }
    }
}
