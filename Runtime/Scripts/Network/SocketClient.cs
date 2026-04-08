using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using ST.Core.Logging;
using Logger = ST.Core.Logging.Logger;

namespace ST.Core.Network
{
    /// <summary>
    /// 基于 <see cref="TcpClient"/> 的异步 TCP 客户端：负责连接、粘包拆包及将完整消息投递到 <see cref="NetManager.AddEvent"/>。
    /// </summary>
    public class SocketClient
    {
        /// <summary>
        /// 连接断开原因分类，用于日志与断线处理分支。
        /// </summary>
        public enum DisType
        {
            /// <summary>读循环异常导致的断开。</summary>
            Exception,
            /// <summary>对端关闭或读取长度为 0。</summary>
            Disconnect,
        }

        /// <summary>底层 TCP 连接句柄。</summary>
        TcpClient m_Client = null;

        /// <summary>TCP 连接上的网络数据流，用于异步读写字节。</summary>
        NetworkStream m_NetStream = null;

        /// <summary>粘包拆包用的内存缓冲，持续追加接收到的碎片数据。</summary>
        MemoryStream m_MemStream = null;

        /// <summary>从 <see cref="m_MemStream"/> 读取完整消息帧的二进制读取器。</summary>
        BinaryReader m_Reader = null;

        /// <summary>单次异步读取的原始字节接收缓冲（大小由 <see cref="NetworkDefine.s_MaxReadNum"/> 决定）。</summary>
        byte[] m_ByteBuffer = new byte[NetworkDefine.s_MaxReadNum];

        /// <summary>当前连接的服务器 IP 地址。</summary>
        string m_ip;

        /// <summary>当前连接的服务器端口。</summary>
        int m_port;

        /// <summary>创建未连接的客户端实例。</summary>
        public SocketClient()
        {
        }

        /// <summary>初始化内存流与 <see cref="BinaryReader"/>，在 <see cref="NetManager.DoInit"/> 中调用。</summary>
        public void OnRegister()
        {
            m_MemStream = new MemoryStream();
            m_Reader = new BinaryReader(m_MemStream);
        }

        /// <summary>关闭 TCP 连接并释放内存流与读取器，在 <see cref="NetManager.DoClose"/> 中调用。</summary>
        public void OnRemove()
        {
            Close();

            if (m_Reader != null)
                m_Reader.Close();

            if (m_MemStream != null)
                m_MemStream.Close();

        }

        /// <summary>创建 <see cref="TcpClient"/> 并异步 <c>BeginConnect</c>，连接结果回调 <see cref="OnConnect"/>。</summary>
        void ConnectServer(string host, int port)
        {
            m_ip = host;
            m_port = port;

            m_Client = null;
            m_Client = new TcpClient();
            m_Client.SendTimeout = 1000;
            m_Client.ReceiveTimeout = 1000;
            m_Client.NoDelay = true;

            try
            {
                m_Client.BeginConnect(host, port, new AsyncCallback(OnConnect), null);
            }
            catch (Exception e)
            {
                Close();
                Logger.LogError(e.Message);
            }
        }

        /// <summary>TCP 连接成功回调：触发连接事件、获取网络流并启动首次异步读取。</summary>
        void OnConnect(IAsyncResult asr)
        {
            if (MainThreadTask.S != null)
            {
                MainThreadTask.S.AddTask(NetManager.S.onConnectEvent);
            }

            m_NetStream = m_Client.GetStream();
            m_NetStream.BeginRead(m_ByteBuffer, 0, NetworkDefine.s_MaxReadNum, new AsyncCallback(OnRead), null);

            Logger.LogDebug("======连接=" + m_ip + "=" + m_port + "=======");
        }

        /// <summary>向网络流异步写入消息字节，连接断开时记录错误。</summary>
        void WriteMessage(byte[] message)
        {
            if (IsConnected())
            {
                m_NetStream.BeginWrite(message, 0, message.Length, new AsyncCallback(OnWrite), null);
            }
            else
            {
                Logger.LogError("client.connected----->>false");
            }
        }

