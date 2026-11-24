# 网络管理

[← 返回主页](../../README.md)

## 概述

TCP 长连接客户端，负责连接生命周期、粘包拆包、主线程消息分发，以及 Protobuf / FlatBuffers 两种消息的组包与解析。

模块实例由 `GameWorld` 的模块表持有，业务侧通过 `kernel.GetModule<NetModule>()` 与 `kernel.GetModule<MsgModule>()` 获取。分帧格式与消息 ID 宽度属于协议约定，改动会影响服务端兼容性。

## 文件结构

| 目录 | 文件 | 职责 |
| --- | --- | --- |
| `Define/` | `NetworkDefine.cs` | 分帧尺寸、缓冲上限、心跳参数、断线原因、公共枚举 |
| `Module/` | `MsgModule.cs` | 消息处理表、注册与分发 |
| `Module/` | `MsgModule_Send.cs` | Protobuf / FlatBuffers 组包与发送入口 |
| `Module/` | `MsgModule_Define.cs` | 序列化后端、处理表、发送缓冲等成员变量 |
| `Module/` | `NetModule.cs` | 连接接口、生命周期、收发统计入口 |
| `Module/` | `NetModule_Event.cs` | 主线程消息队列、连接事件队列、会话过滤 |
| `Module/` | `NetModule_Heartbeat.cs` | 心跳发送与收包超时 |
| `Module/` | `NetModule_Define.cs` | 底层客户端、事件、队列、心跳计时等成员变量 |
| `Transport/` | `SocketClient.cs` | 连接状态与关闭流程 |
| `Transport/` | `SocketClient_Connect.cs` | 建连、域名解析、连接回调、连接超时 |
| `Transport/` | `SocketClient_Read.cs` | 异步读取回调与继续监听 |
| `Transport/` | `SocketClient_Frame.cs` | 接收缓冲、拆包、残留数据前移 |
| `Transport/` | `SocketClient_Send.cs` | 复用发送缓冲、批量写、线程校验 |
| `Transport/` | `SocketClient_Stat.cs` | 收发字节、消息条数与丢弃计数 |
| `Dispatch/` | `MsgProcFunBase.cs` | 消息处理基类 |
| `Dispatch/` | `ProtobufProcFun*.cs` | Protobuf 消息解析与回调 |
| `Dispatch/` | `FlatBufferProcFun*.cs` | FlatBuffers 消息解析与回调 |

## 接入方式

### 由 GameWorld 注册

`GameLauncher.CollectBaseModules` 已按“消息分发 → 网络 → 启动流程”的顺序注册两个模块，`DoInit` 时互相通过内核解析依赖，注册顺序不影响结果。

~~~csharp
IKernel kernel = GameWorld.gameKernel;
NetModule netModule = kernel.GetModule<NetModule>();
MsgModule msgModule = kernel.GetModule<MsgModule>();
~~~

### 手动装配

测试场景可以用 `internal` 的 `SetNetModule`、`SetMsgModule` 显式接线，并自行调用 `DoUpdate`。参考 `Runtime/Scripts/Test/TestNetwork.cs`。

~~~csharp
NetModule netModule = new NetModule();
MsgModule msgModule = new MsgModule();
msgModule.SetNetModule(netModule);
netModule.SetMsgModule(msgModule);

void Update()
{
    netModule.DoUpdate();
}
~~~

## 连接与重连

~~~csharp
netModule.SendConnect("127.0.0.1", 3580);
netModule.CloseSocket();
bool isConnected = netModule.IsConnected();
~~~

- `SendConnect` 会先关闭旧连接，再向新地址发起连接。
- 主动重连不投递 `onDisconnectEvent`，避免业务在断线回调里重连造成事件自激。
- 只有异常断开与主动 `CloseSocket` 才会派发断线事件。
- 域名解析在线程池完成，解析结果回到主线程再发起连接，DNS 慢不会卡主线程。
- 连接超时 10 秒，DNS 解析时间也计入超时。
- 每次连接递增会话序号，上一会话的残留消息与事件会被丢弃。

