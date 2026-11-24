using System;
using System.Reflection;
using Google.Protobuf;
using pb = Google.Protobuf;
using ST.Core.Logging;

namespace ST.Core.Network
{
    /// <summary>
    /// 泛型 Protobuf 消息处理器：反射获取类型静态 <c>_parser</c>，在 <see cref="Invoke"/> 中解析并回调业务。
    /// 消息类型提供 <c>public void Clear()</c> 时会复用实例，否则退回逐条 <c>ParseFrom</c>。
    /// 复用模式下回调收到的是同一个实例，需要跨回调保存时请先 <c>Clone</c>。
    /// </summary>
    /// <typeparam name="T">具体 Protobuf 消息类型。</typeparam>
    public class ProtobufProcFun<T> : ProtobufProcFunBase where T : pb::IMessage
    {
        /// <summary>业务层回调。</summary>
        Run<T> m_Dlg;

        /// <summary>由生成代码提供的静态解析器。</summary>
        pb.MessageParser m_Parser;

        /// <summary>清空复用实例的委托，由生成的 <c>Clear</c> 方法绑定。</summary>
        Run<T> m_ClearDlg;

        /// <summary>复用实例，仅在消息类型支持 <c>Clear</c> 时创建。</summary>
        T m_Obj;

        /// <summary>是否启用实例复用。</summary>
        bool m_IsReuseEnabled;

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
            TryEnableReuse(type);
        }

        /// <summary>
        /// 将消息体解析为 <typeparamref name="T"/> 并调用业务回调。
        /// 解析异常由 <see cref="MsgModule"/> 捕获，以便日志带上消息 ID。
        /// </summary>
        /// <param name="data">Protobuf 编码的消息体。</param>
        public override void Invoke(byte[] data)
        {
            bool isParserValid = m_Parser != null;
            if (!isParserValid)
            {
                return;
            }

            T message = ParseMessage(data);
            bool isMessageValid = message != null;
            if (!isMessageValid)
            {
                return;
            }

            ERunner.Run(m_Dlg, message);
        }

        /// <summary>
        /// 尝试绑定生成代码的 <c>Clear</c> 方法。当前工程的生成代码没有该方法，
        /// 找不到时保持逐条解析，行为与改造前一致。
        /// </summary>
        /// <param name="type">消息类型。</param>
        void TryEnableReuse(Type type)
        {
            MethodInfo clearMethod = type.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
            bool isClearMethodFound = clearMethod != null;
            if (!isClearMethodFound)
            {
                return;
            }

            try
            {
                Run<T> clearDlg = (Run<T>)Delegate.CreateDelegate(typeof(Run<T>), clearMethod);
                T template = (T)m_Parser.ParseFrom(Array.Empty<byte>());
                m_ClearDlg = clearDlg;
                m_Obj = template;
                m_IsReuseEnabled = true;
            }
            catch (Exception e)
            {
                STLogger.LogErrorF("Protobuf 消息 {0} 的实例复用初始化失败，将逐条解析。异常:{1}", type.Name, e);
            }
        }

        /// <summary>优先复用实例，未启用复用时逐条解析。</summary>
        /// <param name="data">Protobuf 编码的消息体。</param>
        /// <returns>解析后的消息实例。</returns>
        T ParseMessage(byte[] data)
        {
            T reusableMessage = m_Obj;
            bool canReuse = m_IsReuseEnabled && (reusableMessage != null) && (m_ClearDlg != null);
            if (!canReuse)
            {
                T parsedMessage = (T)m_Parser.ParseFrom(data);
                return parsedMessage;
            }

            m_ClearDlg(reusableMessage);
            reusableMessage.MergeFrom(data);
            return reusableMessage;
        }
    }
}

