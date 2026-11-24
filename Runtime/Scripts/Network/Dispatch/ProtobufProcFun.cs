using System;
using System.Reflection;
using pb = Google.Protobuf;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 泛型 Protobuf 消息处理器：反射获取类型静态 <c>_parser</c>，在 <see cref="Invoke"/> 中解析并回调业务。
    /// </summary>
    /// <typeparam name="T">具体 Protobuf 消息类型。</typeparam>
    public class ProtobufProcFun<T> : ProtobufProcFunBase where T : pb::IMessage
    {
        /// <summary>业务层回调。</summary>
        Run<T> m_Dlg;

        /// <summary>由生成代码提供的静态解析器。</summary>
        pb.MessageParser m_Parser;

        /// <summary>最近一次解析结果。</summary>
        T m_Obj;

        /// <summary>绑定委托并解析类型的 <c>MessageParser</c>。</summary>
        /// <param name="dlg">收到强类型消息后的回调。</param>
        public ProtobufProcFun(Run<T> dlg)
        {
            m_Dlg = dlg;

            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField("_parser", BindingFlags.Static | BindingFlags.NonPublic);
            bool isParserFound = fieldInfo != null;
            if (!isParserFound)
            {
                STLogger.LogErrorF("未找到 Protobuf 消息 {0} 的 _parser 字段，消息无法解析。", type.Name);
                return;
            }

            m_Parser = (pb.MessageParser)fieldInfo.GetValue(null);
        }

        /// <summary>
        /// 将消息体解析为 <typeparamref name="T"/> 并调用业务回调。
        /// 解析异常由 <see cref="MsgModule"/> 捕获，以便日志带上消息 ID。
        /// </summary>
        /// <param name="data">Protobuf 编码的消息体。</param>
        public override void Invoke(byte[] data)
        {
            if (m_Parser == null)
                return;

            m_Obj = (T)m_Parser.ParseFrom(data);
            bool isMessageValid = m_Obj != null;
            if (!isMessageValid)
                return;

            ERunner.Run(m_Dlg, m_Obj);
        }
    }
}
