using System;
using System.Collections.Generic;
using System.Reflection;
using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 游戏模块内核的模块集合部分，负责注册、查找并遍历全部逻辑模块。
    /// </summary>
    public sealed partial class GameWorld
    {
        /// <summary>按运行时类型精确查找模块。</summary>
        /// <typeparam name="T">模块的运行时类型。</typeparam>
        /// <returns>对应模块实例；未注册时返回 null。</returns>
        public T GetModule<T>() where T : ILogicModule
        {
            ILogicModule module;
            bool isFound = m_ModuleByType.TryGetValue(typeof(T), out module);
            if (!isFound)
                return null;

            T result = module as T;
            return result;
        }

        /// <summary>按运行时类型对象精确查找模块。</summary>
        /// <param name="type">模块的运行时类型。</param>
        /// <returns>对应模块实例；type 为 null 或未注册时返回 null。</returns>
        public ILogicModule GetModule(Type type)
        {
            if (type == null)
                return null;

            ILogicModule module;
            bool isFound = m_ModuleByType.TryGetValue(type, out module);
            if (!isFound)
                return null;

            return module;
        }

        /// <summary>按模块类型名或完整类型名查找模块。</summary>
        /// <param name="name">模块的 <see cref="Type.Name"/> 或 <see cref="Type.FullName"/>。</param>
        /// <returns>对应模块实例；name 为空或未注册时返回 null。</returns>
        public ILogicModule GetModule(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            ILogicModule module;
            bool isFound = m_ModuleByName.TryGetValue(name, out module);
            if (!isFound)
                return null;

            return module;
        }

        /// <summary>获取全部已注册模块。</summary>
        /// <returns>按注册顺序排列的模块数组，每次调用都会分配新数组。</returns>
        public ILogicModule[] GetAllModule()
        {
            ILogicModule[] modules = m_Modules.ToArray();
            return modules;
        }

        /// <summary>注册模块列表，并给每个模块注入内核。</summary>
        /// <param name="modules">基础与业务模块列表。</param>
        void AddModules(List<ILogicModule> modules)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                ILogicModule module = modules[i];
                if (module == null)
                {
                    STLogger.LogWarningF("GameWorld 模块列表第 {0} 项为 null，已跳过。", i);
                    continue;
                }

                AddModule(module);
            }
        }

        /// <summary>注册单个模块：注入内核、建立类型与名称索引，并记录需要逐帧驱动的模块。</summary>
        /// <param name="module">待注册的模块。</param>
        void AddModule(ILogicModule module)
        {
            if (m_Modules.Contains(module))
                return;

            Type moduleType = module.GetType();
            bool hasSameType = m_ModuleByType.ContainsKey(moduleType);
            if (hasSameType)
            {
                STLogger.LogWarningF("GameWorld 收到重复类型的模块 {0}，按类型查找将返回后注册的实例。", moduleType.Name);
            }

            module.SetKernel(m_Kernel);
            m_Modules.Add(module);
            m_ModuleByType[moduleType] = module;
            m_ModuleByName[moduleType.Name] = module;
            m_ModuleByName[moduleType.FullName] = module;

            bool overridesUpdate = OverridesLifecycleMethod(moduleType, "DoUpdate");
            if (overridesUpdate)
            {
                m_UpdateModules.Add(module);
            }

            bool overridesLateUpdate = OverridesLifecycleMethod(moduleType, "DoLateUpdate");
            if (overridesLateUpdate)
            {
                m_LateUpdateModules.Add(module);
            }
        }

        /// <summary>按注册顺序初始化全部模块。</summary>
        void InitModules()
        {
            for (int i = 0; i < m_Modules.Count; i++)
            {
                ILogicModule module = m_Modules[i];
                try
                {
                    module.DoInit();
                }
                catch (Exception exception)
                {
                    LogModuleException(module, "DoInit", exception);
                }
            }
        }

        /// <summary>按注册顺序驱动重写了逐帧逻辑的模块。</summary>
        void UpdateModules()
        {
            for (int i = 0; i < m_UpdateModules.Count; i++)
            {
                ILogicModule module = m_UpdateModules[i];
                try
                {
                    module.DoUpdate();
                }
                catch (Exception exception)
                {
                    LogModuleException(module, "DoUpdate", exception);
                }
            }
        }

        /// <summary>按注册顺序驱动重写了滞后帧逻辑的模块。</summary>
        void LateUpdateModules()
        {
            for (int i = 0; i < m_LateUpdateModules.Count; i++)
            {
                ILogicModule module = m_LateUpdateModules[i];
                try
                {
                    module.DoLateUpdate();
                }
                catch (Exception exception)
                {
                    LogModuleException(module, "DoLateUpdate", exception);
                }
            }
        }

        /// <summary>按初始化逆序关闭全部模块。</summary>
        void CloseModules()
        {
            for (int i = m_Modules.Count - 1; i >= 0; i--)
            {
                ILogicModule module = m_Modules[i];
                try
                {
                    module.DoClose();
                }
                catch (Exception exception)
                {
                    LogModuleException(module, "DoClose", exception);
                }
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
            AddModule(m_LaunchModule);
        }

        /// <summary>判断模块类型是否重写了指定生命周期方法；未重写时不会进入逐帧调用列表。</summary>
        /// <param name="moduleType">模块运行时类型。</param>
        /// <param name="methodName">生命周期方法名。</param>
        /// <returns>重写了该方法时返回 true。</returns>
        static bool OverridesLifecycleMethod(Type moduleType, string methodName)
        {
            MethodInfo method = moduleType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return false;

            bool isOverridden = method != method.GetBaseDefinition();
            return isOverridden;
        }

        /// <summary>记录模块生命周期抛出的异常，避免单个模块中断整帧驱动。</summary>
        /// <param name="module">抛出异常的模块。</param>
        /// <param name="methodName">抛出异常的生命周期方法名。</param>
        /// <param name="exception">捕获到的异常。</param>
        static void LogModuleException(ILogicModule module, string methodName, Exception exception)
        {
            STLogger.LogErrorF(
                "[GameWorld] 模块 {0} 的 {1} 抛出异常：{2}",
                module.GetType().Name,
                methodName,
                exception);
        }
    }
}
