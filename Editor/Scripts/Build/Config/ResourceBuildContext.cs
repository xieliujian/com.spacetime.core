using System.Collections.Generic;
using ST.Core.Logging;
using UnityEditor;

namespace ST.Core
{
    /// <summary>
    /// 单次资源打包的执行上下文，缓存全量 Bundle 名并收集错误与警告。
    /// </summary>
    public class ResourceBuildContext
    {
        /// <summary>本次打包选项。</summary>
        ResourceBuildOptions m_Options;

        /// <summary>阻断性错误。</summary>
        List<string> m_Errors = new List<string>(BuildDefine.MESSAGE_LIST_CAPACITY);

        /// <summary>非阻断警告。</summary>
        List<string> m_Warnings = new List<string>(BuildDefine.MESSAGE_LIST_CAPACITY);

        /// <summary>AssetDatabase 中的全量 Bundle 名缓存。</summary>
        List<string> m_AllBundleNames = new List<string>(BuildDefine.BUNDLE_NAME_LIST_CAPACITY);

        /// <summary>Bundle 名缓存是否已加载。</summary>
        bool m_IsBundleNamesLoaded;

        /// <summary>已写入的数据库数量。</summary>
        int m_DatabaseCount;

        /// <summary>使用打包选项构造上下文。</summary>
        /// <param name="options">本次打包选项，允许为空，由调用方先校验。</param>
        public ResourceBuildContext(ResourceBuildOptions options)
        {
            m_Options = options;
        }

        /// <summary>本次打包选项。</summary>
        public ResourceBuildOptions options
        {
            get { return m_Options; }
        }

        /// <summary>是否存在阻断性错误。</summary>
        public bool hasErrors
        {
            get { return m_Errors.Count > 0; }
        }

        /// <summary>错误数量。</summary>
        public int errorCount
        {
            get { return m_Errors.Count; }
        }

        /// <summary>已写入的数据库数量。</summary>
        public int databaseCount
        {
            get { return m_DatabaseCount; }
            set { m_DatabaseCount = value; }
        }

        /// <summary>获取全量 Bundle 名，第一次访问时从 AssetDatabase 读取。</summary>
        public List<string> allBundleNames
        {
            get
            {
                EnsureBundleNames();
                return m_AllBundleNames;
            }
        }

        /// <summary>清空 Bundle 名缓存，资源刷新后调用。</summary>
        public void ClearBundleNames()
        {
            m_AllBundleNames.Clear();
            m_IsBundleNamesLoaded = false;
        }

        /// <summary>记录错误并输出日志。</summary>
        /// <param name="message">错误描述。</param>
        public void AddError(string message)
        {
            m_Errors.Add(message);
            STLogger.LogError(message);
        }

        /// <summary>记录警告并输出日志。</summary>
        /// <param name="message">警告描述。</param>
        public void AddWarning(string message)
        {
            m_Warnings.Add(message);
            STLogger.LogWarning(message);
        }

        /// <summary>输出普通日志。</summary>
        /// <param name="message">日志内容。</param>
        public void LogInfo(string message)
        {
            STLogger.LogInfo(message);
        }

        /// <summary>输出带两个参数的格式化日志。</summary>
        /// <param name="format">格式模板。</param>
        /// <param name="arg0">第一个参数。</param>
        /// <param name="arg1">第二个参数。</param>
        public void LogInfoF(string format, object arg0, object arg1)
        {
            STLogger.LogInfoF(format, arg0, arg1);
        }

        /// <summary>把错误列表填入调用方容器。</summary>
        /// <param name="result">接收错误的列表，允许为空。</param>
        public void GetErrors(List<string> result)
        {
            bool isResultNull = result == null;
            if (isResultNull)
            {
                return;
            }

            result.Clear();
            for (int i = 0; i < m_Errors.Count; ++i)
            {
                result.Add(m_Errors[i]);
            }
        }

        /// <summary>读取并缓存 AssetDatabase 中的全量 Bundle 名。</summary>
        void EnsureBundleNames()
        {
            if (m_IsBundleNamesLoaded)
            {
                return;
            }

            string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < bundleNames.Length; ++i)
            {
                m_AllBundleNames.Add(bundleNames[i]);
            }

            m_IsBundleNamesLoaded = true;
        }
    }
}
