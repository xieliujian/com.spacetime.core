namespace ST.Core.Network
{
    /// <summary>
    /// FlatBuffers 消息处理函数基类，由 <see cref="FlatBufferProcFun{T}"/> 实现具体表类型解析。
    /// </summary>
    /// <remarks>
    /// 保留独立类型是为了区分 FlatBuffers 处理器，注册时与 Protobuf 处理器写入同一张表。
    /// </remarks>
    public abstract class FlatBufferProcFunBase : MsgProcFunBase
    {
        /// <summary>将消息体解析为具体表类型并回调业务。</summary>
        /// <param name="data">消息体字节。</param>
        public abstract override void Invoke(byte[] data);
    }
}
