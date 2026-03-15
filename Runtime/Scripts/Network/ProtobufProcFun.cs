using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using pb = Google.Protobuf;

namespace ST.Core.Network
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ProtobufProcFun<T> : IProtobufProcFun where T : pb::IMessage
    {
        /// <summary>
        /// 
        /// </summary>
        MsgProcDelegate<T> m_Dlg;
        pb.MessageParser m_Parser;
        T m_Obj;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dlg"></param>
        public ProtobufProcFun(MsgProcDelegate<T> dlg)
        {
            m_Dlg = dlg;

            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("_parser", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                m_Parser = (pb.MessageParser)fieldInfo.GetValue(null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytearray"></param>
        public override void Invoke(byte[] bytearray)
        {
            m_Obj = (T)m_Parser.ParseFrom(bytearray);

            try
            {
                if (m_Obj != null)
                {
                    m_Dlg.Invoke(m_Obj);
                }
            }
            catch (Exception e)
            {
                Debugger.Debugger.LogError("process protobuf msg error!" + typeof(T).FullName);
                Debugger.Debugger.LogError(e.ToString());
            }
        }
    }
}
