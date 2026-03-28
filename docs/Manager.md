# 管理器系统

[← 返回主页](../README.md)

## 概述

统一的管理器生命周期框架。

## 核心接口

### IManager
所有管理器的基类，提供标准生命周期：
- `DoInit()` - 初始化
- `DoUpdate()` - 每帧更新
- `DoLateUpdate()` - 延迟更新
- `DoClose()` - 清理关闭

---
[← 返回主页](../README.md)
