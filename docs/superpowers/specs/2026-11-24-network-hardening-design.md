# 网络模块加固与代码整理

- 日期 2026-11-24
- 状态 已实现，MSBuild 编译通过，未跑 Unity PlayMode 与真实服务器
- 目标包 com.spacetime.core
- 范围 `Runtime/Scripts/Network`、`Core/IO/Binary/ByteBuffer.cs`、`Test/TestNetwork.cs`

## 1. 背景

上一轮对比了 SoulEngine 网络与项目网络。本次确认协议保持不变，只处理现有网络实现的不完善之处与 C# 代码组织问题。分帧仍是 2 字节长度加 8 字节消息 ID 加负载，服务端不需要改动。

`IMainThreadTask`、`MainThreadTask` 在改造前核对时没有任何调用方，主线程调度改由 `NetModule` 自己的队列负责，这两个类型已删除。

## 2. 约束

- 分帧格式与消息 ID 宽度不变。
- 不新增心跳消息、KCP、压缩、加密等协议能力。
- 模块实例由 `GameWorld` 的模块表持有，业务侧通过 `m_Kernel.GetModule<T>()` 获取。
- 保留现有公开方法名与发送入口，减少业务侧改动。

## 3. 问题清单

| # | 类别 | 位置 | 现象 | 处理 |
| --- | --- | --- | --- | --- |
| 1 | 单例 | `NetModule.S` | 构造函数里 `s_Instance = this`，后创建的实例会覆盖先创建的。`TestNetwork` 里再 `new` 一个就会抢占全局 | 删除 `s_Instance` 与 `S`，实例由模块表持有 |
| 2 | 单例 | `MsgModule.S` | 同样的静态单例问题 | 删除静态单例 |
| 3 | GC | `SocketClient.OnReceive` | 每条消息 `ReadBytes` 分配一个数组，每次读事件再 `ReadBytes` 分配一个残留数组 | 换成可增长 `byte[]` 加读写下标，残留数据用 `BlockCopy` 前移 |
| 4 | GC | `SocketClient.BeginRead` | 每次读、写都 `new AsyncCallback(...)` | 缓存三个回调委托字段 |
| 5 | CPU | `SocketClient.OnRead` | 每次读都 `Array.Clear` 整个 8192 字节缓冲 | 删除，缓冲按 `EndRead` 返回长度使用 |
| 6 | GC | `MsgModule.SendPBMsg` | `MemoryStream` 加 `ToArray` 加 `ByteBuffer` 加 `ToBytes`，一次发送多次拷贝 | 复用单个 `ByteBuffer`，`Reset` 后直接写入 |
| 7 | GC | `MsgModule.SendFBMsg` | 每次发送 `new ByteBuffer` | 复用发送缓冲 |
| 8 | GC | `ByteBuffer.ReadString` | 先 `new byte[len]` 再被 `ReadBytes` 覆盖 | 去掉第一次分配 |
| 9 | 正确性 | `SocketClient.OnConnect` | 只调用 `GetStream`，没有 `EndConnect` | 补 `EndConnect`，异常统一走关闭 |
| 10 | 正确性 | `SocketClient` | 没有连接超时，`BeginConnect` 可以无限挂起 | 新增 10 秒连接超时，由 `DoUpdate` 驱动 |
| 11 | 正确性 | `SocketClient.Close` | 只关 socket，不清拆包缓冲 | 关闭与重连时重置 `m_RecvLength`，避免上次半包污染新会话 |
| 12 | 正确性 | `SocketClient.ParseFrames` | 长度小于 8 时 `ReadBytes` 抛异常，直接断线且没有可读原因 | 补长度校验，非法长度记录原因后断开 |
| 13 | 正确性 | `MsgModule` 组包 | 长度强转 `ushort`，超过 65535 静默截断 | 发送前校验，超限记录错误并放弃发送 |
| 14 | 正确性 | `FlatBufferProcFun` | `__init` 在 try 之外，坏包异常冒到读回调 | 解析异常上移到 `MsgModule` 统一捕获，日志带消息 ID |
| 15 | 正确性 | `ProtobufProcFun` | `ParseFrom` 在 try 之外，`_parser` 缺失时空引用 | 补 `_parser` 判空，解析异常由 `MsgModule` 捕获 |
| 16 | 正确性 | `MsgModule.RegisterFBMsg/RegisterPBMsg` | 反射取 `HashID` 不判空 | 补判空与错误日志，注册与反注册共用 `TryGetHashId` |
| 17 | 正确性 | `MsgModule.Dispatcher` | 未知消息静默丢弃，排查时没有任何日志 | 补错误日志并带上消息 ID |
| 18 | 正确性 | `MsgModule.DoClose` | 只清空 FlatBuffers 表 | 两张表都清空，发送缓冲关闭并置空 |
| 19 | 正确性 | `NetModule.DoClose` | 关闭时调用 `onConnectEvent.Invoke()`，语义反了 | 删除，关闭只清监听与队列 |
| 20 | 线程 | `NetModule.UpdateEventQueue` | `Queue` 在 IO 线程写、主线程读，`Count` 判断在锁外 | 整段加锁批量取出，锁外分发 |
| 21 | 性能 | `NetModule.UpdateEventQueue` | 一帧清空全部消息，消息突发时单帧卡顿 | 每帧最多分发 64 条 |
| 22 | 接口 | `SocketClient.IsConnected` | 依赖 `TcpClient.Connected`，对端静默断开时仍返回 true | 改为维护自身连接状态 |
| 23 | 接口 | `NetModule` | 没有断线通知 | 新增 `onDisconnectEvent`，连接结果统一在主线程派发 |
| 24 | 组织 | `NetworkDefine.cs` | 枚举、两个基类、委托、常量挤在一个文件 | 常量按用途分组放在 `NetworkDefine`，公共类型与常量同文件，处理基类拆成独立文件，委托改用 `Run<T>` |
| 25 | 命名 | `IFlatBufferProcFun`、`IProtobufProcFun` | 类名带接口前缀 `I`，`Invoke` 是空实现的 virtual | 改名为 `FlatBufferProcFunBase`、`ProtobufProcFunBase`，`Invoke` 改为 abstract |
| 26 | 命名 | `IMsgType` | 枚举带接口前缀 `I` | 改名 `MsgCodecType` |
| 27 | 命名 | `NetworkDefine.s_MaxReadNum` | 编译期常量按静态字段命名 | 改为 `const int MAX_READ_NUM` |
| 28 | 组织 | `MsgModule` | 空的 `DoUpdate`、`DoLateUpdate` 重写会让模块进入逐帧列表 | 删除空重写 |
| 29 | 组织 | `MsgModule.flatBufferBuilder` | getter 里执行 `Clear`，读属性带副作用 | 改为 `BeginFlatBuffer()` |
| 30 | 组织 | `SocketClient.PrintBytes` | 死代码，循环拼接十六进制字符串 | 删除 |
| 31 | 组织 | `TestNetwork` | 依赖全局单例，内含两段 `#if false` 的旧实现 | 改为手动装配两个模块的示例 |

