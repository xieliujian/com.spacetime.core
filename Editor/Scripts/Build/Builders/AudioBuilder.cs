namespace ST.Core
{
    /// <summary>
    /// 音频构建器，收集 audio/ 下的 AssetBundle，记录写入 other 库。
    /// </summary>
    public class AudioBuilder : ResourceBuilderBase
    {
        /// <summary>接受的前缀。</summary>
        static readonly string[] s_Prefixes =
        {
            BuildDefine.PREFIX_AUDIO,
        };

        /// <summary>构造音频构建器。</summary>
        public AudioBuilder()
            : base("AudioBuilder", EResourceBuildScope.Audio, s_Prefixes)
        {
        }
    }
}
