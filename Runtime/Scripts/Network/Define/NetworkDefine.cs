namespace ST.Core.Network
{
    /// <summary>
    /// 网络层通用定义：分帧尺寸、缓冲上限、分发上限、连接超时与断线原因。
    /// </summary>
    public static class NetworkDefine
    {
        // 接收定义
        public const int MAX_READ_NUM = 8192; // 单次异步读取使用的最大字节数
        public const int INITIAL_RECV_BUFFER_SIZE = 16384; // 接收缓冲初始容量
        public const int MAX_RECV_BUFFER_SIZE = 4 * 1024 * 1024; // 接收缓冲上限，超过视为异常数据

        // 协议头定义
        public const int MSG_LENGTH_SIZE = sizeof(ushort); // 消息长度字段占用的字节数
        public const int MSG_ID_SIZE = sizeof(ulong); // 消息 ID 字段占用的字节数

        // 分发定义
        public const int MAX_PROCESS_MSG_PER_FRAME = 64; // 单帧最多分发的消息条数

        // 发送定义
        public const int SEND_BUFFER_SIZE = 131072; // 发送缓冲初始容量，同一帧的消息在缓冲内批量发送
        public const int SEND_TIMEOUT_MS = 500; // 同步发送超时时间，单位毫秒

        // 心跳定义
        public const int HEARTBEAT_INTERVAL_MS = 5000; // 心跳发送间隔，单位毫秒
        public const int HEARTBEAT_TIMEOUT_MS = 30000; // 心跳超时，超时内未收到任何消息即断开

        // 连接定义
        public const int CONNECT_TIMEOUT_MS = 10000; // TCP 连接超时时间，单位毫秒

        // 断线原因定义
        public const string REASON_NEW_CONNECT = "new connect"; // 主动重连前关闭旧连接
        public const string REASON_CONNECT_FAILED = "connect failed"; // 连接建立失败
        public const string REASON_CONNECT_CALLBACK_FAILED = "connect callback failed"; // 连接回调处理失败
        public const string REASON_CONNECT_TIMEOUT = "connect timeout"; // 连接超时
        public const string REASON_READ_FAILED = "read failed"; // 异步读取失败
        public const string REASON_BEGIN_READ_FAILED = "begin read failed"; // 投递异步读取失败
        public const string REASON_WRITE_FAILED = "write failed"; // 异步发送失败
        public const string REASON_SERVER_CLOSED = "server closed the connection"; // 服务器主动关闭
        public const string REASON_RECV_BUFFER_OVERFLOW = "recv buffer overflow"; // 接收缓冲超过上限
        public const string REASON_INVALID_MESSAGE_LENGTH = "invalid message length"; // 消息长度字段非法
        public const string REASON_NET_MODULE_CLOSED = "net module closed"; // 网络模块关闭
        public const string REASON_CLIENT_CLOSED = "client closed"; // 业务主动断开
        public const string REASON_HEARTBEAT_TIMEOUT = "heartbeat timeout"; // 心跳超时

        // 日志文案定义
        public const string LOG_CLOSE_REASON = "网络连接关闭，地址 {0}:{1}，原因 {2}"; // 关闭连接的基础日志
        public const string LOG_CLOSE_REASON_VALUE = "网络连接关闭，地址 {0}:{1}，原因 {2}，数值 {3}"; // 带整型细节
        public const string LOG_CLOSE_REASON_DETAIL = "网络连接关闭，地址 {0}:{1}，原因 {2}，详情 {3}"; // 带文本细节
        public const string LOG_CONNECT_START = "网络开始连接，地址 {0}:{1}"; // 发起连接
        public const string LOG_CONNECT_SUCCESS = "网络连接成功，地址 {0}:{1}"; // 连接建立成功
        public const string LOG_CLOSE_SUMMARY = "网络模块关闭，累计发送 {0} 字节 / {1} 条，接收 {2} 字节 / {3} 条"; // 关闭时的统计汇总
    }

    /// <summary>
    /// 消息序列化后端选择，供 <see cref="MsgModule"/> 使用。
    /// </summary>
    public enum MsgCodecType
    {
        FlatBuffer, // 使用 FlatBuffers 编解码
        Protobuf, // 使用 Google Protobuf 编解码
    }

    /// <summary>
    /// 底层套接字产生、需要切回主线程处理的事件类型。
    /// </summary>
    public enum NetSocketEventType
    {
        Connected, // TCP 连接建立成功
        Disconnected, // 连接断开或建立失败
    }

    /// <summary>
    /// 套接字事件数据，由网络线程写入、主线程消费。
    /// </summary>
    public struct NetSocketEvent
    {
        /// <summary>事件类型。</summary>
        public NetSocketEventType type;

        /// <summary>断线或失败原因，连接成功时为空字符串。</summary>
        public string reason;
    }
}
