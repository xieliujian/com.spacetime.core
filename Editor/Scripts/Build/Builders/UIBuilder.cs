namespace ST.Core
{
    /// <summary>
    /// UI 构建器，收集 ui/ 下的 AssetBundle。
    /// </summary>
    public class UIBuilder : ResourceBuilderBase
    {
        /// <summary>接受的前缀。</summary>
        static readonly string[] s_Prefixes =
        {
            BuildDefine.PREFIX_UI,
        };

        /// <summary>构造 UI 构建器。</summary>
        public UIBuilder()
            : base("UIBuilder", EResourceBuildScope.Ui, s_Prefixes)
        {
        }
    }
}
