using System;
using System.Net.Sockets;

namespace ST.Core.Network
{
    /// <summary>
    /// TCP 客户端的读取部分：异步读取回调与继续监听下一次读取。
    /// 连接生命周期见 SocketClient.cs，拆包细节见 SocketClient_Frame.cs。
    /// </summary>
    public partial class SocketClient
    {
        /// <summary>读取回调，把收到的字节送入拆包缓冲后继续监听下一次读取。</summary>
        /// <param name="asr">异步读取结果。</param>
        void OnRead(IAsyncResult asr)
        {
            NetworkStream stream = m_NetStream;
            bool isCurrentStream = stream != null && ReferenceEquals(asr.AsyncState, stream);
            if (!isCurrentStream)
                return;

            int bytesRead = 0;
            try
            {
                bytesRead = stream.EndRead(asr);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_READ_FAILED, e.Message);
                return;
            }

            bool isConnectionAlive = bytesRead > 0;
            if (!isConnectionAlive)
            {
                CloseWithReason(NetworkDefine.REASON_SERVER_CLOSED);
                return;
            }

            AddDownBytesStat(bytesRead);

            int sessionID = m_SessionID;
            bool isAppended = AppendRecvData(bytesRead);
            if (isAppended)
            {
                ParseFrames(sessionID);
            }

            bool isStillConnected = m_State == SocketState.Connected;
            if (isStillConnected)
            {
                BeginRead(stream);
            }
        }

        /// <summary>向当前数据流投递一次异步读取。</summary>
        /// <param name="stream">当前数据流。</param>
        void BeginRead(NetworkStream stream)
        {
            bool isStreamValid = stream != null;
            if (!isStreamValid)
                return;

            try
            {
                stream.BeginRead(GetReadBuffer(), 0, GetReadBufferLength(), m_ReadCallback, stream);
            }
            catch (Exception e)
            {
                CloseWithReason(NetworkDefine.REASON_BEGIN_READ_FAILED, e.Message);
            }
        }
    }
}