// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// 包内版本，顶点与片元实现拆分在 UI-Default 目录下的 hlsl 文件中。

Shader "UI/Default"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Toggle(UNITY_UI_NOALPHAMUL)] _NotUseUIAlphaMul ("Not Use Alpha Multiply", Float) = 0

        //blend mode
        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlendCustom("SrcBlend", Float) = 1 // One
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstBlendCustom("DstBlend", Float) = 10 // OneMinusSrcAlpha

        // 运行时由 UI 系统写入，声明为隐藏属性以满足 SRP Batcher 的 UnityPerMaterial 校验。
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (0,0,0,0)
        [HideInInspector] _UIMaskSoftnessX ("UIMask Softness X", Float) = 0
        [HideInInspector] _UIMaskSoftnessY ("UIMask Softness Y", Float) = 0
        [HideInInspector] _CircleClipParams ("Circle Clip Params", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "LRMobileOnChip"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlendCustom] [_DstBlendCustom]
        ColorMask [_ColorMask]

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT UNITY_UI_CLIP_CIRCLE
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_NOALPHAMUL

            #include "UI-Default/UI-DefaultInput.hlsl"
            #include "UI-Default/UI-DefaultForwardPass.hlsl"
            ENDHLSL
        }
    }
}
