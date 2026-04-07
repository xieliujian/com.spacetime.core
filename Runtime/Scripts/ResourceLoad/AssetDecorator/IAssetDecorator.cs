using System;

namespace ST.Core
{
    /// <summary>资产装饰器接口，在加载前后对资产路径、类型、内容进行变换。</summary>
    public interface IAssetDecorator
    {
        /// <summary>加载前：可修改资产 key 和目标类型。</summary>
        void BeforeLoad(ref string key, ref Type type);

        /// <summary>加载后：可替换返回的资产对象。</summary>
        void AfterLoad(string key, Type type, ref object asset);
    }
}