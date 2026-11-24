using System;
using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>
    /// 一条 AssetBundle 在合包容器中的字节切片。
    /// </summary>
    public class AssetBundleRedirectSlice
    {
        /// <summary>容器 AssetBundle 的逻辑路径。</summary>
        public string containerName;

        /// <summary>容器内字节偏移，必须非负。</summary>
        public long offset;

        /// <summary>切片字节长度，必须为正。</summary>
        public long length;
    }

    /// <summary>
    /// 一个客户端数据库中的 AssetBundle 记录。
    /// </summary>
    public class AssetBundleDatabaseRecord
    {
        /// <summary>AssetBundle 逻辑路径，与输出文件相对路径一致。</summary>
        public string name;

        /// <summary>依赖逻辑路径，保持原顺序并允许重复。</summary>
        public List<string> dependencies = new List<string>(ResourceDefine.DEPEND_LIST_CAPACITY);

        /// <summary>可选的 Redirect 切片，为空表示独立文件。</summary>
        public AssetBundleRedirectSlice redirect;
    }

    /// <summary>
    /// 一个独立索引空间的客户端数据库。
    /// </summary>
    public class AssetBundleDatabase
    {
        /// <summary>数据库文件名。</summary>
        string m_DatabaseName;

        /// <summary>数据库记录。</summary>
        List<AssetBundleDatabaseRecord> m_Records =
            new List<AssetBundleDatabaseRecord>(ResourceDefine.BUNDLE_INFO_DICT_CAPACITY);

        /// <summary>使用数据库文件名构造空库。</summary>
        /// <param name="databaseName">非空数据库文件名。</param>
        public AssetBundleDatabase(string databaseName)
        {
            bool isNameMissing = string.IsNullOrEmpty(databaseName);
            if (isNameMissing)
            {
                throw new ArgumentException("数据库文件名不能为空。", "databaseName");
            }

            m_DatabaseName = databaseName;
        }

        /// <summary>数据库文件名。</summary>
        public string databaseName
        {
            get { return m_DatabaseName; }
        }

        /// <summary>数据库记录，编码前会被排序。</summary>
        public List<AssetBundleDatabaseRecord> records
        {
            get { return m_Records; }
        }

        /// <summary>追加一条记录。</summary>
        /// <param name="record">待追加记录，允许为空。</param>
        public void AddRecord(AssetBundleDatabaseRecord record)
        {
            bool isRecordNull = record == null;
            if (isRecordNull)
            {
                return;
            }

            m_Records.Add(record);
        }

        /// <summary>判断库内是否已有指定名称的记录。</summary>
        /// <param name="recordName">待查询的 Bundle 名。</param>
        public bool Contains(string recordName)
        {
            bool isNameMissing = string.IsNullOrEmpty(recordName);
            if (isNameMissing)
            {
                return false;
            }

            for (int i = 0; i < m_Records.Count; ++i)
            {
                string currentName = m_Records[i].name;
                bool isNameMatch = string.Equals(currentName, recordName, StringComparison.Ordinal);
                if (isNameMatch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>清空全部记录。</summary>
        public void Clear()
        {
            m_Records.Clear();
        }
    }
}
