using System.Threading;

namespace ST.Core.Network
{
    /// <summary>
    /// TCP 客户端的统计部分：累计收发字节数与消息条数，供诊断与网络监控读取。
    /// 收包统计在读取回调线程累加，读取侧统一走 Interlocked，主线程读取不会读到撕裂值。
    /// </summary>
    public partial class SocketClient
    {
        /// <summary>累计发送字节数，含帧头。</summary>
        long m_UpBytes;

        /// <summary>累计接收字节数，含帧头。</summary>
        long m_DownBytes;

        /// <summary>累计发送消息条数。</summary>
        long m_UpMessages;

        /// <summary>累计接收消息条数。</summary>
        long m_DownMessages;

        /// <summary>读取累计收发统计，计数不会被清零。</summary>
        /// <param name="upBytes">累计发送字节数。</param>
        /// <param name="downBytes">累计接收字节数。</param>
        /// <param name="upMessages">累计发送消息条数。</param>
        /// <param name="downMessages">累计接收消息条数。</param>
        public void GetNetStat(out long upBytes, out long downBytes, out long upMessages, out long downMessages)
        {
            upBytes = Interlocked.Read(ref m_UpBytes);
            downBytes = Interlocked.Read(ref m_DownBytes);
            upMessages = Interlocked.Read(ref m_UpMessages);
            downMessages = Interlocked.Read(ref m_DownMessages);
        }

        /// <summary>清零累计收发统计。</summary>
        public void ResetNetStat()
        {
            Interlocked.Exchange(ref m_UpBytes, 0L);
            Interlocked.Exchange(ref m_DownBytes, 0L);
            Interlocked.Exchange(ref m_UpMessages, 0L);
            Interlocked.Exchange(ref m_DownMessages, 0L);
        }

        /// <summary>统计一条已进入发送缓冲的消息。</summary>
        /// <param name="length">帧字节数，含帧头。</param>
        void AddUpMessageStat(int length)
        {
            Interlocked.Add(ref m_UpBytes, length);
            Interlocked.Increment(ref m_UpMessages);
        }

        /// <summary>统计本次读取到的字节数。</summary>
        /// <param name="length">读取字节数。</param>
        void AddDownBytesStat(int length)
        {
            Interlocked.Add(ref m_DownBytes, length);
        }

        /// <summary>统计一条已拆出的完整消息。</summary>
        void AddDownMessageStat()
        {
            Interlocked.Increment(ref m_DownMessages);
        }
    }
}