## 分帧协议

每帧由三部分组成。

| 字段 | 长度 | 说明 |
| --- | --- | --- |
| 消息长度 | 2 字节 | 小端，包含消息 ID 与负载，最大 65535 |
| 消息 ID | 8 字节 | 小端，即消息类型的静态 `HashID` |
| 负载 | 可变 | Protobuf 或 FlatBuffers 编码结果 |

单帧负载上限为 65535 减 8 字节。超过上限时组包直接放弃并记录错误，不会截断发送。接收缓冲上限 4 MB，超过视为异常数据并断开。

## 发送流程

1. `MsgModule.SendPBMsg` 或 `SendFBMsg` 把长度前缀、消息 ID 与负载写入复用缓冲。
2. `NetModule.SendMessage` 把整帧追加到 `SocketClient` 的发送缓冲，这一步只做一次内存拷贝。
3. `NetModule.DoUpdate` 在本帧末尾调用 `FlushSend`，用一次阻塞写把缓冲内容全部发出。

~~~csharp
ReqLogin msg = new ReqLogin();
bool isSent = msgModule.SendPBMsg(ReqLogin.HashID, msg);
if (!isSent)
{
    // 未连接或缓冲越界，消息没有进入发送队列
}
~~~

- 同一次 flush 之前产生的多条消息会合并成一次写操作。
- 发送缓冲初始 128 KB，上限 4 MB，超过上限会记录错误并断开连接。
- Protobuf 发送复用同一个 `CodedOutputStream`，不再为每条消息分配 4 KB 内部缓冲。
- 发送入口只能在主线程调用，子线程调用会丢弃本次数据并记录一次错误。
- 写失败或超过 500 毫秒未完成会记录原因并断开连接。
- 需要立刻发包时调用 `netModule.Flush()`。

## 接收与分发

1. 读取回调在 IO 线程完成 `EndRead`，字节追加到拆包缓冲。
2. 按分帧协议循环切出完整消息，负载复制一份独立数组后按会话序号入队。
3. 主线程在 `NetModule.DoUpdate` 中每帧最多取出 64 条消息、16 个连接事件。

~~~csharp
netModule.onMsgEvent = OnAnyMessage;

void OnAnyMessage(ulong msgid, byte[] payload)
{
    // 未注册强类型处理器的消息可以在这里统一处理
}
~~~

- 消息先交给 `MsgModule.Dispatcher`，再派发 `onMsgEvent`。
- 已入队的消息在断线后仍会优先分发一次，随后派发 `onDisconnectEvent`。
- 分发过程中连接断开时，本帧剩余消息会被丢弃并记录调试日志。
- 上一会话残留的消息按会话序号过滤，不会串到新连接。

## 消息注册

分发后端由 `RegisterMsgType` 指定，默认 Protobuf。

~~~csharp
msgModule.RegisterMsgType(MsgCodecType.Protobuf);
msgModule.RegisterPBMsg<RspLogin>(OnRspLogin);

void OnRspLogin(RspLogin msg)
{
    // 处理登录回包
}
~~~

~~~csharp
msgModule.RegisterMsgType(MsgCodecType.FlatBuffer);
msgModule.RegisterFBMsg<LoginTable>(OnLoginTable);

void OnLoginTable(LoginTable table)
{
    // 处理 FlatBuffers 表
}
~~~

处理器按类型静态 `HashID` 注册，重复注册同一条消息会记录错误并忽略。解析异常在 `MsgModule` 统一捕获，日志带消息 ID，不会中断后续分发。

## 心跳与超时

心跳消息由业务提供，传输层只负责按间隔触发和检测超时。

~~~csharp
netModule.SetHeartbeat(SendHeartbeat);

