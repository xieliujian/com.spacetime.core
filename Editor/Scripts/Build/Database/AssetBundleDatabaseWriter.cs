using System.Collections.Generic;
using System.Text;

namespace ST.Core
{
    /// <summary>
    /// 把客户端数据库编码为旧客户端可解析的索引文本，并在写出前校验引用与依赖环。
    /// </summary>
    public static class AssetBundleDatabaseWriter
    {
        /// <summary>
        /// 依赖遍历状态。
        /// </summary>
        enum EDependVisitState
        {
            Unvisited = 0, // 未访问
            Visiting = 1, // 访问中
            Visited = 2, // 已完成
        }

        /// <summary>
        /// 把一个数据库编码为 UTF-8 字节。
        /// </summary>
        /// <param name="database">待编码数据库。</param>
        /// <param name="lineEnding">换行策略。</param>
        /// <param name="bytes">成功时返回编码结果，失败时为 null。</param>
        /// <param name="error">失败原因。</param>
        /// <returns>编码成功返回 true。</returns>
        public static bool TryWrite(AssetBundleDatabase database, ELineEnding lineEnding,
            out byte[] bytes, out string error)
        {
            bytes = null;
            error = string.Empty;

            bool isDatabaseNull = database == null;
            if (isDatabaseNull)
            {
                error = BuildDefine.LOG_DB_NULL;
                return false;
            }

            List<AssetBundleDatabaseRecord> sortedRecords =
                new List<AssetBundleDatabaseRecord>(database.records);
            sortedRecords.Sort(CompareByName);

            Dictionary<string, int> indexByName = new Dictionary<string, int>(sortedRecords.Count);
            bool isIndexValid = BuildIndexMap(sortedRecords, indexByName, out error);
            if (!isIndexValid)
            {
                return false;
            }

            Dictionary<string, AssetBundleDatabaseRecord> recordByName =
                new Dictionary<string, AssetBundleDatabaseRecord>(sortedRecords.Count);
            for (int i = 0; i < sortedRecords.Count; ++i)
            {
                AssetBundleDatabaseRecord record = sortedRecords[i];
                recordByName.Add(record.name, record);
            }

            bool isGraphValid = ValidateDependencies(sortedRecords, indexByName, recordByName, out error);
            if (!isGraphValid)
            {
                return false;
            }

            string separator = GetLineEnding(lineEnding);
            StringBuilder builder = new StringBuilder(sortedRecords.Count * 48);
            AppendRecords(builder, sortedRecords, indexByName, separator);

            string text = builder.ToString();
            byte[] encodedBytes = Encoding.UTF8.GetBytes(text);
            bytes = encodedBytes;
            return true;
        }

        /// <summary>按名字序数排序记录。</summary>
        /// <param name="left">左侧记录。</param>
        /// <param name="right">右侧记录。</param>
        static int CompareByName(AssetBundleDatabaseRecord left, AssetBundleDatabaseRecord right)
        {
            string leftName = left.name;
            bool isLeftNameMissing = leftName == null;
            if (isLeftNameMissing)
            {
                leftName = string.Empty;
            }

            string rightName = right.name;
            bool isRightNameMissing = rightName == null;
            if (isRightNameMissing)
            {
                rightName = string.Empty;
            }

            int compareResult = string.CompareOrdinal(leftName, rightName);
            return compareResult;
        }

        /// <summary>建立名字到输出序号的映射并检查重复名。</summary>
        /// <param name="records">已排序记录。</param>
        /// <param name="indexByName">接收名字到序号的映射。</param>
        /// <param name="error">失败原因。</param>
        static bool BuildIndexMap(List<AssetBundleDatabaseRecord> records,
            Dictionary<string, int> indexByName, out string error)
        {
            error = string.Empty;

            for (int i = 0; i < records.Count; ++i)
            {
                string recordName = records[i].name;
                bool isNameMissing = string.IsNullOrEmpty(recordName);
                if (isNameMissing)
                {
                    error = BuildDefine.LOG_RECORD_NAME_EMPTY;
                    return false;
                }

                bool isDuplicate = indexByName.ContainsKey(recordName);
                if (isDuplicate)
                {
                    error = string.Format(BuildDefine.LOG_DUPLICATE_BUNDLE, recordName);
                    return false;
                }

                indexByName.Add(recordName, i);
            }

            return true;
        }