        /// <summary>异步读取回调：将收到的字节交给 <see cref="OnReceive"/> 拆包，然后继续投递下一次读取。</summary>
        void OnRead(IAsyncResult asr)
        {
            int bytesRead = 0;
            try
            {
                if (!IsConnected())
                    return;

                lock (m_Client.GetStream())
                {
                    //读取字节流到缓冲区
                    bytesRead = m_Client.GetStream().EndRead(asr);
                }

                if (bytesRead < 1)
                {
                    //包尺寸有问题，断线处理
                    OnDisconnected(DisType.Disconnect, "bytesRead < 1");
                    return;
                }
                
                //分析数据包内容，抛给逻辑层
                OnReceive(m_ByteBuffer, bytesRead);

                var stream = m_Client.GetStream();
                lock (stream)
                {
                    //分析完，再次监听服务器发过来的新消息
                    Array.Clear(m_ByteBuffer, 0, m_ByteBuffer.Length);   //清空数组
                    stream.BeginRead(m_ByteBuffer, 0, NetworkDefine.s_MaxReadNum, new AsyncCallback(OnRead), null);
                }
            }
            catch (Exception ex)
            {
                //PrintBytes();
                OnDisconnected(DisType.Exception, ex.Message);
            }
        }

        /// <summary>断线处理：记录日志并关闭客户端。</summary>
        void OnDisconnected(DisType dis, string msg)
        {
            Logger.LogDebug("OnDisconnected" + msg);
            Logger.LogDebug("======断开连接========");
            Close();   //关掉客户端链接
        }

        /// <summary>将接收缓冲中的全部字节以十六进制格式打印到日志（调试用）。</summary>
        void PrintBytes()
        {
            string returnStr = string.Empty;
            for (int i = 0; i < m_ByteBuffer.Length; i++)
            {
                returnStr += m_ByteBuffer[i].ToString("X2");
            }

            Logger.LogError(returnStr);
        }

        /// <summary>异步写完成回调：调用 <c>EndWrite</c> 完成写入并捕获异常。</summary>
        void OnWrite(IAsyncResult r)
        {
            try
            {
                m_NetStream.EndWrite(r);
            }
            catch (Exception ex)
            {
                Logger.LogError("OnWrite--->>>" + ex.Message);
            }
        }

        /// <summary>将新收到的字节追加到 <see cref="m_MemStream"/> 并循环解析完整消息帧（2 字节长度 + 8 字节 msgid + 负载）。</summary>
        void OnReceive(byte[] bytes, int length)
        {
            m_MemStream.Seek(0, SeekOrigin.End);
            m_MemStream.Write(bytes, 0, length);

            //Reset to beginning
            m_MemStream.Seek(0, SeekOrigin.Begin);

            while (RemainingBytes() > 2)
            {
                ushort msglen = m_Reader.ReadUInt16();
                if (RemainingBytes() >= msglen)
                {
                    ulong msgid = m_Reader.ReadUInt64();
                    int protocollen = msglen - sizeof(ulong);
                    byte[] bytearray = m_Reader.ReadBytes(protocollen);
                    OnReceivedMessage(msgid, bytearray);
                }
                else
                {
                    m_MemStream.Position = m_MemStream.Position - 2;
                    break;
                }
            }

            byte[] leftover = m_Reader.ReadBytes((int)RemainingBytes());
            m_MemStream.SetLength(0);
            m_MemStream.Write(leftover, 0, leftover.Length);
        }

        /// <summary>返回 <see cref="m_MemStream"/> 当前位置到末尾的未读字节数。</summary>
        private long RemainingBytes()
        {
            return m_MemStream.Length - m_MemStream.Position;
        }

        /// <summary>将解包后的完整消息投递到 <see cref="NetManager.AddEvent"/> 主线程队列。</summary>
        void OnReceivedMessage(ulong msgid, byte[] bytearray)
        {
            NetManager.S.AddEvent(msgid, bytearray);
        }

        /// <summary>将已序列化的字节帧提交给 <see cref="WriteMessage"/> 异步发送。</summary>
        void SessionSend(byte[] bytes)
        {
            WriteMessage(bytes);
        }

        /// <summary>关闭并置空 <see cref="TcpClient"/>；已断开时无操作。</summary>
        public void Close()
        {
            if (m_Client != null)
            {
                if (m_Client.Connected)
                    m_Client.Close();

                m_Client = null;

                Logger.LogDebug("======关闭连接========");
            }
        }

        /// <summary>发起连接请求（代理至 <see cref="ConnectServer"/>）。</summary>
        /// <param name="address">服务器 IP 或域名。</param>
        /// <param name="port">端口号。</param>
        public void SendConnect(string address, int port)
        {
            ConnectServer(address, port);
        }

        /// <summary>将 <see cref="ByteBuffer"/> 内容序列化为字节数组后异步发送，发送后关闭缓冲。</summary>
        /// <param name="buffer">已写好包体的缓冲实例。</param>
        public void SendMessage(ByteBuffer buffer)
        {
            if (!IsConnected())
                return;

            SessionSend(buffer.ToBytes());
            buffer.Close();
        }

        /// <summary>是否处于已连接状态。</summary>
        public bool IsConnected()
        {
            return (m_Client != null && m_Client.Connected);
        }
    }

}
