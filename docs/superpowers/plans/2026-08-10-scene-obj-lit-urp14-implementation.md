# SceneObjLit URP 14 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Unity 2020-era SceneObjLit fork with a locally customizable URP 14.0.11 Lit implementation while preserving the Shader asset identity.

**Architecture:** Vendor the URP 14 Lit Shader, Lit passes, and selected lighting modules into the existing `SceneObjLit` directory. Keep URP rendering infrastructure as package includes, but redirect every vendored-module dependency to the corresponding local `SceneObj*` file.

**Tech Stack:** Unity 2022.3.35f1c1, Universal Render Pipeline 14.0.11, ShaderLab, HLSL, PowerShell verification.

---

The workspace has no `.git` directory, so commit steps are unavailable. Preserve all existing `.meta` files; Unity import creates `.meta` files for new HLSL assets.

### Task 1: Establish the failing structural baseline

**Files:**
- Inspect: `Packages/com.spacetime.core/Shaders/SceneObjLit.shader`
- Inspect: `Packages/com.spacetime.core/Shaders/SceneObjLit/*.hlsl`

- [ ] **Step 1: Verify the new URP 14 files and properties are currently absent**

Run:

```powershell
$shaderRoot = 'Packages\com.spacetime.core\Shaders'
$localRoot = Join-Path $shaderRoot 'SceneObjLit'
$required = @(
    'SceneObjLitDepthNormalsPass.hlsl',
    'SceneObjUniversalMetaPass.hlsl',
    'SceneObjBRDF.hlsl',
    'SceneObjGlobalIllumination.hlsl',
    'SceneObjRealtimeLights.hlsl',
    'SceneObjAmbientOcclusion.hlsl'
)
$missing = $required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $localRoot $_)) }
$shader = Get-Content -LiteralPath (Join-Path $shaderRoot 'SceneObjLit.shader') -Raw
if (($missing.Count -eq 0) -and $shader.Contains('_SrcBlendAlpha')) { throw 'Baseline unexpectedly already upgraded' }
```

Expected: command succeeds because at least one required local file or URP 14 property is absent.

### Task 2: Vendor the URP 14 source set

**Files:**
- Modify: `Packages/com.spacetime.core/Shaders/SceneObjLit.shader`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjDepthOnlyPass.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjLighting.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjLitForwardPass.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjLitGBufferPass.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjLitInput.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjLitMetaPass.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjMetaInput.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjShadowCasterPass.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjShadows.hlsl`
- Replace: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjUnityGBuffer.hlsl`
- Create: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjLitDepthNormalsPass.hlsl`
- Create: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjUniversalMetaPass.hlsl`
- Create: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjBRDF.hlsl`
- Create: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjGlobalIllumination.hlsl`
- Create: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjRealtimeLights.hlsl`
- Create: `Packages/com.spacetime.core/Shaders/SceneObjLit/SceneObjAmbientOcclusion.hlsl`

- [ ] **Step 1: Copy the exact URP 14 sources to their local destinations**

Use the following exact mapping, preserving existing destination `.meta` files:

```powershell
$urpRoot = 'Library\PackageCache\com.unity.render-pipelines.universal@14.0.11'
$shaderRoot = 'Packages\com.spacetime.core\Shaders'
$localRoot = Join-Path $shaderRoot 'SceneObjLit'
$map = [ordered]@{
    'Shaders\Lit.shader' = 'SceneObjLit.shader'
    'Shaders\LitInput.hlsl' = 'SceneObjLit\SceneObjLitInput.hlsl'
    'Shaders\LitForwardPass.hlsl' = 'SceneObjLit\SceneObjLitForwardPass.hlsl'
    'Shaders\LitGBufferPass.hlsl' = 'SceneObjLit\SceneObjLitGBufferPass.hlsl'
    'Shaders\DepthOnlyPass.hlsl' = 'SceneObjLit\SceneObjDepthOnlyPass.hlsl'
    'Shaders\LitDepthNormalsPass.hlsl' = 'SceneObjLit\SceneObjLitDepthNormalsPass.hlsl'
    'Shaders\ShadowCasterPass.hlsl' = 'SceneObjLit\SceneObjShadowCasterPass.hlsl'
    'Shaders\LitMetaPass.hlsl' = 'SceneObjLit\SceneObjLitMetaPass.hlsl'
    'ShaderLibrary\UniversalMetaPass.hlsl' = 'SceneObjLit\SceneObjUniversalMetaPass.hlsl'
    'ShaderLibrary\MetaInput.hlsl' = 'SceneObjLit\SceneObjMetaInput.hlsl'
    'ShaderLibrary\Lighting.hlsl' = 'SceneObjLit\SceneObjLighting.hlsl'
    'ShaderLibrary\Shadows.hlsl' = 'SceneObjLit\SceneObjShadows.hlsl'
    'ShaderLibrary\UnityGBuffer.hlsl' = 'SceneObjLit\SceneObjUnityGBuffer.hlsl'
    'ShaderLibrary\BRDF.hlsl' = 'SceneObjLit\SceneObjBRDF.hlsl'
    'ShaderLibrary\GlobalIllumination.hlsl' = 'SceneObjLit\SceneObjGlobalIllumination.hlsl'
    'ShaderLibrary\RealtimeLights.hlsl' = 'SceneObjLit\SceneObjRealtimeLights.hlsl'
    'ShaderLibrary\AmbientOcclusion.hlsl' = 'SceneObjLit\SceneObjAmbientOcclusion.hlsl'
}
foreach ($entry in $map.GetEnumerator()) {
    Copy-Item -LiteralPath (Join-Path $urpRoot $entry.Key) -Destination (Join-Path $shaderRoot $entry.Value) -Force
}
```

Expected: all 16 HLSL destinations contain byte-for-byte URP 14 source before remapping.

### Task 3: Apply SceneObj identity and local dependency remapping

**Files:**
- Modify: `Packages/com.spacetime.core/Shaders/SceneObjLit.shader`
- Modify: all 16 HLSL files listed in Task 2

- [ ] **Step 1: Change the Shader identity and all pass includes**

Change the first line to:

```shaderlab
Shader "SpaceTime/Scene/SceneObjLit"
```

Replace every `LitInput.hlsl`, Lit pass, depth pass, shadow pass, and meta pass include in the local Shader with its mapped `SceneObjLit/...` path. Keep `Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl` as a package include.

- [ ] **Step 2: Preserve the URP include guards used by package infrastructure**

Keep each top-level URP guard unchanged, for example:

```hlsl
#ifndef UNIVERSAL_LIGHTING_INCLUDED
#define UNIVERSAL_LIGHTING_INCLUDED
```

Package infrastructure such as `Debugging3D.hlsl` directly includes URP BRDF/GI/RealtimeLights/Shadows. Reusing the original guards prevents those package copies from expanding after the local modules have loaded.

- [ ] **Step 3: Redirect vendored dependencies to local files**

Apply these exact logical remaps:

```text
LitForwardPass -> SceneObjLighting
LitGBufferPass -> SceneObjLighting, SceneObjUnityGBuffer
LitDepthNormalsPass -> SceneObjLighting
ShadowCasterPass -> SceneObjShadows
LitMetaPass -> SceneObjUniversalMetaPass
SceneObjUniversalMetaPass -> SceneObjMetaInput
SceneObjMetaInput -> SceneObjLighting
SceneObjLighting -> SceneObjBRDF, SceneObjGlobalIllumination, SceneObjRealtimeLights, SceneObjAmbientOcclusion
SceneObjGlobalIllumination -> SceneObjRealtimeLights
SceneObjRealtimeLights -> SceneObjAmbientOcclusion, SceneObjShadows
SceneObjUnityGBuffer -> SceneObjLighting
```

Use relative includes such as:

```hlsl
#include "SceneObjLighting.hlsl"
```

In `SceneObjShadows.hlsl`, replace the two URP-relative includes with full package paths:

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.deprecated.hlsl"
```

