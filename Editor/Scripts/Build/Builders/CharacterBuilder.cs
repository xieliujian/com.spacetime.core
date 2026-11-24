namespace ST.Core
{
    /// <summary>
    /// 角色构建器，收集 character/ 下的 AssetBundle。
    /// </summary>
    public class CharacterBuilder : ResourceBuilderBase
    {
        /// <summary>接受的前缀。</summary>
        static readonly string[] s_Prefixes =
        {
            BuildDefine.PREFIX_CHARACTER,
        };

        /// <summary>构造角色构建器。</summary>
        public CharacterBuilder()
            : base("CharacterBuilder", EResourceBuildScope.Character, s_Prefixes)
        {
        }
    }
}
