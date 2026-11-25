// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// 包内版本，顶点与片元实现拆分在 UIDefault 目录下的 hlsl 文件中。
// 材质面板使用 LWGUI，需要工程内存在 LWGUI-2.x 包。

Shader "SpaceTime/UI/Default"
{
    Properties
    {
        [Main(UITint, _, on, off)] _FoldoutTint("基础", Float) = 0
        [PerRendererData] [Tex(UITint, _Color)] _MainTex("精灵贴图", 2D) = "white" {}
        [HideInInspector] _Color("颜色", Color) = (1,1,1,1)

        [Main(UIState, _, off, off)] _FoldoutState("混合与裁剪", Float) = 0
        [SubEnum(UIState, UnityEngine.Rendering.BlendMode)] _SrcBlendCustom("源混合", Float) = 1
        [SubEnum(UIState, UnityEngine.Rendering.BlendMode)] _DstBlendCustom("目标混合", Float) = 10
        [SubEnum(UIState, UnityEngine.Rendering.ColorWriteMask)] _ColorMask("写颜色通道", Float) = 15
        [SubToggle(UIState, UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha 裁剪", Float) = 0
        [SubToggle(UIState, UNITY_UI_NOALPHAMUL)] _NotUseUIAlphaMul("不做 Alpha 预乘", Float) = 0

        [Main(UIStencil, _, off, off)] _FoldoutStencil("模板", Float) = 0
        [SubEnum(UIStencil, UnityEngine.Rendering.CompareFunction)] _StencilComp("比较方式", Float) = 8
        [Sub(UIStencil)] _Stencil("模板值", Float) = 0
        [SubEnum(UIStencil, UnityEngine.Rendering.StencilOp)] _StencilOp("通过操作", Float) = 0
        [Sub(UIStencil)] _StencilWriteMask("写掩码", Float) = 255
        [Sub(UIStencil)] _StencilReadMask("读掩码", Float) = 255

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

            #include "UIDefault/UIDefaultInput.hlsl"
            #include "UIDefault/UIDefaultForwardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}