## 4. 关键决策

### 4.1 模块不再使用静态单例

`GameWorld` 在 `DoInit` 前会先把所有模块注册进模块表并注入内核，因此 `MsgModule` 与 `NetModule` 在各自的 `DoInit` 里互相解析是安全的，注册顺序不影响结果。

`SocketClient` 需要把消息与连接事件投递回模块，直接用构造函数注入 `NetModule`，不再通过静态字段。`TestNetwork` 这类手动装配场景通过 `internal` 的 `SetNetModule`、`SetMsgModule` 显式接线。

### 4.2 接收缓冲

拆包缓冲改成固定数组加读写下标，解析完的字节前移到头部，不再用 `MemoryStream` 与 `BinaryReader`。这样每次读事件只产生消息负载本身的分配。

消息负载仍然逐条分配。原因是分发在主线程执行，拆包缓冲会被下一次读取覆盖，负载必须拷贝。`FlatBuffers` 的表结构会引用底层数组，如果改成数组池，业务把表结构保存到回调之外就会读到被复用的数据，所以这次不做池化。

### 4.3 发送缓冲

`ByteBuffer` 新增 `length`、`Reset`、`GetStream`、`GetBuffer`。`MsgModule` 持有一个实例，发送前 `Reset`，Protobuf 通过 `WriteTo(Stream)` 直接写入，FlatBuffers 从 `RawBuffer` 直接切片。

