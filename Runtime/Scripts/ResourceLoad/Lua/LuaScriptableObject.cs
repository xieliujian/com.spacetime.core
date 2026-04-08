using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 将多份 Lua 字节码打包进单个 <see cref="ScriptableObject"/> 资产，便于随 AssetBundle 分发并由运行时按路径查找。
    /// </summary>
    public class LuaScriptableObject : ScriptableObject
    {
        /// <summary>单条 Lua 资源：相对路径与编译后字节。</summary>
        [Serializable]
        public class LuaEntry
        {
            /// <summary>相对 Lua 根目录的路径键。</summary>
            public string path;
            /// <summary>luac 等工具产出的字节码。</summary>
            public byte[] data;
        }

        /// <summary>所有已打包条目列表。</summary>
        public List<LuaEntry> m_lualist = new List<LuaEntry>();

        /// <summary>按键查找字节数据，未找到返回 <c>null</c>。</summary>
        public byte[] FindEntity(string path)
        {
            foreach (var entity in m_lualist)
            {
                if (entity == null)
                    continue;

                if (entity.path == path)
                    return entity.data;
            }

            return null;
        }

        /// <summary>追加一条 Lua 条目。</summary>
        public void AddEntry(string path, byte[] data)
        {
            LuaEntry entity = new LuaEntry();
            entity.path = path;
            entity.data = data;
            m_lualist.Add(entity);
        }

        /// <summary>清空列表（打包前重置）。</summary>
        public void Clear()
        {
            m_lualist.Clear();
        }
    }
}
