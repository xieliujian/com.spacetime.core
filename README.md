# com.spacetime.core

Unity游戏核心框架，提供网络通信、数据表、事件系统等基础功能。

## 功能模块

### [网络管理](readme/Network.md)
TCP网络通信层，支持Protobuf和FlatBuffers消息序列化，提供连接管理和消息分发功能。

### [数据表系统](readme/Table.md)
二进制数据表加载和读取系统，支持高效的游戏配置数据管理。

### [事件系统](readme/Event.md)
解耦的事件通信机制，用于系统间消息传递。

### [管理器系统](readme/Manager.md)
统一的管理器生命周期框架，提供初始化、更新和清理接口。

### [日志系统](readme/Logging.md)
文件日志系统，支持批量刷新、文件轮转、Unity 日志捕获，提供解耦的接口抽象设计。

### [资源加载系统](readme/ResourceLoad.md)
统一资源加载框架，支持 AssetBundle 与编辑器 AssetDatabase 双模式、同步/异步加载、场景加载、Lua 资产处理，以及编辑器打包工具。

### [UI 管理系统](readme/UI.md)
全功能 UI 管理框架，支持 Panel/Page 生命周期、三层排序（Bottom/Auto/Top）、HideMask 遮挡优化、面板缓存复用、异步资源加载与动态页面挂载。

### [场景漫游摄像机](readme/Camera.md)
轻量级场景漫游摄像机控制器，支持 WASD 移动、鼠标右键旋转、滚轮调速与 Shift 加速，兼容 WebGL 平台。

## 环境要求

- Unity 2022.3.35f1c1
- Universal Render Pipeline (URP) 14.0.11
