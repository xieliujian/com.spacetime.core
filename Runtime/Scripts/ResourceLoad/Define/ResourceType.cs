namespace ST.Core
{
    /// <summary>
    /// 同步/异步加载时使用的逻辑资源类型；部分枚举值会映射到 CLR 类型供装饰器与加载器使用。
    /// </summary>
    public enum ResourceType : byte
    {
        /// <summary>默认，按 <c>object</c> 处理。</summary>
        Default,
        /// <summary>映射为 <see cref="string"/>。</summary>
        String,
        /// <summary>映射为 <c>byte[]</c>。</summary>
        Bytes,
        GameObject,
        Scene,
        Texture,
        Sprite,
        Material,
        Shader,
        AnimationClip,
        AudioClip,
        ScriptableObject,
    }
}