`SocketClient.Send` 在调用 `BeginWrite` 前做一次定长拷贝，因为异步写在回调完成前一直引用传入数组，调用方复用缓冲会破坏在途数据。这次保留这一次拷贝，换掉的是之前的多余拷贝。

第二轮把发送改成复用缓冲加每帧批量阻塞写，异步写与这次定长拷贝都已移除，见第 9 节。

### 4.4 线程边界与事件顺序

`SocketClient` 的所有回调都在异步 IO 线程执行，只做三件事，入队消息、入队连接事件、记录断线原因。`NetModule` 在 `DoUpdate` 里先分发消息，再派发连接事件，保证断线时队列里已收到的消息仍能处理。

### 4.5 异常与日志

解析异常统一在 `MsgModule` 捕获，日志能带上消息 ID。传输层的异常统一走 `CloseWithReason`，记录地址、端口与原因后关闭连接并投递断线事件。

### 4.6 类型拆分与命名

`NetworkDefine.cs` 按项目 Define 规范集中存放常量，常量按用途分组并使用行尾 `//` 注释。`MsgCodecType`、`NetSocketEventType`、`NetSocketEvent` 也放在这个文件里，避免为单个小类型单独建文件。消息回调统一使用项目已有的 `Run<T>` 配合 `ERunner.Run`，不再自定义委托。

### 4.7 目录与 partial

- `Network/` 根目录只放子文件夹，四层职责分别是 `Module/`、`Transport/`、`Dispatch/`、`Define/`。
- 单个类超过 200 行或同时承担两类职责时用 `partial` 拆分，文件命名 `类名_职责.cs`，与主文件放在同一文件夹。
- 模块类只保留生命周期与对外接口，队列、组包、拆包各自独立成 partial。
- 所有文件移动时同步移动 `.meta`，保持 GUID 不变。
- `Define/NetworkDefine.cs` 集中常量与网络公共定义，常量分组并用行尾 `//` 注释。

## 5. 改动清单

| 位置 | 改动 |
| --- | --- |
| `Define/NetworkDefine.cs` | 常量按接收、协议头、分发、连接、断线原因分组并使用行尾注释，同时容纳 `MsgCodecType`、`NetSocketEventType`、`NetSocketEvent` |
| `Dispatch/FlatBufferProcFunBase.cs` | 新增，FlatBuffers 处理基类 |
| `Dispatch/ProtobufProcFunBase.cs` | 新增，Protobuf 处理基类 |
| `Dispatch/FlatBufferProcFun.cs` | 改为继承新基类，解析异常交给上层捕获 |
| `Dispatch/ProtobufProcFun.cs` | 同上，补 `_parser` 判空 |
| `Module/NetModule.cs` | 去掉静态单例，保留生命周期、连接接口与模块依赖 |
| `Module/NetModule_Event.cs` | 新增，主线程消息队列、连接事件队列、每帧上限 |
| `Module/MsgModule.cs` | 去掉静态单例，保留注册与分发，补长度校验与日志 |
| `Module/MsgModule_Send.cs` | 新增，复用发送缓冲完成 Protobuf 与 FlatBuffers 组包 |
| `Transport/SocketClient.cs` | 重写连接状态、连接超时、发送与断线通知 |
| `Transport/SocketClient_Frame.cs` | 新增，接收缓冲、追加、拆包与前移残留 |
| `Core/IO/Binary/ByteBuffer.cs` | 新增 `length`、`Reset`、`GetStream`、`GetBuffer`，修复重复分配与重复 `Close` |
| `Test/TestNetwork.cs` | 改为手动装配两个模块，补断线与关闭示例 |

