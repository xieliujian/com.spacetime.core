# MsgModule 双后端分发整改

- 日期 2026-11-24
- 状态 已实现，MSBuild 编译通过，未跑 Unity PlayMode 与真实服务器
- 目标包 com.spacetime.core
- 范围 `Runtime/Scripts/Network` 下与 MsgModule 收发相关的代码

## 1. 背景

MsgModule 同时承载 Protobuf 与 FlatBuffers 两套处理器，但分发入口只有一个全局 `m_MsgType`。这带来三个直接后果。

- 默认值是 Protobuf，`RegisterMsgType` 在整个仓库没有调用点，只注册 FlatBuffers 处理器时收到的消息仍走 Protobuf 表，日志还会提示"未注册的 Protobuf 消息"。
- 一个进程只能解一种编码，两种协议共存时另一半消息必然丢弃。
- 注册与反序列化被拆成两张表、两个几乎相同的分发函数，重复代码掩盖了真正的分支条件。

## 2. 问题清单

| # | 级别 | 位置 | 现象 | 处理 |
| --- | --- | --- | --- | --- |
| 1 | 高 | `MsgModule` | 全局 `m_MsgType` 让两套后端互斥，FlatBuffers 收包路径实际不可达 | 合并为一张处理表和统一基类，删除全局编码开关 |
| 2 | 高 | `MsgModule.SendFBMsg` | 不校验构建器是否 `Finish`，未完成时 `Length - Position` 等于整块容量，会把 1024 字节零数据当包体发出 | 新增 `EndFlatBuffer` 统一完成构建并发送，`SendFBMsg` 拒绝未完成构建器 |
| 3 | 中 | `MsgModule.DoInit` | 只在初始化时解析 NetModule，找不到就永久失败 | 发送时惰性重试解析 |
| 4 | 中 | `MsgModule.SendBuffer` | 断线时 `SocketClient` 静默丢弃，调用方拿不到任何结果 | 发送链改为返回 bool 并记录日志 |
| 5 | 低 | `MsgModule.WriteHeader` | 上限常量名表示负载，实际比较的是帧长；负数负载会转成极大的 ushort | 常量改名 `MAX_MSG_FRAME_LENGTH`，补负值判断，先算上限再相加 |
| 6 | 低 | `MsgModule.TryGetHashId` | `Convert.ToUInt64` 对字符串或负数会抛异常，中断整个注册流程 | 拆出 `TryConvertHashId`，异常转成注册失败日志 |
| 7 | 低 | `MsgModule` 全体 | 处理表与分发函数重复、`DoClose` 不释放构建器、空委托也能注册 | 合并分发、关闭时释放构建器、注册前校验委托 |

## 3. 方案

### 3.1 统一处理表

新增 `MsgProcFunBase`，只暴露 `Invoke(byte[] data)`。`ProtobufProcFunBase` 与 `FlatBufferProcFunBase` 都继承它，原有泛型处理器保持不变。

`MsgModule` 只维护一张 `Dictionary<ulong, MsgProcFunBase>`。注册时根据泛型类型创建对应处理器，分发时按消息 ID 直接调用，编码方式由处理器自己决定。帧头只有消息 ID，消息 ID 本来就唯一确定处理器，不需要再维护全局编码类型。

保留 `ProtobufProcFunBase` 与 `FlatBufferProcFunBase` 是为了不破坏已有的扩展代码，两者现在只是语义标记。

### 3.2 FlatBuffers 发送

新增 `EndFlatBuffer(ulong msgid, int rootTable)`，由模块内部执行 `Finish` 并组包。调用方不再需要自己 `Finish`，也不会拿到可以随意传入的共享构建器。

`BeginFlatBuffer` 增加构建中标记，嵌套调用会记录错误，避免外层数据被 `Clear` 后仍然发送。

`SendFBMsg` 保留给外部构建器，但会检查 `DataBuffer.Position > 0`。当前工程使用的 FlatBuffers 版本只在 `Finish` 时把 `Position` 设成数据起点，未完成构建时恒为 0，这个判断与版本实现绑定，升级 FlatBuffers 时需要重新核对。

### 3.3 依赖与失败反馈

- `ResolveNetModule` 在 `DoInit` 与发送时都会尝试解析，模块晚注册也能恢复。
- `SendPBMsg`、`SendFBMsg`、`EndFlatBuffer` 返回 bool。
- `NetModule.SendMessage` 与 `SocketClient.SendMessage` 返回 bool，未连接、空缓冲、非主线程都返回 false。
- `MsgModule` 在未连接时记录警告，未绑定 NetModule 时记录错误。

## 4. 改动清单

| 位置 | 改动 |
| --- | --- |
| `Dispatch/MsgProcFunBase.cs` | 新增，Protobuf 与 FlatBuffers 处理器的公共入口 |
| `Dispatch/ProtobufProcFunBase.cs` | 改为继承 `MsgProcFunBase` |
| `Dispatch/FlatBufferProcFunBase.cs` | 改为继承 `MsgProcFunBase` |
| `Module/MsgModule.cs` | 单表注册与分发，惰性解析 NetModule，安全转换 HashID，关闭时释放构建器 |
| `Module/MsgModule_Define.cs` | 单个处理表、惰性构建器、预分配发送缓冲、构建中标记、复用 Protobuf 写入器 |
| `Module/MsgModule_Send.cs` | 新增 `EndFlatBuffer`，发送链返回 bool，补负值与上限校验，复用 `CodedOutputStream` |
| `Dispatch/ProtobufProcFun.cs` | 追加可选的实例复用，消息类型没有 `Clear` 时退回逐条解析 |
| `Module/NetModule.cs` | `SendMessage` 返回 bool |
| `Transport/SocketClient_Send.cs` | `SendMessage` 返回 bool，保留主线程校验与丢弃统计 |
| `Define/NetworkDefine.cs` | 删除 `MsgCodecType`，接收 `MAX_MSG_FRAME_LENGTH` 与 `SEND_BUFFER_INITIAL_CAPACITY` |

