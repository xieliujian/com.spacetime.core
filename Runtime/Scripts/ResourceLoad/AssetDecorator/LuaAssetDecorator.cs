using System;
using UnityEngine;

namespace ST.Core
{
    public class LuaAssetDecorator : IAssetDecorator
    {
        public void BeforeLoad(ref string name, ref Type type)
        {
            if (type == typeof(string) || type == typeof(byte[]))
                type = typeof(TextAsset);
        }

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
