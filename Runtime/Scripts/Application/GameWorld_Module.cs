using System.Collections.Generic;

namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏模块内核的模块集合部分，负责收集、查找并遍历全部管理器模块。
    /// </summary>
    public sealed partial class GameWorld
    {
        /// <summary>全部管理器模块，列表顺序即初始化与逐帧驱动的执行顺序。</summary>
        readonly List<IManager> m_Modules = new List<IManager>(CommonDefine.s_ListConst_16);

        /// <summary>追加外部模块列表，调用方后续修改不会影响内核持有的集合。</summary>
        /// <param name="modules">基础与业务模块列表。</param>
        void AddModules(List<IManager> modules)
        {
            m_Modules.AddRange(modules);
        }

        /// <summary>按列表顺序初始化全部模块。</summary>
        void InitModules()
        {
            for (int i = 0; i < m_Modules.Count; i++)
            {
                IManager module = m_Modules[i];
                if (module == null)
                {
                    continue;
                }

                module.DoInit();
            }
        }

        /// <summary>按列表顺序驱动全部模块的逐帧逻辑。</summary>
        void UpdateModules()
        {
            for (int i = 0; i < m_Modules.Count; i++)
            {
                IManager module = m_Modules[i];
                if (module == null)
                {
                    continue;
                }

                module.DoUpdate();
            }
        }

        /// <summary>按列表顺序驱动全部模块的滞后帧逻辑。</summary>
        void LateUpdateModules()
        {
            for (int i = 0; i < m_Modules.Count; i++)
            {
                IManager module = m_Modules[i];
                if (module == null)
                {
                    continue;
                }

                module.DoLateUpdate();
            }
        }

        /// <summary>按初始化逆序关闭全部模块。</summary>
        void CloseModules()
        {
            for (int i = m_Modules.Count - 1; i >= 0; i--)
            {
                IManager module = m_Modules[i];
                if (module == null)
                {
                    continue;
                }

                module.DoClose();
            }
        }

        /// <summary>从模块列表中查找启动模块，未提供时追加一个标准启动模块。</summary>
        void CollectLaunchModule()
        {
            for (int i = 0; i < m_Modules.Count; i++)
            {
                LaunchModule launchModule = m_Modules[i] as LaunchModule;
                if (launchModule != null)
                {
                    m_LaunchModule = launchModule;
                    return;
                }
            }

            m_LaunchModule = new LaunchModule();
            m_Modules.Add(m_LaunchModule);
        }
    }
}
