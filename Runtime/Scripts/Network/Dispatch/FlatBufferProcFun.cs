using FlatBuffers;

namespace ST.Core.Network
{
    /// <summary>
    /// 泛型 FlatBuffers 表处理器：用 <c>__init</c> 绑定根偏移后调用业务委托。
    /// </summary>
    /// <typeparam name="T">结构体表类型。</typeparam>
    public class FlatBufferProcFun<T> : FlatBufferProcFunBase where T : struct, IFlatbufferObject
    {
        /// <summary>业务层回调。</summary>
        Run<T> m_Dlg;

        /// <summary>解析后的表实例。</summary>
        T m_Obj = default(T);

        /// <summary>绑定 FlatBuffers 消息处理委托。</summary>
        /// <param name="dlg">收到表类型实例后的回调。</param>
        public FlatBufferProcFun(Run<T> dlg)
        {
            m_Dlg = dlg;
        }

        /// <summary>
        /// 从消息体构造 <see cref="FlatBuffers.ByteBuffer"/> 并完成 <c>__init</c> 后回调。
        /// 解析异常由 <see cref="MsgModule"/> 捕获，以便日志带上消息 ID。
        /// </summary>
        /// <param name="data">FlatBuffer 消息体。</param>
        public override void Invoke(byte[] data)
        {
            FlatBuffers.ByteBuffer buffer = new FlatBuffers.ByteBuffer(data);
            m_Obj.__init(buffer.GetInt(buffer.Position) + buffer.Position, buffer);
            ERunner.Run(m_Dlg, m_Obj);
        }
    }
}
