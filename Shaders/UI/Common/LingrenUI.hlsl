// 将字体图集 UV 的行信息换算为所在字形行与通道坐标。
float2 GetFontEncodeUV(float2 uv)
{
    if (uv.y > 0.75)
    {
        return float2((uv.y - 0.75) / 0.25, 0.1);
    }
    else if (uv.y > 0.5)
    {
        return float2((uv.y - 0.5) / 0.25, 1.1);
    }
    else if (uv.y > 0.25)
    {
        return float2((uv.y - 0.25) / 0.25, 2.1);
    }
    else
    {
        return float2((uv.y - 0.0) / 0.25, 3.1);
    }
}

// 按编码 UV 从字体图集中取出对应图层的 Alpha。
fixed GetFontA(sampler2D fontTex, float4 fontEncodeUV)
{
    fixed4 colorFontLayerRGBA = tex2D(fontTex, fontEncodeUV.xz);

    if (fontEncodeUV.w > 3)
    {
        return colorFontLayerRGBA.a;
    }
    else if (fontEncodeUV.w > 2)
    {
        return colorFontLayerRGBA.b;
    }
    else if (fontEncodeUV.w > 1)
    {
        return colorFontLayerRGBA.g;
    }
    else
    {
        return colorFontLayerRGBA.r;
    }
}

// 顶点色接近黑色时输出灰度，用于 UI 灰度显示。
fixed3 GetGrayColor(half3 texColor, float3 inColor, float4 finalColor)
{
    half grayVal = Luminance(texColor) * finalColor.a;
    float inColorVal = inColor.r + inColor.g + inColor.b;
    return lerp(finalColor, half3(grayVal, grayVal, grayVal), inColorVal == 0 ? 1 : 0);
}
