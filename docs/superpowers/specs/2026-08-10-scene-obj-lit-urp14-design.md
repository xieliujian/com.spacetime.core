# SceneObjLit URP 14 重写设计

## 目标

废弃基于 Unity 2020/旧版 URP 的 `SceneObjLit` Shader 分叉，以项目当前使用的 Unity 2022.3.35f1c1
和 URP 14.0.11 `Lit.shader` 为基线重写。保留 Shader 资产路径、Shader 名称和主 `.meta`，避免已有材质引用失效。

## 实现边界

- `SceneObjLit.shader` 的属性、SubShader、Pass、关键字和渲染状态对齐 URP 14.0.11 `Lit.shader`。
- 原有 10 个本地 HLSL 文件全部使用 URP 14 对应实现替换，包括本地 Lighting、Shadows 和 UnityGBuffer。
- 新增 URP 14 的 Lit DepthNormals Pass。
- 新增本地 BRDF、GlobalIllumination、RealtimeLights、AmbientOcclusion 四个核心光照模块。
- DBuffer、调试、Forward+ 聚类、Light Cookie、Core、Input 等渲染基础设施继续引用 URP 14 包内实现。
- 所有本地 HLSL 使用 `SceneObj` 文件名前缀，但保留对应 URP 14 原始 include guard。部分包内基础设施（尤其
  `Debugging3D.hlsl`）会重新 include URP Lighting 子模块，复用原 guard 才能阻止包内副本与本地副本重复展开。
  已本地化模块之间必须引用本地副本；只有上述基础设施和未本地化的公共 API 可以保留 URP 包引用。
- `SceneObjLighting.hlsl` 必须先加载本地 BRDF、GI、RealtimeLights、AO/Shadow 链，再加载包内
  `Debugging3D.hlsl`，确保 `DEBUG_DISPLAY` Variant 只使用本地光照模块且不会重复定义。
- `SceneObjShadows.hlsl` 中的相对引用必须显式改为 URP 包路径，其中 `Core.hlsl` 和
  `Shadows.deprecated.hlsl` 保持引用 URP 14 包内文件。

## 文件映射

| URP 14 源文件 | 本地文件 |
| --- | --- |
| `Shaders/LitInput.hlsl` | `SceneObjLitInput.hlsl` |
| `Shaders/LitForwardPass.hlsl` | `SceneObjLitForwardPass.hlsl` |
| `Shaders/LitGBufferPass.hlsl` | `SceneObjLitGBufferPass.hlsl` |
| `Shaders/DepthOnlyPass.hlsl` | `SceneObjDepthOnlyPass.hlsl` |
| `Shaders/LitDepthNormalsPass.hlsl` | `SceneObjLitDepthNormalsPass.hlsl` |
| `Shaders/ShadowCasterPass.hlsl` | `SceneObjShadowCasterPass.hlsl` |
| `Shaders/LitMetaPass.hlsl` | `SceneObjLitMetaPass.hlsl` |
| `ShaderLibrary/UniversalMetaPass.hlsl` | `SceneObjUniversalMetaPass.hlsl` |
| `ShaderLibrary/MetaInput.hlsl` | `SceneObjMetaInput.hlsl` |
| `ShaderLibrary/Lighting.hlsl` | `SceneObjLighting.hlsl` |
| `ShaderLibrary/Shadows.hlsl` | `SceneObjShadows.hlsl` |
| `ShaderLibrary/UnityGBuffer.hlsl` | `SceneObjUnityGBuffer.hlsl` |
| `ShaderLibrary/BRDF.hlsl` | `SceneObjBRDF.hlsl` |
| `ShaderLibrary/GlobalIllumination.hlsl` | `SceneObjGlobalIllumination.hlsl` |
| `ShaderLibrary/RealtimeLights.hlsl` | `SceneObjRealtimeLights.hlsl` |
| `ShaderLibrary/AmbientOcclusion.hlsl` | `SceneObjAmbientOcclusion.hlsl` |

## 兼容性

- Shader 名称保持 `SpaceTime/Scene/SceneObjLit`。
- 保留 `SceneObjLit.shader.meta`，已有 Material GUID 引用不变。
- Properties 与 URP 14 Lit 对齐，保留标准 Lit ShaderGUI 所需的隐藏状态属性。
- 仓库内只发现 `Assets/mats/sceneobjlit.mat` 直接引用该 Shader，且该材质是 Opaque。本次兼容承诺仅覆盖该仓库内
  材质：不直接改写材质 YAML，使用 Shader 默认值，并检查导入后的 render queue、RenderType、keywords 和渲染状态。
- 自定义 Shader 名称不会进入 URP 内置 MaterialPostprocessor 的 Lit 自动升级流程。包外、AssetBundle 或其他项目中的旧
  Transparent 材质可能缺少 `_SrcBlendAlpha`、`_DstBlendAlpha`、`_AlphaToMask`、
  `_BlendModePreserveSpecular` 等 URP 14 序列化状态，其批量迁移工具不属于本次 Shader 重写范围，不能将当前验证结论
  外推到这些材质。
- Pass 覆盖 Forward、GBuffer、ShadowCaster、DepthOnly、DepthNormals、Meta 和 Universal2D。
- 支持 URP 14 的 Forward+、Rendering Layers、DBuffer、Light Cookies、动态光照贴图、LOD CrossFade 和分级软阴影关键字。

## 验证

- 检查所有本地 include 路径存在、已本地化模块没有错误回指 URP 对应实现，并确认本地文件保留 URP 14 原 guard。
- 对 `SceneObjLit.shader` 与 URP 14 `Lit.shader` 做规范化完整 diff；除 Shader 名称、本地 include、注释和必要的
  `CustomEditor`/`FallBack` 差异外，Properties、Tags、LOD、Pass、状态和 pragma 必须一致。
- 触发 Unity 批处理导入或 Shader 编译，检查 Console 中的 Shader error。
- 至少覆盖 Forward、Deferred/GBuffer、DepthNormals/Rendering Layers、Meta 烘焙、方向光及点光源 ShadowCaster、
  Opaque/Transparent/AlphaClip/Premultiply/Multiply、Forward+、DBuffer、Light Cookies、动态光照贴图、分级软阴影、
  LOD CrossFade 和项目目标图形 API 的关键 Variant。
- 检查现有 Opaque 材质 `Assets/mats/sceneobjlit.mat` 的序列化属性兼容性，以及导入后的 render queue、
  RenderType、keywords 和渲染结果；不以此宣称旧 Transparent 材质已完成升级。
- 确认目标目录不存在旧版 API 残留，并确认新增文件具备 `.meta`。