void SendHeartbeat()
{
    ReqHeartbeat msg = new ReqHeartbeat();
    msgModule.SendPBMsg(ReqHeartbeat.HashID, msg);
}
~~~

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| 发送间隔 | 5 秒 | 到点调用一次心跳委托 |
| 超时时间 | 30 秒 | 期间没有收到任何消息即断开 |

收到任意消息都会清零空闲计时。超时断开原因为 `heartbeat timeout`，随后走正常断线事件。需要自定义节奏时使用 `SetHeartbeat(sender, intervalMilliseconds, timeoutMilliseconds)`。

不调用 `SetHeartbeat` 时不启用超时检测，避免空闲连接被误判。`DoClose` 会清理心跳设置，模块重新 `DoInit` 后需要业务再次调用 `SetHeartbeat`。

## 断线原因

| 常量 | 值 | 触发场景 |
| --- | --- | --- |
| `REASON_NEW_CONNECT` | new connect | 重连前关闭旧连接 |
| `REASON_CONNECT_FAILED` | connect failed | 建立连接或域名解析失败 |
| `REASON_CONNECT_CALLBACK_FAILED` | connect callback failed | 连接回调处理失败 |
| `REASON_CONNECT_TIMEOUT` | connect timeout | 10 秒内未建立连接 |
| `REASON_READ_FAILED` | read failed | 异步读取异常 |
| `REASON_BEGIN_READ_FAILED` | begin read failed | 投递读取失败 |
| `REASON_WRITE_FAILED` | write failed | 批量写失败或超时 |
| `REASON_SERVER_CLOSED` | server closed the connection | 服务端关闭 |
| `REASON_RECV_BUFFER_OVERFLOW` | recv buffer overflow | 未解析数据超过 4 MB |
| `REASON_INVALID_MESSAGE_LENGTH` | invalid message length | 长度字段小于 8 |
| `REASON_SEND_BUFFER_OVERFLOW` | send buffer overflow | 待发送数据超过 4 MB |
| `REASON_NET_MODULE_CLOSED` | net module closed | 模块关闭 |
| `REASON_CLIENT_CLOSED` | client closed | 业务主动断开 |
| `REASON_HEARTBEAT_TIMEOUT` | heartbeat timeout | 心跳超时 |

业务主动关闭与模块关闭使用 Info 级别日志，异常关闭使用 Warning 级别。

## 收发统计

~~~csharp
netModule.GetNetStat(
    out long upBytes,
    out long downBytes,
    out long upMessages,
    out long downMessages,
    out long droppedMessages);
netModule.ResetNetStat();
~~~

字节数包含帧头，消息条数按完整帧统计，`droppedMessages` 统计未连接或缓冲越界时被丢弃的发送消息。统计值在重连后保留，需要对比单次会话时先调用 `ResetNetStat`。

## 线程边界

| 操作 | 所在线程 |
| --- | --- |
| `Connect`、`CloseSocket`、`SendPBMsg`、`SendFBMsg`、`Flush` | 主线程 |
| 域名解析 | 线程池 |
| `EndRead`、拆包、入队 | 异步 IO 线程 |
| 消息分发、连接事件、心跳检测、批量写 | 主线程 |

业务回调都在主线程执行，可以安全访问 Unity API 与其它模块。发送入口有运行时校验，子线程调用会被丢弃并记录错误。

## 已知限制

- 接收侧每条消息仍会分配一个负载数组，便于业务在回调外持有数据。
- Protobuf 收包在消息类型提供 `Clear` 时会复用实例。当前 pbs 生成代码没有 `Clear`，实际仍逐条新建对象，需要生成器补上后才会生效。
- 发送是主线程阻塞写，网络拥塞时单帧最多卡顿 `SEND_TIMEOUT_MS`。
- 底层使用 `System.Net.Sockets.TcpClient`，WebGL 平台不可用。
- 不包含自动重连、KCP、压缩与加密，这些由业务或服务端协议层提供。
- 没有自动化测试覆盖网络层，现有验证以编译和手工联调为主。

---
[← 返回主页](../../README.md)
