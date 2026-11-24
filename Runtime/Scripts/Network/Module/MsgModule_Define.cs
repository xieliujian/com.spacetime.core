using System.Collections.Generic;

namespace ST.Core.Network
{
    /// <summary>
    /// 消息模块定义部分，集中存放处理表、发送缓冲、FlatBuffers 构建器与依赖模块。
    /// </summary>
    public partial class MsgModule
    {
        // 处理表定义
        /// <summary>
        /// 消息处理表，Protobuf 与 FlatBuffers 共用，键为消息类型的静态 HashID。
        /// 解析方式由注册时创建的处理器决定，不再依赖全局编码开关。
        /// </summary>
        Dictionary<ulong, MsgProcFunBase> m_MsgProcDict =
            new Dictionary<ulong, MsgProcFunBase>(CommonDefine.s_ListConst_100);

        // 缓冲定义
        /// <summary>FlatBuffers 构建器，首次构建时创建，跨消息复用。</summary>
        FlatBuffers.FlatBufferBuilder m_FlatBufferBuilder;

        /// <summary>发送缓冲，跨消息复用，写出前先 <see cref="ByteBuffer.Reset"/>。</summary>
        ByteBuffer m_SendBuffer = new ByteBuffer(NetworkDefine.SEND_BUFFER_INITIAL_CAPACITY);

        /// <summary>
        /// Protobuf 写入器，绑定发送缓冲的底层流复用。
        /// CodedOutputStream 构造时会分配 4 KB 内部缓冲，复用后每条消息不再重复分配。
        /// </summary>
        Google.Protobuf.CodedOutputStream m_PbOutputStream;

        // 依赖模块定义
        /// <summary>网络模块，在 <see cref="DoInit"/> 中解析，发送时缺失会重试。</summary>
        NetModule m_NetModule;

        /// <summary>是否处于 BeginFlatBuffer 与 EndFlatBuffer 之间，用于发现漏掉 End 的构建。</summary>
        bool m_IsFlatBufferBuilding;
    }
}

