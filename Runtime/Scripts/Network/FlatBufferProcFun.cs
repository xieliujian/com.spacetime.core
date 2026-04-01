using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST.Core.Network
{
    /// <summary>
    /// 泛型 FlatBuffers 表处理器：用 <c>__init</c> 绑定根偏移后调用业务委托。
    /// </summary>
    /// <typeparam name="T">结构体表类型</typeparam>
    public class FlatBufferProcFun<T> : IFlatBufferProcFun where T : struct, FlatBuffers.IFlatbufferObject
    {
        /// <summary>业务层回调。</summary>
        MsgProcDelegate<T> m_Dlg;
        /// <summary>解析后的表实例。</summary>
        T m_Obj = default(T);

        /// <summary>绑定 FlatBuffers 消息处理委托。</summary>
        /// <param name="dlg">收到表类型实例后的回调</param>
        public FlatBufferProcFun(MsgProcDelegate<T> dlg)
        {
            m_Dlg = dlg;
        }

        /// <summary>
        /// 从字节缓冲构造 <see cref="FlatBuffers.ByteBuffer"/> 并完成 <c>__init</c> 后回调；异常时输出错误日志。
        /// </summary>
        /// <param name="bytearray">FlatBuffer 消息体</param>
        public override void Invoke(byte[] bytearray)
        {
            FlatBuffers.ByteBuffer buf = new FlatBuffers.ByteBuffer(bytearray);
            m_Obj.__init(buf.GetInt(buf.Position) + buf.Position, buf);

            try
            {
                m_Dlg(m_Obj);
            }
            catch (Exception e)
            {
                Debugger.Debugger.LogError("process flatbuffer msg error!" + typeof(T).FullName);
                Debugger.Debugger.LogError(e.ToString());
            }
        }
    }
}
