using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using pb = Google.Protobuf;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 泛型 Protobuf 消息处理器：反射获取类型静态 <c>_parser</c>，在 <see cref="Invoke"/> 中 <c>ParseFrom</c> 并调用业务委托。
    /// </summary>
    /// <typeparam name="T">具体 Protobuf 消息类型</typeparam>
    public class ProtobufProcFun<T> : IProtobufProcFun where T : pb::IMessage
    {
        /// <summary>业务层回调。</summary>
        MsgProcDelegate<T> m_Dlg;
        /// <summary>由生成代码提供的静态解析器。</summary>
        pb.MessageParser m_Parser;
        /// <summary>解析结果缓存（复用引用）。</summary>
        T m_Obj;

        /// <summary>
        /// 绑定委托并解析类型的 <c>MessageParser</c>。
        /// </summary>
        /// <param name="dlg">收到强类型消息后的回调</param>
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
        /// 将字节解析为 <typeparamref name="T"/> 并调用 <see cref="m_Dlg"/>；异常时输出错误日志。
        /// </summary>
        /// <param name="bytearray">Protobuf 编码的消息体</param>
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
                Logger.LogError("process protobuf msg error!" + typeof(T).FullName);
                Logger.LogError(e.ToString());
            }
        }
    }
}
