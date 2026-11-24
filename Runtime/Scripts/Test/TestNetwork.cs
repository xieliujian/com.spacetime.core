using pbs;
using ST.Core.Network;
using UnityEngine;

namespace ST.Core.Test
{
    /// <summary>
    /// 网络模块示例：手动装配 <see cref="NetModule"/> 与 <see cref="MsgModule"/>，按钮触发连接与 Protobuf 发送。
    /// 仅用于本地联调演示，正式业务应由 <see cref="ST.Core.Launch.GameWorld"/> 注册这两个模块。
    /// </summary>
    public class TestNetwork : MonoBehaviour
    {
        /// <summary>示例用网络模块。</summary>
        NetModule m_NetManager = new NetModule();

        /// <summary>示例用消息分发器。</summary>
        MsgModule m_MsgDispatcher = new MsgModule();

        /// <summary>连接目标 IP，可在 Inspector 中修改。</summary>
        public string ipAddress = "127.0.0.1";

        /// <summary>连接目标端口，可在 Inspector 中修改。</summary>
        public int port = 3580;

        /// <summary>手动装配两个模块并订阅断线事件。</summary>
        void Start()
        {
            m_MsgDispatcher.SetNetModule(m_NetManager);
            m_NetManager.SetMsgModule(m_MsgDispatcher);
            m_NetManager.onDisconnectEvent.AddListener(OnDisconnected);
        }

        /// <summary>驱动网络模块的拆包与消息分发。</summary>
        void Update()
        {
            m_NetManager.DoUpdate();
        }

        /// <summary>释放连接与监听。</summary>
        void OnDestroy()
        {
            m_NetManager.DoClose();
            m_MsgDispatcher.DoClose();
        }

        /// <summary>绘制测试按钮。</summary>
        void OnGUI()
        {
            if (GUI.Button(new Rect(0, 0, 300, 60), "SendConnect"))
            {
                m_NetManager.SendConnect(ipAddress, port);
            }

            if (GUI.Button(new Rect(0, 60, 300, 60), "CloseSocket"))
            {
                m_NetManager.CloseSocket();
            }

            if (GUI.Button(new Rect(0, 120, 300, 60), "SendRegisterMsg"))
            {
                SendRegisterAccount();
            }

            bool isConnected = m_NetManager.IsConnected();
            GUI.Label(new Rect(320, 0, 400, 60), "Connected: " + isConnected);

            long upBytes;
            long downBytes;
            long upMessages;
            long downMessages;
            long droppedMessages;
            m_NetManager.GetNetStat(
                out upBytes, out downBytes, out upMessages, out downMessages, out droppedMessages);
            string statText = string.Format(
                "Up: {0} B / {1} msgs, Down: {2} B / {3} msgs, Dropped: {4}",
                upBytes, upMessages, downBytes, downMessages, droppedMessages);
            GUI.Label(new Rect(320, 40, 600, 60), statText);
        }

        /// <summary>构造并发送 <see cref="ReqRegisterAccount"/> 测试包。</summary>
        void SendRegisterAccount()
        {
            ReqRegisterAccount msg = new ReqRegisterAccount();
            msg.Account = "test_account";
            msg.Password = "test_password";
            m_MsgDispatcher.SendPBMsg(ReqRegisterAccount.HashID, msg);
        }

        /// <summary>断线事件回调，正式业务在这里提示玩家或发起重连。</summary>
        /// <param name="reason">断线原因。</param>
        void OnDisconnected(string reason)
        {
            Debug.LogWarning("network disconnected: " + reason);
        }
    }
}
