namespace ST.Core.Network
{
    /// <summary>
    /// 消息处理器公共基类：Protobuf 与 FlatBuffers 处理器实现同一个入口。
    /// <see cref="MsgModule"/> 只按消息 ID 维护一张处理表，解析方式由具体处理器自行决定，
    /// 因此同一进程里两种编码可以共存，不需要全局编码开关。
    /// </summary>
    public abstract class MsgProcFunBase
    {
        /// <summary>将消息体解析为具体消息类型或表类型并回调业务。</summary>
        /// <param name="data">消息体字节。</param>
        public abstract void Invoke(byte[] data);
    }
}
