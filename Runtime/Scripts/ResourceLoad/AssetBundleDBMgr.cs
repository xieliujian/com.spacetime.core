using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// AB 信息，包含名称和依赖列表。
    /// </summary>
    public struct AssetBundleInfo
    {
        public string name;
        public string[] depends;
    }

    /// <summary>
    /// AssetBundle 文本数据库管理器：解析 assetbundledb.txt，替代 Unity 原生 AssetBundleManifest。
    /// 文件格式：
    /// <code>
    /// AB名称\t序号ID
    /// \tDepend:依赖ID1\t依赖ID2\t...
    /// </code>
    /// </summary>
    public class AssetBundleDBMgr
    {
        Dictionary<string, AssetBundleInfo> m_ABInfoDict = new Dictionary<string, AssetBundleInfo>(4096);
        List<string> m_TempStrList = new List<string>();
        List<string> m_TempABNameList = new List<string>();

        /// <summary>初始化，解析指定路径的数据库文件。</summary>
        /// <param name="dbFilePath">assetbundledb.txt 的完整磁盘路径。</param>
        public void Init(string dbFilePath)
        {
            m_ABInfoDict.Clear();
            ParseSingleFile(dbFilePath);
        }

        /// <summary>初始化，解析多个数据库文件。</summary>
        /// <param name="dbFilePaths">多个 assetbundledb_*.txt 的完整磁盘路径。</param>
        public void Init(string[] dbFilePaths)
        {
            m_ABInfoDict.Clear();
            for (int i = 0; i < dbFilePaths.Length; ++i)
            {
                ParseSingleFile(dbFilePaths[i]);
            }
        }

        /// <summary>获取指定 AB 的依赖列表。</summary>
        public string[] GetAssetBundleDepends(string abName)
        {
            AssetBundleInfo info;
            if (m_ABInfoDict.TryGetValue(abName, out info))
            {
                return info.depends;
            }
            return null;
        }

        /// <summary>获取所有已注册的 AB 名称。</summary>
        public List<string> GetAllAssetBundleNames()
        {
            var names = new List<string>(m_ABInfoDict.Count);
            foreach (var kv in m_ABInfoDict)
            {
                names.Add(kv.Key);
            }
            return names;
        }

        void ParseSingleFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"AssetBundleDBMgr can not load {filePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(filePath);
            string str = Encoding.UTF8.GetString(data);
            m_TempABNameList.Clear();

            int pos = 0;
            string abName = string.Empty;

            while (pos < str.Length)
            {
                if (str[pos] == '\r' || str[pos] == '\n')
                {
                    ++pos;
                    continue;
                }
                else if (str[pos] == '\t')
                {
                    if (IsABAttrHeader(str, pos, "\tDepend:", ref pos))
                    {
                        AssetBundleInfo info = m_ABInfoDict[abName];
                        info.depends = ReadABAttrStringArray(str, ref pos);
                        m_ABInfoDict[abName] = info;
                    }
                    else
                    {
                        // 跳过未识别的属性行
                        SkipLine(str, ref pos);
                    }
                    continue;
                }
                else
                {
                    abName = ReadABName(str, ref pos);
                    ++pos;
                    ReadABAttrInt(str, ref pos);
                    AssetBundleInfo info = new AssetBundleInfo();
                    info.name = abName;
                    info.depends = null;
                    m_ABInfoDict[abName] = info;
                    m_TempABNameList.Add(abName);
                    continue;
                }
            }

            // 将依赖中的整数 ID 还原为实际 AB 名称
            for (int i = 0; i < m_TempABNameList.Count; ++i)
            {
                string name = m_TempABNameList[i];
                AssetBundleInfo info = m_ABInfoDict[name];

                if (info.depends != null)
                {
                    for (int j = 0; j < info.depends.Length; ++j)
                    {
                        info.depends[j] = GetAssetBundleName(m_TempABNameList, info.depends[j]);
                    }
                }

                m_ABInfoDict[name] = info;
            }
        }

        bool IsABAttrHeader(string str, int startPos, string header, ref int pos)
        {
            if (startPos + header.Length >= str.Length)
                return false;

            for (int i = 0; i < header.Length; ++i)
            {
                if (str[startPos + i] != header[i])
                    return false;
            }

            pos = startPos + header.Length;
            return true;
        }

        string ReadABName(string str, ref int pos)
        {
            int startPos = pos;
            for (int i = startPos; i < str.Length; ++i)
            {
                if (str[i] == '\r' || str[i] == '\n' || str[i] == '\t')
                {
                    pos = i;
                    return str.Substring(startPos, i - startPos);
                }
            }
            pos = str.Length;
            return string.Empty;
        }

        int ReadABAttrInt(string str, ref int pos)
        {
            int startPos = pos;
            for (int i = startPos; i < str.Length; ++i)
            {
                if (str[i] == '\r' || str[i] == '\n' || str[i] == '\t')
                {
                    pos = i;
                    string intStr = str.Substring(startPos, i - startPos);
                    int result;
                    if (int.TryParse(intStr, out result))
                        return result;
                    return 0;
                }
            }
            pos = str.Length;
            return 0;
        }

        string[] ReadABAttrStringArray(string str, ref int pos)
        {
            if (str[pos] == '\r' || str[pos] == '\n')
                return null;

            int lastItemPos = pos;
            m_TempStrList.Clear();

            for (int i = pos; i < str.Length; ++i)
            {
                if (str[i] == '\r' || str[i] == '\n')
                {
                    if (i != lastItemPos)
                        m_TempStrList.Add(str.Substring(lastItemPos, i - lastItemPos));
                    pos = i;
                    return m_TempStrList.ToArray();
                }
                else if (str[i] == '\t')
                {
                    if (i != lastItemPos)
                        m_TempStrList.Add(str.Substring(lastItemPos, i - lastItemPos));
                    lastItemPos = i + 1;
                }
            }

            pos = str.Length;
            return null;
        }

        void SkipLine(string str, ref int pos)
        {
            for (int i = pos; i < str.Length; ++i)
            {
                if (str[i] == '\r' || str[i] == '\n')
                {
                    pos = i;
                    return;
                }
            }
            pos = str.Length;
        }

        string GetAssetBundleName(List<string> abNameList, string indexStr)
        {
            int index;
            if (!int.TryParse(indexStr, out index))
                return indexStr;

            if (index < 0 || index >= abNameList.Count)
            {
                Debug.LogError($"AssetBundleDBMgr GetAssetBundleName index out of range: {indexStr}");
                return string.Empty;
            }

            return abNameList[index];
        }
    }
}