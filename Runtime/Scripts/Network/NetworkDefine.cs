using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST.Core.Network
{
    /// <summary>
    /// 网络消息序列化后端选择，供 <see cref="MsgDispatcher"/> 使用。
    /// </summary>
    public enum IMsgType
    {
        /// <summary>使用 FlatBuffers 编解码。</summary>
        FlatBuffer,
        /// <summary>使用 Google Protobuf 编解码。</summary>
        Protobuf,
    }

    /// <summary>
    /// FlatBuffers 消息处理函数非泛型基类，由 <see cref="FlatBufferProcFun{T}"/> 实现具体类型解析。
    /// </summary>
    public class IFlatBufferProcFun
    {
        /// <summary>由派生类将 <paramref name="buf"/> 解析为具体表类型并回调业务。</summary>
        /// <param name="buf">消息体字节</param>
        public virtual void Invoke(byte[] buf)
        {

        }
    }

    /// <summary>
    /// Protobuf 消息处理函数非泛型基类，由 <see cref="ProtobufProcFun{T}"/> 实现解析。
    /// </summary>
    public class IProtobufProcFun
    {
        /// <summary>由派生类将 <paramref name="buf"/> 解析为具体消息并回调业务。</summary>
        /// <param name="buf">消息体字节</param>
        public virtual void Invoke(byte[] buf)
        {

        }
    }

    /// <summary>
    /// 已反序列化后的消息处理委托。
    /// </summary>
    /// <typeparam name="T">消息或表类型</typeparam>
    /// <param name="msg">解析结果</param>
    public delegate void MsgProcDelegate<T>(T msg);

    /// <summary>
    /// 网络层通用常量（套接字读缓冲等）。
    /// </summary>
    public class NetworkDefine
    {
        /// <summary>TCP 单次异步读取使用的最大字节数。</summary>
        public static readonly int s_MaxReadNum = 8192;
    }
}
