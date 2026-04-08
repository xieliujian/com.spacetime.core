# 网络管理

[← 返回主页](../README.md)

## 概述

TCP网络通信层，支持Protobuf和FlatBuffers消息序列化。

## 核心类

### NetManager
单例网络管理器，负责连接管理和消息队列处理。

### SocketClient
TCP socket客户端实现。

### MsgDispatcher
消息分发器，支持Protobuf和FlatBuffers两种序列化格式。

---
[← 返回主页](../README.md)
