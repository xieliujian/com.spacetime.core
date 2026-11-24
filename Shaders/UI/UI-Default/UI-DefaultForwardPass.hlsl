#ifndef UI_DEFAULT_FORWARD_PASS_INCLUDED
#define UI_DEFAULT_FORWARD_PASS_INCLUDED

// 顶点着色器：完成坐标变换，并预先计算矩形遮罩所需的参数。
Varyings vert(Attributes v)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
    float4 positionCS = UnityObjectToClipPos(v.positionOS);
    OUT.worldPosition = mul(UNITY_MATRIX_M, v.positionOS);
    OUT.positionCS = positionCS;

    float2 pixelSize = positionCS.w;
    pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
    float2 maskUV = (v.positionOS.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
    OUT.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
    OUT.mask = half4(v.positionOS.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

    OUT.color = v.color * _Color;
    return OUT;
}

// 片元着色器：采样主纹理叠加顶点色，依次处理矩形遮罩、圆形遮罩、Alpha 裁剪与灰度。
fixed4 frag(Varyings IN) : SV_Target
{
    half4 texColor = (tex2D(_MainTex, IN.uv) + _TextureSampleAdd);
    float4 finalColor = texColor * IN.color;

    #ifdef UNITY_UI_CLIP_RECT
    half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
    m = ApplyUIRetainSoftClip(m, float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    finalColor.a *= m.x * m.y;
    #endif
    
    #ifdef UNITY_UI_CLIP_CIRCLE
    half circleClipDiffX = IN.worldPosition.x - _CircleClipParams.x;
    half circleClipDiffZ = IN.worldPosition.y - _CircleClipParams.y;
    float circleClipDisSQ = circleClipDiffX * circleClipDiffX + circleClipDiffZ * circleClipDiffZ;
    if (circleClipDisSQ >= _CircleClipParams.z * _CircleClipParams.z)
    {
        finalColor.a = 0;
    }
    #endif

    #ifdef UNITY_UI_ALPHACLIP
    clip (finalColor.a - 0.05);
    #endif

    #if !UNITY_UI_NOALPHAMUL
    finalColor.rgb *= finalColor.a;
    #endif
    finalColor.rgb = GetGrayColor(texColor.rgb, IN.color.rgb, finalColor);

    return finalColor;
}

#endif
