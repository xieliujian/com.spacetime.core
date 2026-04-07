using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    public class LuaScriptableObject : ScriptableObject
    {
        [Serializable]
        public class LuaEntry
        {
            public string path;
            public byte[] data;
        }

        public List<LuaEntry> m_lualist = new List<LuaEntry>();

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

        public void AddEntry(string path, byte[] data)
        {
            LuaEntry entity = new LuaEntry();
            entity.path = path;
            entity.data = data;
            m_lualist.Add(entity);
        }

        public void Clear()
        {
            m_lualist.Clear();
        }
    }
}
