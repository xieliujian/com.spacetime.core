namespace ST.Core
{
    /// <summary>
    /// 剧情构建器，收集 story/ 下的 AssetBundle，记录写入场景库。
    /// </summary>
    public class StoryBuilder : ResourceBuilderBase
    {
        /// <summary>接受的前缀。</summary>
        static readonly string[] s_Prefixes =
        {
            BuildDefine.PREFIX_STORY,
        };

        /// <summary>构造剧情构建器。</summary>
        public StoryBuilder()
            : base("StoryBuilder", EResourceBuildScope.Story, s_Prefixes)
        {
        }
    }
}