Leave DBuffer, Debugging3D, Input, Clustering, LightCookie, SurfaceData, Deprecated, Core and render-pipelines.core includes pointing to their packages.

In `SceneObjLighting.hlsl`, load local BRDF, GlobalIllumination, RealtimeLights and AmbientOcclusion before the package `Debugging3D.hlsl` include. This ensures all original URP guards are defined before the debug include attempts to load package copies.

### Task 4: Run structural and Unity verification

**Files:**
- Verify: `Packages/com.spacetime.core/Shaders/SceneObjLit.shader`
- Verify: `Packages/com.spacetime.core/Shaders/SceneObjLit/*.hlsl`
- Verify: `Assets/mats/sceneobjlit.mat`

- [ ] **Step 1: Verify all local includes resolve**

Run a PowerShell include resolver over `SceneObjLit.shader` and all local HLSL files. Resolve relative paths from the including file, `SceneObjLit/...` from the Shader directory, and `Packages/...` from the project root. Expected: zero missing includes.

- [ ] **Step 2: Verify vendored modules do not point back to their URP counterparts**

Run:

```powershell
rg -n 'Packages/com\.unity\.render-pipelines\.universal/(Shaders/(LitInput|LitForwardPass|LitGBufferPass|DepthOnlyPass|LitDepthNormalsPass|ShadowCasterPass|LitMetaPass)|ShaderLibrary/(UniversalMetaPass|MetaInput|Lighting|Shadows|UnityGBuffer|BRDF|GlobalIllumination|RealtimeLights|AmbientOcclusion))\.hlsl' Packages/com.spacetime.core/Shaders/SceneObjLit.shader Packages/com.spacetime.core/Shaders/SceneObjLit
```

Expected: no matches.

- [ ] **Step 3: Verify the local Shader remains aligned to URP 14**

Normalize only the Shader name and the seven local include path pairs, then compare with `Library/PackageCache/com.unity.render-pipelines.universal@14.0.11/Shaders/Lit.shader` using `Compare-Object`. Expected: no differences.

- [ ] **Step 4: Import and compile in Unity batch mode**

Run the project with Unity 2022.3.35f1c1 in batch mode, force asset import, and write a dedicated log file. Expected: exit code 0 and no `Shader error in 'SpaceTime/Scene/SceneObjLit'` in the log.

- [ ] **Step 5: Check the existing Opaque material**

Confirm `Assets/mats/sceneobjlit.mat` still references GUID `40842581642f9e74bb8e57b41cd69b0e`, remains queue 2000 / RenderType Opaque, and loads without missing Shader. Do not claim migration coverage for external legacy Transparent materials.
