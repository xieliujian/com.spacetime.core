#ifndef UI_DEFAULT_INPUT_INCLUDED
#define UI_DEFAULT_INPUT_INCLUDED

#include "UnityCG.cginc"
#include "UnityUI.cginc"
#include "../Common/LingrenUI.hlsl"
#include "../Common/UISoftClip.hlsl"

// UI 顶点输入，由 CanvasRenderer 提交。
struct Attributes
{
    float4 positionOS : POSITION;
    float4 color      : COLOR;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// 顶点着色器输出，携带裁剪空间坐标、顶点色、UV、世界坐标与矩形遮罩参数。
struct Varyings
{
    float4 positionCS    : SV_POSITION;
    float4 color         : COLOR;
    float2 uv            : TEXCOORD0;
    float4 worldPosition : TEXCOORD1;
    half4  mask          : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

// 材质级常量集中在 UnityPerMaterial，SRP Batcher 要求材质属性全部位于此缓冲。
CBUFFER_START(UnityPerMaterial)
float4 _Color;
float4 _MainTex_ST;
float4 _ClipRect;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _CircleClipParams;
CBUFFER_END

// 主纹理由 UI 在绘制时绑定。
sampler2D _MainTex;

// 图集采样补偿值，由 UI 系统写入。
float4 _TextureSampleAdd;

#endif
