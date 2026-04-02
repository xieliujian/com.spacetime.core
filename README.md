# com.spacetime.core

Unity游戏核心框架，提供网络通信、数据表、事件系统等基础功能。

## 功能模块

### [网络管理](docs/Network.md)
TCP网络通信层，支持Protobuf和FlatBuffers消息序列化，提供连接管理和消息分发功能。

### [数据表系统](docs/Table.md)
二进制数据表加载和读取系统，支持高效的游戏配置数据管理。

### [事件系统](docs/Event.md)
解耦的事件通信机制，用于系统间消息传递。

### [管理器系统](docs/Manager.md)
统一的管理器生命周期框架，提供初始化、更新和清理接口。

### [日志系统](docs/Logging.md)
文件日志系统，支持批量刷新、文件轮转、Unity 日志捕获，提供解耦的接口抽象设计。

## 环境要求

- Unity 2022.3.35f1c1
- Universal Render Pipeline (URP) 14.0.11
