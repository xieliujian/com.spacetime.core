# 诊断与性能工具

[← 返回主页](../README.md)

运行时 OnGUI 性能浮层，实时显示 FPS、顶点数、三角面数、Draw Call 与 Batch 数目；
底层由 `SceneMeshStatistics` 按采样间隔遍历场景，避免每帧全量遍历。

---

## 快速开始

### 挂载到场景

将 `RuntimePerformanceHud` 组件挂载到任意 GameObject 上，运行时浮层即自动显示。

```csharp
// 也可以在代码中动态创建
var go = new GameObject("PerformanceHud");
go.AddComponent<RuntimePerformanceHud>();
DontDestroyOnLoad(go);
```

### Inspector 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Visible` | bool | `true` | 是否显示浮层 |
| `ToggleKey` | KeyCode | `F3` | 切换显示的热键 |
| `UseToggleKey` | bool | `true` | 是否响应热键切换 |
| `MeshSampleInterval` | float | `0.5` | 网格统计刷新间隔（秒） |
| `ScreenPosition` | Vector2 | `(8, 8)` | 浮层左上角屏幕坐标 |
| `FontSize` | int | `28` | 标签字体大小 |

---

## 运行时显示内容

```
── Runtime Performance ──
FPS: 60.0
顶点: 12,345
三角面: 8,210
MeshRenderer: 42  Skinned: 6
Draw Calls: 38  Batches: 31
```

| 指标 | 来源 | 更新频率 |
|------|------|----------|
| FPS | `Time.unscaledDeltaTime` 帧计数 | 每 0.5 秒 |
| 顶点 / 三角面 | `SceneMeshStatistics.Gather()` | 每 `MeshSampleInterval` 秒 |
| MeshRenderer / Skinned | 同上 | 每 `MeshSampleInterval` 秒 |
| Draw Calls / Batches（编辑器） | `UnityStats.drawCalls` / `batches` | 每帧 |
| Draw Calls（真机 Dev Build） | `ProfilerRecorder` | 每帧 |

> **真机 Draw Call 限制**：非编辑器环境下需同时满足：
> - Player Settings → Development Build ✓  
> - 脚本宏 `ENABLE_PROFILER` 已定义  
>
> 否则浮层仅显示 `Draw Calls: —` 提示。

---

## Draw Call 平台差异

| 平台 | 实现 | 要求 |
|------|------|------|
| Unity Editor | `UnityStats.drawCalls` / `UnityStats.batches` | 无额外要求 |
| 真机 Development Build | `ProfilerRecorder("Draw Calls Count")` | 需脚本宏 `ENABLE_PROFILER` |
| 真机 Release Build | 不可用，显示 `—` | — |

编译宏判断顺序：

```csharp
#if UNITY_EDITOR
    // UnityStats
#elif ENABLE_PROFILER
    // ProfilerRecorder
#else
    // 不可用
#endif
```

---

## SceneMeshStatistics API

```csharp
// 仅统计激活且已启用的渲染器（默认，适合实时性能监控）
SceneMeshStats stats = SceneMeshStatistics.Gather();

// 包含非激活 GameObject 上的渲染器（适合资产总量排查）
SceneMeshStats stats = SceneMeshStatistics.Gather(includeInactive: true);

// 读取结果
Debug.Log(stats.TotalVertices);           // 顶点总数
Debug.Log(stats.TotalTriangles);          // 三角面总数
Debug.Log(stats.MeshRendererCount);       // MeshRenderer 数量
Debug.Log(stats.SkinnedMeshRendererCount); // SkinnedMeshRenderer 数量
```

### SceneMeshStats 字段

| 字段 | 说明 |
|------|------|
| `TotalVertices` | 顶点总数（所有子 Mesh 累加） |
| `TotalTriangles` | 三角面总数（仅 `MeshTopology.Triangles`，其他拓扑跳过） |
| `MeshRendererCount` | 已启用的 `MeshRenderer` 数量 |
| `SkinnedMeshRendererCount` | 已启用的 `SkinnedMeshRenderer` 数量 |

> **统计范围说明**：仅累加 `MeshRenderer`（需配套 `MeshFilter.sharedMesh`）和 `SkinnedMeshRenderer`，
> **不包含** Particle System、UI Canvas、Terrain、Line Renderer 等。

---

## 性能设计

| 优化点 | 说明 |
|--------|------|
| 间隔采样 | 网格遍历和 FPS 计算均每 0.5 秒执行一次，不在每帧全量遍历场景 |
| GUIStyle 缓存 | `_labelStyle` 惰性初始化，仅在 `FontSize` 改变时重建，避免每帧 GC 分配 |
| Rect 缓存 | `CalcSize` 仅在采样周期或字体变化时调用，其余帧复用上次结果 |
| 只查激活对象 | `FindObjectsOfType(false)` 默认只遍历激活 GameObject，减少遍历量 |

---

## 架构

```
RuntimePerformanceHud (MonoBehaviour)
  ├── Update()
  │     ├── FPS 采样（每 0.5s）→ _fps
  │     └── 网格采样（每 MeshSampleInterval）→ SceneMeshStatistics.Gather()
  └── OnGUI()
        ├── GUIStyle 惰性缓存
        ├── BuildText() → StringBuilder
        ├── CalcSize()（仅 _sizeNeedsRecalc 时）
        └── GUI.Box + GUI.Label

SceneMeshStatistics（静态工具类）
  └── Gather(includeInactive)
        ├── FindObjectsOfType<MeshRenderer>
        │     └── GetComponent<MeshFilter>.sharedMesh → vertexCount + GetIndexCount/3
        └── FindObjectsOfType<SkinnedMeshRenderer>
              └── sharedMesh → vertexCount + GetIndexCount/3
```

---

## 依赖

- `UnityEngine`（`MonoBehaviour`、`GUI`、`Input`、`Time`、`MeshRenderer`、`SkinnedMeshRenderer`）
- `UnityEditor`（仅 Editor，`UnityStats`）
- `Unity.Profiling`（仅真机 `ENABLE_PROFILER`，`ProfilerRecorder`）

---

## 许可

MIT License

---
[← 返回主页](../README.md)
