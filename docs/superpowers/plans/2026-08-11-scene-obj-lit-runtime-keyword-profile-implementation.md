# SceneObjLit Runtime Keyword Profile Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an editor/runtime keyword split to SceneObjLit ForwardLit so packaged Android, iOS, and Windows shaders use a fixed Forward+ profile with substantially fewer pipeline variants.

**Architecture:** A commented `ST_SHADER_RUNTIME` marker selects between the existing complete URP 14 pragma set and a fixed runtime profile. Material keywords and object-dependent Lightmap, LOD, Fog, and GPU Instancing variants remain shared by both branches; runtime-only exclusions are confined to ForwardLit.

**Tech Stack:** Unity 2022.3.35f1c1, URP 14.0.11, ShaderLab/HLSL, PowerShell verification.

---

### Task 1: Add the ForwardLit runtime profile

**Files:**
- Modify: `Shaders/SceneObjLit.shader:248-281`

- [x] **Step 1: Add the runtime macro and fixed pipeline definitions**

Insert immediately after the ForwardLit material keyword block:

```hlsl
            //#define ST_SHADER_RUNTIME 1

            #if defined(ST_SHADER_RUNTIME)
                #define _FORWARD_PLUS 1

                #if defined(SHADER_API_MOBILE)
                    #define _MAIN_LIGHT_SHADOWS 1
                    #define _SHADOWS_SOFT 1
                    #define _SHADOWS_SOFT_LOW 1
                    #define EVALUATE_SH_VERTEX 1
                #else
                    #define _MAIN_LIGHT_SHADOWS_CASCADE 1
                    #define _ADDITIONAL_LIGHT_SHADOWS 1
                    #define _SHADOWS_SOFT 1
                    #define _SHADOWS_SOFT_MEDIUM 1
                #endif
            #else
```

Keep the existing complete URP pipeline pragma block inside the `#else` branch, including Foveated Rendering,
Rendering Layers, Dynamic Lightmap, and Debug Display. Close the branch after those editor-only pragmas:

```hlsl
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #endif
```

- [x] **Step 2: Keep object-dependent Unity variants shared**

Keep these pragmas outside the runtime/editor branch:

```hlsl
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
```

- [x] **Step 3: Keep normal GPU Instancing and remove runtime DOTS variants**

Use this GPU Instancing block:

```hlsl
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #if !defined(ST_SHADER_RUNTIME)
                #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #endif
```

This preserves ordinary GPU Instancing while excluding `DOTS_INSTANCING_ON` from packaged runtime variants.

- [x] **Step 4: Inspect the focused diff**

Run:

```powershell
git diff -- Shaders/SceneObjLit.shader
```

Expected: only the ForwardLit keyword and instancing sections change; Properties and all other Passes remain untouched.

### Task 2: Verify the keyword partition

**Files:**
- Verify: `Shaders/SceneObjLit.shader`

- [x] **Step 1: Verify required runtime defines**

Run:

```powershell
$path = 'Shaders\SceneObjLit.shader'
$text = Get-Content -Raw -Encoding UTF8 $path
$required = @(
    '//#define ST_SHADER_RUNTIME 1',
    '#define _FORWARD_PLUS 1',
    '#define _MAIN_LIGHT_SHADOWS 1',
    '#define _MAIN_LIGHT_SHADOWS_CASCADE 1',
    '#define _ADDITIONAL_LIGHT_SHADOWS 1',
    '#define _SHADOWS_SOFT_LOW 1',
    '#define _SHADOWS_SOFT_MEDIUM 1',
    '#define EVALUATE_SH_VERTEX 1'
)
$missing = $required | Where-Object { -not $text.Contains($_) }
if ($missing.Count -gt 0) { throw "Missing runtime definitions: $($missing -join ', ')" }
```

Expected: exit code 0 with no missing definitions.

- [x] **Step 2: Verify retained shared variants**

Run:

```powershell
$path = 'Shaders\SceneObjLit.shader'
$text = Get-Content -Raw -Encoding UTF8 $path
$retained = @(
    '#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING',
    '#pragma multi_compile _ SHADOWS_SHADOWMASK',
    '#pragma multi_compile _ DIRLIGHTMAP_COMBINED',
    '#pragma multi_compile _ LIGHTMAP_ON',
    '#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE',
    '#pragma multi_compile_fog',
    '#pragma multi_compile_instancing'
)
$missing = $retained | Where-Object { -not $text.Contains($_) }
if ($missing.Count -gt 0) { throw "Missing retained variants: $($missing -join ', ')" }
```

Expected: exit code 0 with no missing retained variants.

- [x] **Step 3: Verify excluded features are inside editor-only branches**

Inspect the ForwardLit section:

```powershell
$content = Get-Content -Encoding UTF8 'Shaders\SceneObjLit.shader'
$content[225..300]
```

Expected: Light Cookies, SSAO, DBuffer, Light Layers, Reflection Probe variants, Foveated Rendering, Rendering Layers, Dynamic Lightmap, Debug Display, and DOTS include appear only in `#else` or `!ST_SHADER_RUNTIME` blocks.

### Task 3: Compile both branches

**Files:**
- Temporarily modify and restore: an isolated verification copy of `Shaders/SceneObjLit.shader`
- Inspect: Unity Editor log

- [x] **Step 1: Compile the default editor branch**

Leave the marker commented:

```hlsl
//#define ST_SHADER_RUNTIME 1
```

Allow Unity to reimport the Shader, then search the Editor log:

```powershell
Select-String -Path "$env:LOCALAPPDATA\Unity\Editor\Editor.log" `
    -Pattern 'Shader error in.*SceneObjLit|SceneObjLit.shader.*error' `
    -CaseSensitive:$false
```

Expected: no SceneObjLit Shader errors.

- [x] **Step 2: Compile the runtime branch**

Copy the Shader into an isolated minimal Unity project, then temporarily change the copied marker to:

```hlsl
#define ST_SHADER_RUNTIME 1
```

Import the verification project with Windows, Android, and iOS build targets, then run the same Editor log search.
Expected: every target exits with code 0 and reports no SceneObjLit Shader errors.

- [x] **Step 3: Confirm the packaging marker remains inactive in source**

Confirm the source remains:

```hlsl
//#define ST_SHADER_RUNTIME 1
```

Confirm the final diff contains the commented marker and no temporary active runtime macro.

- [x] **Step 4: Commit the implementation**

Run:

```powershell
git add -- Shaders/SceneObjLit.shader `
    docs/superpowers/plans/2026-08-11-scene-obj-lit-runtime-keyword-profile-implementation.md `
    docs/superpowers/plans/2026-08-11-scene-obj-lit-runtime-keyword-profile-implementation.md.meta
git commit -m "feat: add SceneObjLit runtime keyword profile"
```