## 5. 接口变化

| 接口 | 变化 | 业务侧处理 |
| --- | --- | --- |
| `MsgModule.RegisterMsgType` | 删除 | 删除该调用，注册什么处理器就用什么编码 |
| `MsgCodecType` | 删除 | 同上 |
| `MsgModule.EndFlatBuffer` | 新增 | FlatBuffers 发送改为 `BeginFlatBuffer` 后调用本方法 |
| `MsgModule.SendPBMsg` | 返回值从 void 改为 bool | 忽略返回值时无需改动 |
| `MsgModule.SendFBMsg` | 返回值从 void 改为 bool，并要求构建器已 `Finish` | 建议改用 `EndFlatBuffer` |
| `NetModule.SendMessage` | 返回值从 void 改为 bool | 忽略返回值时无需改动 |

## 6. 代码内注释

- `MsgProcFunBase` 类注释说明单表分发的原因。
- `MsgModule.Dispatcher` 注释说明编码由处理器决定。
- `MsgModule.TryGetHashId` 与 `TryConvertHashId` 注释说明非法值只记录失败。
- `BeginFlatBuffer` 注释说明再次调用会清空未发送数据。
- `EndFlatBuffer` 注释说明 `Finish` 由模块统一处理。
- `SendFBMsg` 内注释说明 `Position > 0` 判断与 FlatBuffers 版本的绑定关系。
- `WriteHeader` 注释说明长度字段统计的是消息 ID 加负载。
- `SendBuffer` 内注释说明发送时重新解析 NetModule 的原因。

## 7. 验证

用 Visual Studio 2022 的 MSBuild 编译 runtime 工程。

```
MSBuild com.spacetime.core.runtime.msgmodule.csproj /t:Rebuild /p:Configuration=Debug
```

结果 `com.spacetime.core.runtime.dll` 生成成功，没有编译错误。只剩 `CS8032` 环境警告，来源是 Unity 源生成器在 MSBuild 下加载失败，与本次改动无关。

临时工程是为了补上 Unity 尚未重新生成的新文件引用，验证后已删除。

另外检查了本次改动的 9 个 C# 文件，均为 UTF-8 带 BOM，无 Tab，单行不超过 120 字符。

未执行 Unity PlayMode、真实服务器收发与 Profiler 对比。

## 8. 未做

- 收包负载数组池化仍维持原状。
- 自动重连与重连退避策略，不在本次范围。
- `SendFBMsg` 的构建完成判断依赖当前 vendored FlatBuffers 实现，升级中间件时需要回归。

## 9. 追加优化：Protobuf 发送写入器与接收实例复用

- 日期 2026-11-24
- 状态 已实现，MSBuild 编译通过，运行时自测 9 项全部 PASS

### 9.1 发送写入器复用

原来 `SendPBMsg` 调用 `message.WriteTo(Stream)`，内部每次 `new CodedOutputStream(output)`，构造时固定分配 4096 字节缓冲。消息再小也按 4 KB 计入 GC。

现在 `MsgModule` 持有 `m_PbOutputStream`，用 `leaveOpen` 绑定发送缓冲的流。每条消息写入后 `Flush`，内部偏移会复位，可以继续复用。写入前仍然 `Reset` 发送缓冲并写帧头，写入后校验实际长度与 `CalculateSize` 一致。

`DoClose` 先 `Dispose` 写入器再关闭发送缓冲，避免向已关闭的流 flush。

### 9.2 接收实例复用

`ProtobufProcFun<T>` 增加复用分支。消息类型提供 `public void Clear()` 时，注册阶段把 `Clear` 绑定成 `Run<T>` 委托，每条消息先 `Clear` 再 `MergeFrom`，省掉整棵消息对象。

当前工程的 pbs 生成代码没有 `Clear`，`Login.cs` 2773 行里一处都没有，所以运行时自动退回逐条 `ParseFrom`，行为与改造前一致。生成器补上 `Clear` 后这段逻辑会自动生效。

复用模式要求业务不要跨回调持有消息实例，需要保存时先 `Clone`。

### 9.3 运行时自测

临时控制台工程引用 runtime 程序集，覆盖三项。

- 同一个 `CodedOutputStream` 连续写两条消息，长度与解析结果都正确
- `Clear` 的 open instance 委托绑定可用
- 当前 pbs 消息走降级路径，两次回调拿到不同实例

结果 9 项检查全部 PASS，`failed=0`，临时工程已删除。

### 9.4 仍需生成器配合

要真正启用接收实例复用，需要在 .proto 的 C# 生成模板里补 `public void Clear()` 并重新生成 `Runtime/Scripts/Msg/pbs`。只改运行时不够。

