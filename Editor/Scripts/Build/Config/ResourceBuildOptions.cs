using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>
    /// 一次资源打包的输入参数，包含平台、输出目录、类别、换行策略与自定义构建器。
    /// </summary>
    public class ResourceBuildOptions
    {
        /// <summary>参与打包的资源类别。</summary>
        List<EResourceBuildScope> m_Scopes = new List<EResourceBuildScope>(CommonDefine.s_ListConst_16);

        /// <summary>调用方追加的自定义构建器。</summary>
        List<ResourceBuilderBase> m_Builders = new List<ResourceBuilderBase>(CommonDefine.s_ListConst_16);

        /// <summary>目标平台。</summary>
        EResourceBuildPlatform m_Platform = EResourceBuildPlatform.Windows;

        /// <summary>输出根目录，使用绝对路径。</summary>
        string m_OutputRootPath = string.Empty;

        /// <summary>打包前是否清空输出根目录。</summary>
        bool m_IsCleanOutput;

        /// <summary>是否输出 builds.txt 构建清单。</summary>
        bool m_WriteBuildList = true;

        /// <summary>数据库换行策略。</summary>
        ELineEnding m_LineEnding = ELineEnding.Lf;

        /// <summary>是否把 Unity 当前构建目标切换到目标平台。</summary>
        bool m_IsSwitchBuildTarget = true;

        /// <summary>
        /// 构造默认选项：启用全部类别，输出到 BuildRes 根目录。
        /// </summary>
        public ResourceBuildOptions()
        {
            m_Scopes.Add(EResourceBuildScope.Scene);
            m_Scopes.Add(EResourceBuildScope.Ui);
            m_Scopes.Add(EResourceBuildScope.Other);
            m_Scopes.Add(EResourceBuildScope.Character);
            m_Scopes.Add(EResourceBuildScope.Particle);
            m_Scopes.Add(EResourceBuildScope.Story);
            m_Scopes.Add(EResourceBuildScope.Audio);
            m_Scopes.Add(EResourceBuildScope.Shader);

            string streamingRootPath = FilePath.streamingAssetsPath;
            m_OutputRootPath = streamingRootPath.TrimEnd('/', '\\');
        }

        /// <summary>目标平台。</summary>
        public EResourceBuildPlatform platform
        {
            get { return m_Platform; }
            set { m_Platform = value; }
        }

        /// <summary>输出根目录，使用绝对路径。</summary>
        public string outputRootPath
        {
            get { return m_OutputRootPath; }
            set { m_OutputRootPath = value; }
        }

        /// <summary>打包前是否清空输出根目录。</summary>
        public bool isCleanOutput
        {
            get { return m_IsCleanOutput; }
            set { m_IsCleanOutput = value; }
        }

        /// <summary>是否输出 builds.txt 构建清单。</summary>
        public bool writeBuildList
        {
            get { return m_WriteBuildList; }
            set { m_WriteBuildList = value; }
        }

        /// <summary>数据库换行策略。</summary>
        public ELineEnding lineEnding
        {
            get { return m_LineEnding; }
            set { m_LineEnding = value; }
        }

        /// <summary>是否切换 Unity 当前构建目标。</summary>
        public bool isSwitchBuildTarget
        {
            get { return m_IsSwitchBuildTarget; }
            set { m_IsSwitchBuildTarget = value; }
        }

        /// <summary>加入一个资源类别，重复加入会被忽略。</summary>
        /// <param name="scope">资源类别。</param>
        public void AddScope(EResourceBuildScope scope)
        {
            bool isScopeExists = m_Scopes.Contains(scope);
            if (isScopeExists)
            {
                return;
            }

            m_Scopes.Add(scope);
        }

        /// <summary>清空已启用的资源类别。</summary>
        public void ClearScopes()
        {
            m_Scopes.Clear();
        }

        /// <summary>判断某个资源类别是否参与本次打包。</summary>
        /// <param name="scope">资源类别。</param>
        public bool IsScopeEnabled(EResourceBuildScope scope)
        {
            bool isScopeExists = m_Scopes.Contains(scope);
            return isScopeExists;
        }

        /// <summary>追加一个自定义构建器。</summary>
        /// <param name="builder">自定义构建器，允许为空。</param>
        public void AddBuilder(ResourceBuilderBase builder)
        {
            bool isBuilderNull = builder == null;
            if (isBuilderNull)
            {
                return;
            }

            m_Builders.Add(builder);
        }

        /// <summary>把自定义构建器填入调用方容器。</summary>
        /// <param name="result">接收构建器的容器，允许为空。</param>
        public void GetBuilders(List<ResourceBuilderBase> result)
        {
            bool isResultNull = result == null;
            if (isResultNull)
            {
                return;
            }

            result.Clear();
            for (int i = 0; i < m_Builders.Count; ++i)
            {
                result.Add(m_Builders[i]);
            }
        }
    }
}
