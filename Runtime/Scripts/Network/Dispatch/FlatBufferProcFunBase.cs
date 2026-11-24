namespace ST.Core.Network
{
    /// <summary>
    /// FlatBuffers 消息处理函数基类，由 <see cref="FlatBufferProcFun{T}"/> 实现具体表类型解析。
    /// </summary>
    public abstract class FlatBufferProcFunBase
    {
        /// <summary>将消息体解析为具体表类型并回调业务。</summary>
        /// <param name="data">消息体字节。</param>
        public abstract void Invoke(byte[] data);
    }
}
