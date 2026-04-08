using System;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 将逻辑上的 <see cref="string"/> / <c>byte[]</c> Lua 资源映射为 <see cref="TextAsset"/> 加载，加载后再还原为文本或字节。
    /// </summary>
    public class LuaAssetDecorator : IAssetDecorator
    {
        /// <inheritdoc />
        public void BeforeLoad(ref string name, ref Type type)
        {
            if (type == typeof(string) || type == typeof(byte[]))
                type = typeof(TextAsset);
        }

        /// <inheritdoc />
        public void AfterLoad(string name, Type type, ref object asset)
        {
            if (asset == null)
                return;

            if (type == typeof(string))
                asset = (asset as TextAsset).text;
            else if (type == typeof(byte[]))
                asset = (asset as TextAsset).bytes;
        }
    }
}
