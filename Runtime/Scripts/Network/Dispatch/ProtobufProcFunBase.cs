namespace ST.Core.Network
{
    /// <summary>
    /// Protobuf 消息处理函数基类，由 <see cref="ProtobufProcFun{T}"/> 实现具体消息类型解析。
    /// </summary>
    public abstract class ProtobufProcFunBase
    {
        /// <summary>将消息体解析为具体消息类型并回调业务。</summary>
        /// <param name="data">消息体字节。</param>
        public abstract void Invoke(byte[] data);
    }
}