## 6. 代码内注释

下面几处注释是刻意保留或新增的，改动这些位置前需要先看注释。

- `SocketClient.ParseFrames` 上方说明分帧格式与协议绑定，改动会影响服务端兼容性。
- `SocketClient.ParseFrames` 内说明负载为什么必须拷贝。
- `SocketClient.Send` 内说明为什么要在 `BeginWrite` 前做定长拷贝。
- `MsgModule.SendFBMsg` 内保留原有优化说明，直接读取 `RawBuffer` 少一次拷贝。
- `MsgModule.SendBuffer` 与 `SocketClient.SendMessage` 的文档注释说明发送缓冲由调用方持有，发送后不关闭。

## 7. 验证

执行命令。

```
MSBuild com.spacetime.core.runtime.csproj /t:Rebuild /p:Configuration=Debug
```

结果 `com.spacetime.core.runtime.dll` 生成成功，没有编译错误。只有 Unity 源生成器加载失败的 `CS8032` 环境警告，与本次改动无关。

另外检查了本次改动的 13 个 C# 文件，没有超过 120 字符的行，没有 Tab，均为 UTF-8 带 BOM。

未执行的部分包括 Unity PlayMode 联调、真实服务器收发、Profiler 对比。GC 收益需要运行期抓取确认。

## 8. 非目标与后续

- 心跳消息需要服务端协议配合，第二轮已在传输层提供 `SetHeartbeat` 委托入口，消息本体仍由业务定义。
- 消息负载数组池化需要业务承诺不跨回调持有数据，本次没有做。
- Protobuf 消息对象可以通过 `MergeFrom` 复用，本次保持每条新建。
- `WriteFloat`、`ReadFloat` 仍有 `BitConverter` 数组分配，受 Unity 版本 API 限制，本次没有改。
- KCP、压缩、加密属于协议能力，保持原样。

## 9. 第二轮加固

- 日期 2026-11-24
- 状态 已实现，Roslyn 全量编译通过，未跑 Unity PlayMode 与真实服务器

### 9.1 问题

| # | 类别 | 位置 | 现象 | 处理 |
| --- | --- | --- | --- | --- |
| 32 | 正确性 | `SocketClient.Send` | 多条消息连续 `BeginWrite`，上一笔未完成时抛 `NotSupportedException`，被当作写失败直接断线 | 改为单缓冲批量阻塞写 |
| 33 | GC | `SocketClient.Send` | 每条消息 `new byte[length]` 并在异步写期间持有 | 写入复用发送缓冲，稳态零分配 |
| 34 | 健壮性 | `NetModule` | 没有心跳与收包超时，半开连接无感知 | 新增 `SetHeartbeat` 与超时断开 |
| 35 | 可观测 | `SocketClient` | 没有收发统计 | 新增字节与消息计数 |

### 9.2 发送模型

`MsgModule` 仍然负责组包，`SocketClient.SendMessage` 把整帧追加到复用发送缓冲，`NetModule.DoUpdate` 末尾调用 `FlushSend`，用 `NetworkStream.Write` 一次阻塞写出。

这样同一时刻只有一个写操作，并发写异常消失，每条消息也不再分配数组。`NetModule.Flush()` 供需要立即发包的场景调用。

参考工程 `NetClient` 在 `Send` 阶段就把所有消息拼进一个 `ByteBuffer`，并在 `ProcessSend` 里一次 `Socket.Send`。本次采用同样的批量思路，保留现有 `TcpClient` 与异步读，不改接收线程模型。

与参考工程的差异，参考工程对 `WouldBlock` 重试 5 次，这里超时直接断开。原因是 `NetworkStream.Write` 不暴露已写字节数，重试可能破坏帧顺序。

### 9.3 心跳

