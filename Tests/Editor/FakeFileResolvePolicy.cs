namespace ST.Core.Tests
{
    /// <summary>测试用的外存清单判定，按构造时给定的结果返回。</summary>
    public sealed class FakeFileResolvePolicy : IFileResolvePolicy
    {
        /// <summary>是否把清单当作唯一判定依据。</summary>
        public bool isAuthoritative;

        /// <summary>清单中唯一存在的相对路径。</summary>
        public string externalRelativePath;

        /// <summary>直接返回构造时给定的权威标志。</summary>
        /// <returns><see cref="isAuthoritative"/> 的值。</returns>
        public bool IsManifestAuthoritative()
        {
            return isAuthoritative;
        }

        /// <summary>只有路径与 <see cref="externalRelativePath"/> 相同时视为命中。</summary>
        /// <param name="relativePath">被查询的相对路径。</param>
        /// <returns><c>true</c> 表示命中。</returns>
        public bool ExistInExternal(string relativePath)
        {
            bool isExist = relativePath == externalRelativePath;
            return isExist;
        }
    }
}

