using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 五个客户端数据库的集合，负责路由、manifest 转换与写出。
    /// </summary>
    public class AssetBundleDatabaseSet
    {
        /// <summary>固定顺序的数据库列表。</summary>
        List<AssetBundleDatabase> m_Databases =
            new List<AssetBundleDatabase>(BuildDefine.DATABASE_COUNT);

        /// <summary>数据库名到数据库的映射。</summary>
        Dictionary<string, AssetBundleDatabase> m_DatabaseMap =
            new Dictionary<string, AssetBundleDatabase>(BuildDefine.DATABASE_COUNT);

        /// <summary>路由复用容器。</summary>
        List<string> m_TempDatabaseNames = new List<string>(BuildDefine.DATABASE_COUNT);

        /// <summary>构造五个空数据库。</summary>
        public AssetBundleDatabaseSet()
        {
            AddDatabase(new AssetBundleDatabase(BuildDefine.DB_SCENE));
            AddDatabase(new AssetBundleDatabase(BuildDefine.DB_UI));
            AddDatabase(new AssetBundleDatabase(BuildDefine.DB_OTHER));
            AddDatabase(new AssetBundleDatabase(BuildDefine.DB_CHARACTER));
            AddDatabase(new AssetBundleDatabase(BuildDefine.DB_PARTICLE));
        }

        /// <summary>按名获取数据库。</summary>
        /// <param name="databaseName">数据库文件名。</param>
        /// <returns>命中的数据库，未命中时返回 null。</returns>
        public AssetBundleDatabase GetDatabase(string databaseName)
        {
            bool isNameMissing = string.IsNullOrEmpty(databaseName);
            if (isNameMissing)
            {
                return null;
            }

            AssetBundleDatabase database;
            bool isFound = m_DatabaseMap.TryGetValue(databaseName, out database);
            if (!isFound)
            {
                return null;
            }

            return database;
        }

        /// <summary>把 manifest 中的每个 Bundle 转成记录并按路径路由。</summary>
        /// <param name="manifest">Unity 返回的构建清单。</param>
        /// <param name="context">本次打包上下文。</param>
        public void BuildFromManifest(AssetBundleManifest manifest, ResourceBuildContext context)
        {
            bool isManifestNull = manifest == null;
            if (isManifestNull)
            {
                context.AddError(BuildDefine.LOG_MANIFEST_NULL);
                return;
            }

            string[] bundleNames = manifest.GetAllAssetBundles();
            for (int i = 0; i < bundleNames.Length; ++i)
            {
                string bundleName = bundleNames[i];
                AssetBundleDatabaseRecord record = new AssetBundleDatabaseRecord();
                record.name = bundleName;

                string[] dependencies = manifest.GetAllDependencies(bundleName);
                bool hasDependencies = dependencies != null;
                if (hasDependencies)
                {
                    for (int j = 0; j < dependencies.Length; ++j)
                    {
                        record.dependencies.Add(dependencies[j]);
                    }
                }

                AddRecord(record, context);
            }
        }

        /// <summary>把一条记录路由到一个或多个数据库。</summary>
        /// <param name="record">待路由记录。</param>
        /// <param name="context">本次打包上下文。</param>
        public void AddRecord(AssetBundleDatabaseRecord record, ResourceBuildContext context)
        {
            bool isRecordNull = record == null;
            if (isRecordNull)
            {
                return;
            }

            string error;
            bool isRouted = AssetBundleDatabaseRouter.TryGetDatabaseNames(
                record.name, m_TempDatabaseNames, out error);
            if (!isRouted)
            {
                context.AddError(error);
                return;
            }

            for (int i = 0; i < m_TempDatabaseNames.Count; ++i)
            {
                string databaseName = m_TempDatabaseNames[i];
                AssetBundleDatabase database = m_DatabaseMap[databaseName];
                bool isDuplicate = database.Contains(record.name);
                if (isDuplicate)
                {
                    continue;
                }

                database.AddRecord(record);
            }
        }

        /// <summary>把五个数据库写到输出根目录。</summary>
        /// <param name="outputDirectory">输出根目录绝对路径。</param>
        /// <param name="lineEnding">换行策略。</param>
        /// <param name="context">本次打包上下文。</param>
        public void WriteAll(string outputDirectory, ELineEnding lineEnding, ResourceBuildContext context)
        {
            int writtenCount = 0;
            for (int i = 0; i < m_Databases.Count; ++i)
            {
                AssetBundleDatabase database = m_Databases[i];
                byte[] bytes;
                string error;
                bool isWritten = AssetBundleDatabaseWriter.TryWrite(
                    database, lineEnding, out bytes, out error);
                if (!isWritten)
                {
                    context.AddError(error);
                    continue;
                }

                string filePath = Path.Combine(outputDirectory, database.databaseName);
                File.WriteAllBytes(filePath, bytes);
                ++writtenCount;

                int recordCount = database.records.Count;
                context.LogInfoF(BuildDefine.LOG_DB_WRITTEN, filePath, recordCount);
            }

            context.databaseCount = writtenCount;
        }

        /// <summary>加入一个数据库并建立索引。</summary>
        /// <param name="database">待加入数据库。</param>
        void AddDatabase(AssetBundleDatabase database)
        {
            m_Databases.Add(database);
            m_DatabaseMap.Add(database.databaseName, database);
        }
    }
}