        /// <summary>校验依赖引用、Redirect 切片与依赖环。</summary>
        /// <param name="records">已排序记录。</param>
        /// <param name="indexByName">名字到序号映射。</param>
        /// <param name="recordByName">名字到记录映射。</param>
        /// <param name="error">失败原因。</param>
        static bool ValidateDependencies(List<AssetBundleDatabaseRecord> records,
            Dictionary<string, int> indexByName,
            Dictionary<string, AssetBundleDatabaseRecord> recordByName,
            out string error)
        {
            error = string.Empty;

            for (int i = 0; i < records.Count; ++i)
            {
                AssetBundleDatabaseRecord record = records[i];
                bool isReferenceValid = ValidateRecordReferences(record, indexByName, out error);
                if (!isReferenceValid)
                {
                    return false;
                }
            }

            Dictionary<string, EDependVisitState> states =
                new Dictionary<string, EDependVisitState>(records.Count);
            for (int i = 0; i < records.Count; ++i)
            {
                string recordName = records[i].name;
                bool isAcyclic = VisitDependencies(recordName, recordByName, states, out error);
                if (!isAcyclic)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>校验单条记录的依赖与 Redirect 引用。</summary>
        /// <param name="record">待校验记录。</param>
        /// <param name="indexByName">名字到序号映射。</param>
        /// <param name="error">失败原因。</param>
        static bool ValidateRecordReferences(AssetBundleDatabaseRecord record,
            Dictionary<string, int> indexByName, out string error)
        {
            error = string.Empty;

            List<string> dependencies = record.dependencies;
            bool hasDependencies = dependencies != null;
            if (hasDependencies)
            {
                for (int i = 0; i < dependencies.Count; ++i)
                {
                    string dependencyName = dependencies[i];
                    bool isDependencyKnown = indexByName.ContainsKey(dependencyName);
                    if (!isDependencyKnown)
                    {
                        error = string.Format(
                            BuildDefine.LOG_MISSING_DEPEND, record.name, dependencyName);
                        return false;
                    }
                }
            }

            AssetBundleRedirectSlice redirect = record.redirect;
            bool hasRedirect = redirect != null;
            if (!hasRedirect)
            {
                return true;
            }

            bool isContainerMissing = string.IsNullOrEmpty(redirect.containerName);
            bool isContainerKnown = !isContainerMissing && indexByName.ContainsKey(redirect.containerName);
            if (!isContainerKnown)
            {
                error = string.Format(
                    BuildDefine.LOG_MISSING_REDIRECT, record.name, redirect.containerName);
                return false;
            }

            bool isOffsetValid = redirect.offset >= 0;
            bool isLengthValid = redirect.length > 0;
            bool isSliceValid = isOffsetValid && isLengthValid;
            if (!isSliceValid)
            {
                error = string.Format(BuildDefine.LOG_INVALID_REDIRECT, record.name);
                return false;
            }

            return true;
        }

        /// <summary>深度优先检查一条记录的依赖是否成环。</summary>
        /// <param name="recordName">当前记录名。</param>
        /// <param name="recordByName">名字到记录映射。</param>
        /// <param name="states">遍历状态。</param>
        /// <param name="error">失败原因。</param>
        static bool VisitDependencies(string recordName,
            Dictionary<string, AssetBundleDatabaseRecord> recordByName,
            Dictionary<string, EDependVisitState> states, out string error)
        {
            error = string.Empty;

            EDependVisitState state = EDependVisitState.Unvisited;
            bool hasState = states.TryGetValue(recordName, out state);
            if (hasState)
            {
                bool isVisiting = state == EDependVisitState.Visiting;
                if (isVisiting)
                {
                    error = string.Format(BuildDefine.LOG_DEPEND_CYCLE, recordName);
                    return false;
                }

                return true;
            }

            states[recordName] = EDependVisitState.Visiting;
            AssetBundleDatabaseRecord record = recordByName[recordName];
            List<string> dependencies = record.dependencies;
            bool hasDependencies = dependencies != null;
            if (hasDependencies)
            {
                for (int i = 0; i < dependencies.Count; ++i)
                {
                    bool isAcyclic = VisitDependencies(
                        dependencies[i], recordByName, states, out error);
                    if (!isAcyclic)
                    {
                        return false;
                    }
                }
            }

            states[recordName] = EDependVisitState.Visited;
            return true;
        }

        /// <summary>把排序后的记录追加到文本构建器。</summary>
        /// <param name="builder">文本构建器。</param>
        /// <param name="records">已排序记录。</param>
        /// <param name="indexByName">名字到序号映射。</param>
        /// <param name="separator">换行字符串。</param>
        static void AppendRecords(StringBuilder builder, List<AssetBundleDatabaseRecord> records,
            Dictionary<string, int> indexByName, string separator)
        {
            for (int i = 0; i < records.Count; ++i)
            {
                AssetBundleDatabaseRecord record = records[i];
                builder.Append(record.name);
                builder.Append('\t');
                builder.Append(i);
                builder.Append(separator);

                AppendDependencyLine(builder, record, indexByName, separator);
                AppendRedirectLine(builder, record, indexByName, separator);
            }
        }

        /// <summary>追加依赖行，空依赖不输出整行。</summary>
        /// <param name="builder">文本构建器。</param>
        /// <param name="record">当前记录。</param>
        /// <param name="indexByName">名字到序号映射。</param>
        /// <param name="separator">换行字符串。</param>
        static void AppendDependencyLine(StringBuilder builder, AssetBundleDatabaseRecord record,
            Dictionary<string, int> indexByName, string separator)
        {
            List<string> dependencies = record.dependencies;
            bool hasDependencies = dependencies != null && dependencies.Count > 0;
            if (!hasDependencies)
            {
                return;
            }

            builder.Append(BuildDefine.DEPEND_LINE_PREFIX);
            for (int i = 0; i < dependencies.Count; ++i)
            {
                if (i > 0)
                {
                    builder.Append('\t');
                }

                int dependencyIndex = indexByName[dependencies[i]];
                builder.Append(dependencyIndex);
            }

            builder.Append(separator);
        }

        /// <summary>追加 Redirect 行，无切片时不输出整行。</summary>
        /// <param name="builder">文本构建器。</param>
        /// <param name="record">当前记录。</param>
        /// <param name="indexByName">名字到序号映射。</param>
        /// <param name="separator">换行字符串。</param>
        static void AppendRedirectLine(StringBuilder builder, AssetBundleDatabaseRecord record,
            Dictionary<string, int> indexByName, string separator)
        {
            AssetBundleRedirectSlice redirect = record.redirect;
            bool hasRedirect = redirect != null;
            if (!hasRedirect)
            {
                return;
            }

            int containerIndex = indexByName[redirect.containerName];
            builder.Append(BuildDefine.REDIRECT_LINE_PREFIX);
            builder.Append(containerIndex);
            builder.Append('\t');
            builder.Append(redirect.offset);
            builder.Append('\t');
            builder.Append(redirect.length);
            builder.Append(separator);
        }

        /// <summary>把换行枚举转换为字符串。</summary>
        /// <param name="lineEnding">换行策略。</param>
        static string GetLineEnding(ELineEnding lineEnding)
        {
            string separator = BuildDefine.LINE_ENDING_LF;
            bool isCrlf = lineEnding == ELineEnding.Crlf;
            if (isCrlf)
            {
                separator = BuildDefine.LINE_ENDING_CRLF;
            }

            return separator;
        }
    }
}
