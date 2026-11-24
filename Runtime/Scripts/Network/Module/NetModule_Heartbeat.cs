using UnityEngine;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 网络模块的心跳部分：按固定间隔调用业务心跳发送委托，并在一段时间收不到任何消息时断开连接。
    /// 对应旧工程 GameNetwork 的 UpdateHeartbeat 与 UpdateNoResponse。
    /// </summary>
    public partial class NetModule
    {
        /// <summary>设置业务心跳，使用 <see cref="NetworkDefine"/> 中的默认间隔与超时。</summary>
        /// <param name="heartbeatSender">发出一次心跳消息的委托，由业务通过 MsgModule 发送。</param>
        public void SetHeartbeat(Run heartbeatSender)
        {
            SetHeartbeat(heartbeatSender, NetworkDefine.HEARTBEAT_INTERVAL_MS, NetworkDefine.HEARTBEAT_TIMEOUT_MS);
        }

        /// <summary>设置业务心跳间隔与超时。</summary>
        /// <param name="heartbeatSender">发出一次心跳消息的委托。</param>
        /// <param name="intervalMilliseconds">心跳发送间隔，单位毫秒，必须大于 0。</param>
        /// <param name="timeoutMilliseconds">未收到任何消息的超时时间，单位毫秒，必须大于 0。</param>
        public void SetHeartbeat(Run heartbeatSender, int intervalMilliseconds, int timeoutMilliseconds)
        {
            bool isSenderValid = heartbeatSender != null;
            bool isIntervalValid = intervalMilliseconds > 0;
            bool isTimeoutValid = timeoutMilliseconds > 0;
            bool canSetHeartbeat = isSenderValid && isIntervalValid && isTimeoutValid;
            if (!canSetHeartbeat)
            {
                STLogger.LogError("SetHeartbeat 参数非法，心跳设置被忽略。");
                return;
            }

            m_HeartbeatSender = heartbeatSender;
            m_HeartbeatIntervalSeconds = intervalMilliseconds / 1000f;
            m_HeartbeatTimeoutSeconds = timeoutMilliseconds / 1000f;
            ResetHeartbeatTime();
        }

        /// <summary>关闭心跳检测并清理计时。</summary>
        public void ClearHeartbeat()
        {
            m_HeartbeatSender = null;
            ResetHeartbeatTime();
        }

        /// <summary>按间隔发送心跳，并在长时间收不到消息时断开连接。</summary>
        void UpdateHeartbeat()
        {
            Run heartbeatSender = m_HeartbeatSender;
            bool hasHeartbeat = heartbeatSender != null;
            if (!hasHeartbeat)
                return;

            SocketClient socketClient = m_SocketClient;
            bool isConnected = socketClient != null && socketClient.IsConnected();
            if (!isConnected)
            {
                ResetHeartbeatTime();
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            m_HeartbeatSendElapsed += deltaTime;
            m_ReceiveIdleElapsed += deltaTime;

            bool isTimeout = m_ReceiveIdleElapsed >= m_HeartbeatTimeoutSeconds;
            if (isTimeout)
            {
                ResetHeartbeatTime();
                socketClient.Close(NetworkDefine.REASON_HEARTBEAT_TIMEOUT);
                return;
            }

            bool isSendTime = m_HeartbeatSendElapsed >= m_HeartbeatIntervalSeconds;
            if (!isSendTime)
                return;

            m_HeartbeatSendElapsed = 0f;
            ERunner.Run(heartbeatSender);
        }

        /// <summary>清零心跳发送与收包空闲计时。</summary>
        void ResetHeartbeatTime()
        {
            m_HeartbeatSendElapsed = 0f;
            m_ReceiveIdleElapsed = 0f;
        }
    }
}