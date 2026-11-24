#ifndef LR_UI_SOFT_CLIP_INCLUDED
#define LR_UI_SOFT_CLIP_INCLUDED

// 按柔边参数平滑矩形遮罩结果，柔边为 0 时保持原有遮挡值。
float2 ApplyUIRetainSoftClip(float2 legacyMask, float2 softness)
{
    if (all(softness <= 0.0))
    {
        return legacyMask;
    }

    // 保留原有宽度和分辨率换算，仅调整开启柔边方向的透明度曲线。
    float2 retainMask = legacyMask * (2.0 - legacyMask);
    return lerp(legacyMask, retainMask, step(float2(0.0001, 0.0001), softness));
}

#endif
