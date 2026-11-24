using System.Collections.Generic;

namespace ST.Core.Network
{
    /// <summary>
    /// 消息模块定义部分，集中存放序列化后端、处理表、发送缓冲与依赖模块。
    /// </summary>
    public partial class MsgModule
    {
        // 常量定义
        /// <summary>协议中消息长度字段为 ushort，包体长度不能超过该值。</summary>
        const int MAX_MSG_BODY_LENGTH = ushort.MaxValue;

        // 序列化后端定义
        /// <summary>当前使用的消息序列化后端。</summary>
        MsgCodecType m_MsgType = MsgCodecType.Protobuf;

        // 处理表定义
        /// <summary>FlatBuffers 消息处理表。</summary>
        Dictionary<ulong, FlatBufferProcFunBase> m_FbMsgProcDict =
            new Dictionary<ulong, FlatBufferProcFunBase>(CommonDefine.s_ListConst_100);

        /// <summary>Protobuf 消息处理表。</summary>
        Dictionary<ulong, ProtobufProcFunBase> m_PbMsgProcDict =
            new Dictionary<ulong, ProtobufProcFunBase>(CommonDefine.s_ListConst_100);

        // 缓冲定义
        /// <summary>FlatBuffers 构建器，跨消息复用。</summary>
        FlatBuffers.FlatBufferBuilder m_FlatBufferBuilder =
            new FlatBuffers.FlatBufferBuilder(CommonDefine.s_ListConst_1024);

        /// <summary>发送缓冲，跨消息复用，写出前先 <see cref="ByteBuffer.Reset"/>。</summary>
        ByteBuffer m_SendBuffer = new ByteBuffer();

        // 依赖模块定义
        /// <summary>网络模块，在 <see cref="DoInit"/> 中从内核解析。</summary>
        NetModule m_NetModule;
    }
}