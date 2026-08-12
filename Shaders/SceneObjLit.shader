Shader "SpaceTime/Scene/SceneObjLit"
{
    Properties
    {
        [Main(Base, _, off, off)] _FoldoutBase("基础设置", Float) = 0

        // Specular vs Metallic workflow. Keywords and render states are controlled independently.
        [SubEnum(Base, Specular, 0, Metallic, 1)]
        _WorkflowMode("工作流", Float) = 1.0

        [SubToggle(Base, _SPECULAR_SETUP)]
        _SpecularSetup("使用高光工作流关键字", Float) = 0.0

        [SubToggle(Base, _SURFACE_TYPE_TRANSPARENT)]
        _Surface("透明表面", Float) = 0.0

        [SubEnum(Base, Alpha, 0, Premultiply, 1, Additive, 2, Multiply, 3)]
        _Blend("透明混合方式", Float) = 0.0

        [SubToggle(Base, _ALPHAPREMULTIPLY_ON)]
        _AlphaPremultiply("Alpha 预乘关键字", Float) = 0.0

        [SubToggle(Base, _ALPHAMODULATE_ON)]
        _AlphaModulate("Alpha 调制关键字", Float) = 0.0

        [SubEnum(Base, UnityEngine.Rendering.CullMode)]
        _Cull("剔除模式", Float) = 2.0

        [SubToggle(Base, _ALPHATEST_ON)]
        _AlphaClip("Alpha 裁剪", Float) = 0.0

        [Sub(Base)] [ShowIf(_AlphaClip, Equal, 1)]
        _Cutoff("Alpha 裁剪阈值", Range(0.0, 1.0)) = 0.5

        [SubToggle(Base, _)]
        _ReceiveShadows("接收阴影", Float) = 1.0

        [SubToggle(Base, _RECEIVE_SHADOWS_OFF)]
        _ReceiveShadowsOff("禁用接收阴影关键字", Float) = 0.0

        [SubEnum(Base, UnityEngine.Rendering.BlendMode)]
        _SrcBlend("颜色源混合", Float) = 1.0

        [SubEnum(Base, UnityEngine.Rendering.BlendMode)]
        _DstBlend("颜色目标混合", Float) = 0.0

        [SubEnum(Base, UnityEngine.Rendering.BlendMode)]
        _SrcBlendAlpha("Alpha 源混合", Float) = 1.0

        [SubEnum(Base, UnityEngine.Rendering.BlendMode)]
        _DstBlendAlpha("Alpha 目标混合", Float) = 0.0

        [SubToggle(Base, _)]
        _ZWrite("写入深度", Float) = 1.0

        [SubToggle(Base, _)]
        _BlendModePreserveSpecular("透明保留高光", Float) = 1.0

        [SubToggle(Base, _)]
        _AlphaToMask("Alpha To Coverage", Float) = 0.0

        [Sub(Base)]
        _QueueOffset("渲染队列偏移", Float) = 0.0

        [Main(MainColor, _, off, off)] _FoldoutMainColor("主颜色", Float) = 0

        [Tex(MainColor, _BaseColor)] [MainTexture]
        _BaseMap("基础贴图", 2D) = "white" {}

        [HideInInspector] [MainColor]
        _BaseColor("基础颜色", Color) = (1,1,1,1)

        [Main(MetallicSpecular, _, off, off)] _FoldoutMetallicSpecular("金属与光滑", Float) = 0

        [SubToggle(MetallicSpecular, _METALLICSPECGLOSSMAP)]
        _UseMetallicSpecGlossMap("使用金属/高光贴图", Float) = 0.0

        [Tex(MetallicSpecular)] [ShowIf(_WorkflowMode, Equal, 1)]
        [ShowIf(_UseMetallicSpecGlossMap, Equal, 1)]
        _MetallicGlossMap("金属贴图", 2D) = "white" {}

        [Tex(MetallicSpecular)] [ShowIf(_WorkflowMode, Equal, 0)]
        [ShowIf(_UseMetallicSpecGlossMap, Equal, 1)]
        _SpecGlossMap("高光贴图", 2D) = "white" {}

        [Sub(MetallicSpecular)] [ShowIf(_WorkflowMode, Equal, 1)]
        _Metallic("金属度", Range(0.0, 1.0)) = 0.0

        [Sub(MetallicSpecular)] [ShowIf(_WorkflowMode, Equal, 0)]
        _SpecColor("高光颜色", Color) = (0.2, 0.2, 0.2)

        [Sub(MetallicSpecular)]
        _Smoothness("光滑度", Range(0.0, 1.0)) = 0.5

        [SubToggle(MetallicSpecular, _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)]
        _SmoothnessTextureChannel("光滑度取自基础贴图 Alpha", Float) = 0

        [Main(NormalHeight, _, off, off)] _FoldoutNormalHeight("法线与高度", Float) = 0

        [SubToggle(NormalHeight, _NORMALMAP)]
        _UseNormalMap("使用法线贴图", Float) = 0.0

        [Tex(NormalHeight)] [Normal] [ShowIf(_UseNormalMap, Equal, 1)]
        _BumpMap("法线贴图", 2D) = "bump" {}

        [Sub(NormalHeight)] [ShowIf(_UseNormalMap, Equal, 1)]
        _BumpScale("法线强度", Float) = 1.0

        [SubToggle(NormalHeight, _PARALLAXMAP)]
        _UseParallaxMap("使用高度贴图", Float) = 0.0

        [Tex(NormalHeight)] [ShowIf(_UseParallaxMap, Equal, 1)]
        _ParallaxMap("高度贴图", 2D) = "black" {}

        [Sub(NormalHeight)] [ShowIf(_UseParallaxMap, Equal, 1)]
        _Parallax("高度缩放", Range(0.005, 0.08)) = 0.005

        [Main(Occlusion, _, off, off)] _FoldoutOcclusion("环境遮蔽", Float) = 0

        [SubToggle(Occlusion, _OCCLUSIONMAP)]
        _UseOcclusionMap("使用环境遮蔽贴图", Float) = 0.0

        [Tex(Occlusion)] [ShowIf(_UseOcclusionMap, Equal, 1)]
        _OcclusionMap("环境遮蔽贴图", 2D) = "white" {}

        [Sub(Occlusion)] [ShowIf(_UseOcclusionMap, Equal, 1)]
        _OcclusionStrength("环境遮蔽强度", Range(0.0, 1.0)) = 1.0

        [Main(Emission, _, off, off)] _FoldoutEmission("自发光", Float) = 0

        [SubToggle(Emission, _EMISSION)]
        _UseEmission("启用自发光", Float) = 0.0

        [Tex(Emission, _EmissionColor)] [ShowIf(_UseEmission, Equal, 1)]
        _EmissionMap("自发光贴图", 2D) = "white" {}

        [HideInInspector] [HDR]
        _EmissionColor("自发光颜色", Color) = (0,0,0)

        [Main(Detail, _, off, off)] _FoldoutDetail("纹理细节", Float) = 0

        [SubToggle(Detail, _DETAIL_SCALED)]
        _UseDetailMap("使用纹理细节", Float) = 0.0

        [Tex(Detail)] [ShowIf(_UseDetailMap, Equal, 1)]
        _DetailMask("细节遮罩", 2D) = "white" {}

        [Tex(Detail)] [ShowIf(_UseDetailMap, Equal, 1)]
        _DetailAlbedoMap("细节颜色贴图", 2D) = "linearGrey" {}

        [Sub(Detail)] [ShowIf(_UseDetailMap, Equal, 1)]
        _DetailAlbedoMapScale("细节颜色强度", Range(0.0, 2.0)) = 1.0

        [Tex(Detail)] [Normal] [ShowIf(_UseDetailMap, Equal, 1)]
        _DetailNormalMap("细节法线贴图", 2D) = "bump" {}

        [Sub(Detail)] [ShowIf(_UseDetailMap, Equal, 1)]
        _DetailNormalMapScale("细节法线强度", Range(0.0, 2.0)) = 1.0

        [Main(AdvancedOptions, _, off, off)] _FoldoutAdvancedOptions("高级设置", Float) = 0

        [SubToggle(AdvancedOptions, _)]
        _SpecularHighlights("高光反射", Float) = 1.0

        [SubToggle(AdvancedOptions, _)]
        _EnvironmentReflections("环境反射", Float) = 1.0

        [SubToggle(AdvancedOptions, _SPECULARHIGHLIGHTS_OFF)]
        _SpecularHighlightsOff("禁用高光反射关键字", Float) = 0.0

        [SubToggle(AdvancedOptions, _ENVIRONMENTREFLECTIONS_OFF)]
        _EnvironmentReflectionsOff("禁用环境反射关键字", Float) = 0.0

        // SRP batching compatibility for Clear Coat (Not used in Lit)
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // -------------------------------------
            // Render State Commands
            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            //#define ST_SHADER_RUNTIME 1

            // -------------------------------------
            // Universal Pipeline keywords
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
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
                #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
                #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
                #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
                #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
                #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile_fragment _ _LIGHT_COOKIES
                #pragma multi_compile _ _LIGHT_LAYERS
                #pragma multi_compile _ _FORWARD_PLUS
                #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
                #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #endif


            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #if !defined(ST_SHADER_RUNTIME)
                #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #endif

            #include "SceneObjLit/SceneObjLitInput.hlsl"
            #include "SceneObjLit/SceneObjLitForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Universal Pipeline keywords

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "SceneObjLit/SceneObjLitInput.hlsl"
            #include "SceneObjLit/SceneObjShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "SceneObjLit/SceneObjLitInput.hlsl"
            #include "SceneObjLit/SceneObjDepthOnlyPass.hlsl"
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            // -------------------------------------
            // Render State Commands
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION

            // -------------------------------------
            // Includes
            #include "SceneObjLit/SceneObjLitInput.hlsl"
            #include "SceneObjLit/SceneObjLitMetaPass.hlsl"

            ENDHLSL
        }

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "LWGUI.LWGUI"
}
