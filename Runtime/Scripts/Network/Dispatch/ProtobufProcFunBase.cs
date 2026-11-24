namespace ST.Core.Network
{
    /// <summary>
    /// Protobuf 消息处理函数基类，由 <see cref="ProtobufProcFun{T}"/> 实现具体消息类型解析。
    /// </summary>
    /// <remarks>
    /// 保留独立类型是为了区分 Protobuf 处理器，注册时与 FlatBuffers 处理器写入同一张表。
    /// </remarks>
    public abstract class ProtobufProcFunBase : MsgProcFunBase
    {
        /// <summary>将消息体解析为具体消息类型并回调业务。</summary>
        /// <param name="data">消息体字节。</param>
        public abstract override void Invoke(byte[] data);
    }
}
