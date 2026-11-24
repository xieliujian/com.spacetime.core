namespace ST.Core
{
    /// <summary>
    /// 特效构建器，收集 particle/ 下的 AssetBundle。
    /// </summary>
    public class ParticleBuilder : ResourceBuilderBase
    {
        /// <summary>接受的前缀。</summary>
        static readonly string[] s_Prefixes =
        {
            BuildDefine.PREFIX_PARTICLE,
        };

        /// <summary>构造特效构建器。</summary>
        public ParticleBuilder()
            : base("ParticleBuilder", EResourceBuildScope.Particle, s_Prefixes)
        {
        }
    }
}
