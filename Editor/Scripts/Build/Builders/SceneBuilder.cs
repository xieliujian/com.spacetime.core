namespace ST.Core
{
    /// <summary>
    /// 场景构建器，收集 scene/ 与全局资源目录下的 AssetBundle。
    /// </summary>
    public class SceneBuilder : ResourceBuilderBase
    {
        /// <summary>接受的前缀。</summary>
        static readonly string[] s_Prefixes =
        {
            BuildDefine.PREFIX_SCENE,
            BuildDefine.PREFIX_GLOBAL,
        };

        /// <summary>构造场景构建器。</summary>
        public SceneBuilder()
            : base("SceneBuilder", EResourceBuildScope.Scene, s_Prefixes)
        {
        }
    }
}
