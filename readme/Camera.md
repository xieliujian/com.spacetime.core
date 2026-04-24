# 场景漫游摄像机

[← 返回主页](../README.md)

轻量级场景漫游摄像机控制器，以 MonoBehaviour 形式挂载到摄像机 GameObject 上即可使用。  
全部输入 API 基于 Unity 标准 Input 系统，兼容 **WebGL** 平台。

---

## 快速开始

### 基础使用

1. 在场景中选中 Camera GameObject
2. 挂载 `SceneRoamCamera` 组件
3. 运行后即可使用以下操作漫游场景

```
操作速查
─────────────────────────────────
WASD / 方向键      前后左右移动
Q / PageDown       下降
E / PageUp         上升
按住鼠标右键拖拽   旋转视角
鼠标滚轮           调整移动速度
Shift（按住）      速度 × 加速倍率
─────────────────────────────────
```

### Inspector 参数说明

```
[移动]
moveSpeed          基础移动速度（单位/秒），默认 10
moveSpeedMin       速度下限，防止滚轮调至零，默认 0.5
moveSpeedMax       速度上限，默认 100
scrollSpeedStep    每格滚轮调整量，默认 2
boostMultiplier    Shift 加速倍率，默认 3×

[旋转]
mouseSensitivity   鼠标旋转灵敏度，默认 2
pitchMin           俯仰角下限，默认 -89°
pitchMax           俯仰角上限，默认  89°

[平滑]
rotationSmoothing  旋转插值系数 [0, 1]；0 = 即时响应（WebGL 推荐），默认 0
```

---

## WebGL 兼容说明

| 特性 | 实现方式 |
|------|---------|
| 视角旋转 | `Input.GetMouseButton(1)` 按住右键触发，不依赖 `Cursor.lockState` |
| 移动输入 | `Input.GetAxisRaw("Horizontal/Vertical")`，WebGL 原生支持 |
| 滚轮输入 | `Input.GetAxis("Mouse ScrollWheel")`，WebGL 原生支持 |
| 线程 / IO | 无，纯逻辑计算 |

> **注意**：WebGL 平台建议保持 `rotationSmoothing = 0`，避免帧率波动带来的插值误差。

---

## API 一览

| 成员 | 类型 | 说明 |
|------|------|------|
| `moveSpeed` | `float` | 当前移动速度，可在运行时通过滚轮动态修改 |
| `moveSpeedMin` | `float` | 速度下限 |
| `moveSpeedMax` | `float` | 速度上限 |
| `scrollSpeedStep` | `float` | 滚轮每格的速度调整量 |
| `boostMultiplier` | `float` | Shift 加速倍率 |
| `mouseSensitivity` | `float` | 鼠标旋转灵敏度 |
| `pitchMin` | `float` | 俯仰角最小值（°） |
| `pitchMax` | `float` | 俯仰角最大值（°） |
| `rotationSmoothing` | `float` | 旋转平滑系数，范围 [0, 1] |

---

## 核心特性

- **零依赖** — 仅依赖 `UnityEngine`，无外部包、无 Input System 2.0 要求
- **WebGL 安全** — 不使用 `Cursor.lockState`、多线程或原生 IO
- **右键旋转** — 按住鼠标右键拖拽旋转，松开停止，符合浏览器交互习惯
- **动态调速** — 滚轮实时调整 `moveSpeed`，范围可在 Inspector 中限定
- **加速模式** — Shift 键一键倍速，适合大场景快速漫游
- **俯仰限位** — `pitchMin / pitchMax` 防止视角上下翻转
- **首帧无跳变** — `Start()` 从当前 Transform 读取初始欧拉角，避免挂载时角度突变

---

## 架构

```
SceneRoamCamera（MonoBehaviour）
  ├── Update()
  │     ├── HandleRotation()     — 右键拖拽，更新 Pitch / Yaw
  │     ├── HandleScrollSpeed()  — 滚轮调整 moveSpeed
  │     └── HandleMovement()     — WASD/QE + Shift 移动
  └── NormalizeAngle()           — 欧拉角规范化工具（静态）
```

---

## 依赖

- Unity Engine（`MonoBehaviour`、`Input`、`Transform`）

---

[← 返回主页](../README.md)