`NetModule_Heartbeat.cs` 提供 `SetHeartbeat(Run sender)` 与带间隔、超时的重载，默认 5 秒发送、30 秒超时，对应参考工程 `GameNetwork.UpdateHeartbeat` 与 `UpdateNoResponse`。

传输层不关心心跳消息 ID，业务在委托里用自己的消息发包。未设置委托时不启用超时，避免没有心跳协议的空闲连接被误杀。

### 9.4 统计

`SocketClient_Stat.cs` 记录收发字节与消息条数，读取侧使用 `Interlocked`。写入分布在 IO 线程与主线程，读取不会读到撕裂值。`NetModule.GetNetStat` 与 `ResetNetStat` 对外暴露。

### 9.5 改动清单

| 位置 | 改动 |
| --- | --- |
| `Transport/SocketClient_Send.cs` | 新增，复用发送缓冲、追加、批量 flush、关闭时丢弃 |
| `Transport/SocketClient_Stat.cs` | 新增，收发字节与消息计数 |
| `Transport/SocketClient.cs` | 删除写回调与 `OnWrite`，发送超时改用 `SEND_TIMEOUT_MS`，关闭时清空发送缓冲 |
| `Module/NetModule_Heartbeat.cs` | 新增，心跳发送与超时 |
| `Module/NetModule.cs` | `DoUpdate` 增加心跳与 flush，新增 `Flush`、`GetNetStat`、`ResetNetStat` |
| `Module/NetModule_Define.cs` | 新增心跳计时字段 |
| `Define/NetworkDefine.cs` | 新增发送、心跳常量与心跳超时原因 |
| `Core/IO/Binary/ByteBuffer.cs` | 新增带初始容量的构造函数 |
| `Test/TestNetwork.cs` | 展示收发统计 |
| `readme/Network/Network.md` | 按当前实现重写 |

### 9.6 验证

用 Unity 2022.3.35f1 的 Roslyn 按 runtime 工程引用集编译 `Runtime/Scripts` 下全部 249 个源文件，0 错误 0 警告。

单独编译运行了 `ByteBuffer` 复用自测，覆盖新建容量构造、追加、`Reset` 后重用与只读模式，结果 PASS。

未执行 Unity PlayMode、真实服务器收发与 Profiler 对比。

### 9.7 仍未做

- 接收负载数组池化。`onMsgEvent` 会把数组交给业务，池化回收需要业务承诺不跨回调持有。
- Protobuf 消息对象复用。
- 自动重连与重连退避策略。

## 10. 第三轮加固

- 日期 2026-11-24
- 状态 已实现，Roslyn 全量编译通过，未跑 Unity PlayMode 与真实服务器
- 范围 `Runtime/Scripts/Network`、`readme/Network/Network.md`

### 10.1 问题

| # | 类别 | 位置 | 现象 | 处理 |
| --- | --- | --- | --- | --- |
| 36 | 正确性 | `SocketClient.Connect` | 重连先走 `Close` 并投递断线事件，业务在 `onDisconnectEvent` 里重连会在一帧内反复触发 | 内部重连不投递事件，并给连接事件派发加每帧上限 |
| 37 | 正确性 | `NetModule.DoClose` | 清空队列后，IO 线程仍可能把上一会话的消息入队 | 消息与连接事件携带会话序号，跨会话直接丢弃 |
| 38 | 线程 | `SocketClient.Send` | 复用发送缓冲没有线程约束，子线程调用会破坏缓冲 | 记录主线程 ID，子线程发送丢弃并记录一次错误 |
| 39 | 内存 | `SocketClient.Send` | 发送缓冲无上限，异常发包可持续扩容 | 新增 4 MB 上限，超限记录错误并断开 |
| 40 | 卡顿 | `SocketClient.Connect` | `TcpClient.BeginConnect` 会在主线程同步做 DNS 解析 | 域名解析改到线程池，结果回主线程再建连 |
| 41 | 状态 | `NetModule.UpdateEventQueue` | 一批消息中触发关闭后，剩余消息仍继续分发 | 每条分发后检查连接状态，断开则丢弃剩余 |
| 42 | 可观测 | `SocketClient` | 未连接时发送被静默丢弃 | `SendMessage` 返回 bool，并累计丢弃条数 |
| 43 | 日志 | `SocketClient.Close` | 业务主动关闭与异常关闭同为 Warning | 预期关闭用 Info，异常关闭用 Warning |
| 44 | 线程 | `SocketClient.m_State` | IO 线程写、主线程读，缺少可见性保证 | 改为 `volatile` 字段 |

