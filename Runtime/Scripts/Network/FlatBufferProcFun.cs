using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST.Core.Network
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FlatBufferProcFun<T> : IFlatBufferProcFun where T : struct, FlatBuffers.IFlatbufferObject
    {
        /// <summary>
        /// 
        /// </summary>
        MsgProcDelegate<T> m_Dlg;
        T m_Obj = default(T);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dlg"></param>
        public FlatBufferProcFun(MsgProcDelegate<T> dlg)
        {
            m_Dlg = dlg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytearray"></param>
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
