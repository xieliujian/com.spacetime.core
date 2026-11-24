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

- 心跳消息需要服务端协议配合，本次没有新增。
- 消息负载数组池化需要业务承诺不跨回调持有数据，本次没有做。
- Protobuf 消息对象可以通过 `MergeFrom` 复用，本次保持每条新建。
- `WriteFloat`、`ReadFloat` 仍有 `BitConverter` 数组分配，受 Unity 版本 API 限制，本次没有改。
- KCP、压缩、加密属于协议能力，保持原样。

