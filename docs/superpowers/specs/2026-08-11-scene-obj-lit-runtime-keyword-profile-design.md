# SceneObjLit 运行时关键字配置设计

## 背景

`SceneObjLit.shader` 的 ForwardLit Pass 直接保留了 URP 14 Lit 的完整管线关键字集合。材质关键字与管线关键字相乘后，编辑器和构建阶段需要处理大量 Shader 变体。

项目已有 `LR_SHADER_RUNTIME` 的历史方案：打包前把注释形式的宏临时启用，在运行时分支中用固定宏替代部分 `multi_compile`。`SceneObjLit` 采用相同思想，但使用包自己的宏名 `ST_SHADER_RUNTIME`，且不依赖旧打包器。

## 目标

- 编辑器保持完整 URP 关键字，方便材质制作与功能调试。
- 打包 Shader 时由外部打包逻辑启用 `ST_SHADER_RUNTIME`。
- 运行时固定使用 Forward+，降低 ForwardLit 的管线变体数量。
- 支持 Android、iOS 和 Windows，并为移动端与桌面端选择不同的阴影和 SH 配置。
- 保留材质级功能差异和普通 GPU Instancing。

## 非目标

- 本次不实现或修改 Shader 打包器。
- 本次不优化 ShadowCaster、DepthOnly、DepthNormals、GBuffer、Meta 或 Universal2D Pass。
- 本次不把法线、Alpha Clip、自发光等材质关键字改成全局固定宏。
- 本次不支持运行时 Light Cookies、DOTS Instancing、SSAO、DBuffer Decal 或 Light Layers。

## 宏入口

在 ForwardLit 的 HLSLPROGRAM 中保留如下标记：

```hlsl
//#define ST_SHADER_RUNTIME 1
```

编辑器中该行保持注释，走完整 URP `multi_compile` 分支。打包器后续负责把它临时替换为：

```hlsl
#define ST_SHADER_RUNTIME 1
```

## ForwardLit 运行时配置

运行时统一定义：

```hlsl
#define _FORWARD_PLUS 1
```

URP 14 的 `Core.hlsl` 会在 `_FORWARD_PLUS` 开启时自动定义 `_ADDITIONAL_LIGHTS` 并禁用 `_ADDITIONAL_LIGHTS_VERTEX`，因此不重复定义 `_ADDITIONAL_LIGHTS`。

Android 和 iOS 使用移动端配置：

```hlsl
#define _MAIN_LIGHT_SHADOWS 1
#define _SHADOWS_SOFT 1
#define _SHADOWS_SOFT_LOW 1
#define EVALUATE_SH_VERTEX 1
```

移动端不定义 `_ADDITIONAL_LIGHT_SHADOWS`。

Windows 使用桌面端配置：

```hlsl
#define _MAIN_LIGHT_SHADOWS_CASCADE 1
#define _ADDITIONAL_LIGHT_SHADOWS 1
#define _SHADOWS_SOFT 1
#define _SHADOWS_SOFT_MEDIUM 1
```

URP 14 的 `Shadows.hlsl` 会根据 `_ADDITIONAL_LIGHT_SHADOWS` 自动定义 `ADDITIONAL_LIGHT_CALCULATE_SHADOWS`，无需重复定义内部宏。Windows 不定义 SH 关键字，使用逐像素 SH。

## 运行时移除的变体

`ST_SHADER_RUNTIME` 分支不声明以下 ForwardLit 变体：

- 主光阴影模式组合。
- 附加光 Vertex/Pixel 组合。
- SH Mixed/Vertex 组合。
- 附加光阴影开关。
- Reflection Probe Blending 和 Box Projection。
- Soft Shadow 质量组合。
- Screen Space Occlusion。
- DBuffer MRT1/MRT2/MRT3。
- Light Cookies。
- Light Layers。
- Forward/Forward+ 组合。
- Dynamic Lightmap。
- Debug Display。
- Rendering Layers 输出。
- Foveated Rendering Keywords。
- DOTS Instancing。

## 继续保留的变体

以下功能按材质、Renderer 或场景变化，运行时仍保留：

- 所有 `shader_feature_local` 材质关键字。
- `LIGHTMAP_SHADOW_MIXING`。
- `SHADOWS_SHADOWMASK`。
- `DIRLIGHTMAP_COMBINED`。
- `LIGHTMAP_ON`。
- `LOD_FADE_CROSSFADE`。
- Fog 模式。
- `multi_compile_instancing` 和 `instancing_options renderinglayer`。

普通 GameObject GPU Instancing 与 DOTS Instancing 是两条不同路径。删除 `DOTS.hlsl` 的运行时 pragma 不影响普通 GPU Instancing。

## 编辑器分支

`ST_SHADER_RUNTIME` 未定义时，保留当前 URP 14 ForwardLit 的全部管线 pragma 和 `include_with_pragmas`。这样编辑器仍可验证未纳入运行时配置的功能，不改变现有材质制作流程。

## 限制

- 运行时 Renderer 必须使用 Forward+。
- 运行时不支持 Light Cookies、DOTS Instancing、SSAO、DBuffer Decal、Light Layers 和 Dynamic Lightmap。
- Android/iOS 不支持附加光阴影。
- 修改上述能力时，必须同步更新 `ST_SHADER_RUNTIME` 分支并重新构建 Shader。

## 验证

1. 宏关闭时导入 Shader，确认编辑器分支无编译错误。
2. 临时启用 `ST_SHADER_RUNTIME` 后重新导入，确认运行时分支无编译错误。
3. 检查 ForwardLit 运行时分支不再包含已移除的 `multi_compile`。
4. 确认运行时分支仍保留 Lightmap、LOD、Fog 和普通 GPU Instancing pragma。
5. 恢复注释形式的宏入口，确保提交状态默认使用编辑器分支。