### 10.2 重连语义

`Connect` 先调用 `CloseInternal(REASON_NEW_CONNECT, notify: false)` 清理旧连接，不向业务投递断线事件。只有异常断开和业务主动 `CloseSocket` 才派发 `onDisconnectEvent`。

`UpdateSocketEventQueue` 每帧最多派发 16 个连接事件，即使业务在同一帧内反复操作连接，也不会无限循环。

### 10.3 会话隔离

`SocketClient` 每次 `Connect` 自增 `m_SessionID`，并通过 `NetModule.BeginSession` 同步给模块。读回调把会话序号带进 `AddEvent`，连接事件也携带序号。

- `AddEvent` 在队列锁内比对序号，不一致直接丢弃。
- `DispatchSocketEvent` 在派发前比对序号，丢弃上一会话的残留事件。
- `DoClose` 清理队列后调用 `EndSession`，把接受序号置 0。
- `SendConnect` 在发起新连接前清空消息与事件队列。

### 10.4 发送保护

- 构造时记录主线程 ID，`SendMessage` 和原始 `Send` 都走 `TryBufferFrame` 统一校验。
- 缓冲超过 `SEND_BUFFER_MAX_SIZE` 时记录错误、累计丢弃并断开连接。
- `SendMessage` 返回 bool，未连接、缓冲越界或线程错误都返回 false。
- `m_DroppedMessages` 通过 `GetNetStat` 的第五个参数暴露。

### 10.5 域名解析

新增 `Transport/SocketClient_Connect.cs`，包含建连、DNS 解析、连接回调与连接超时。

- IP 地址直接 `BeginConnect`。
- 域名走 `Dns.BeginGetHostAddresses`，回调只保存不可变结果对象。
- 主线程 `Update` 读取结果，校验会话序号后再发起连接。
- 超时计时从 `Connect` 开始，DNS 阶段同样计入 10 秒。

### 10.6 分发中断

`UpdateEventQueue` 每分发一条消息检查一次 `IsConnected`，连接已断开时记录剩余条数并停止本帧后续分发。剩余消息随列表清空丢弃，不再交给已关闭连接的业务状态。

### 10.7 文件拆分

`SocketClient` 超过 200 行后按职责拆分。

| 文件 | 行数 | 职责 |
| --- | --- | --- |
| `SocketClient.cs` | 196 | 状态字段、构造、`Update`、关闭流程、关闭日志 |
| `SocketClient_Connect.cs` | 213 | `Connect`、DNS、建连回调、连接超时 |
| `SocketClient_Read.cs` | 73 | `OnRead`、`BeginRead` |
| `SocketClient_Send.cs` | 160 | 发送缓冲、批量写、线程校验 |
| `SocketClient_Frame.cs` | 171 | 接收缓冲与拆包 |
| `SocketClient_Stat.cs` | 83 | 收发与丢弃统计 |

### 10.8 验证

用 Unity 2022.3.35f1 的 Roslyn 按 runtime 工程引用集编译 `Runtime/Scripts` 下全部 252 个源文件，0 错误 0 警告。

未执行 Unity PlayMode、真实服务器收发、DNS 慢查询与断线重连联调。

### 10.9 仍未做

- 网络层自动化测试，包括会话过滤、重连不派发事件、发送缓冲越界。
- 接收负载数组池化与 Protobuf 消息对象复用。
- 自动重连与退避策略，目前仍由业务通过 `onDisconnectEvent` 自行处理。

