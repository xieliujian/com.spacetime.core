using System.Collections.Generic;
using ST.Core.Logging;
using ST.Core.Network;
using ST.Core.UI;

namespace ST.Core.Launch
{
    /// <summary>
    /// 启动场景组件的模块组装部分，负责创建基础模块并提供业务模块扩展点。
    /// </summary>
    public abstract partial class GameLauncher
    {
        /// <summary>创建资源加载配置，供基础资源模块与 UI 模块使用。</summary>
        /// <returns>业务项目提供的资源配置；不可为 null。</returns>
        protected abstract IResourceConfig CreateResourceConfig();

        /// <summary>追加业务模块，业务模块会排在基础模块之后初始化并逐帧驱动。</summary>
        /// <param name="modules">按顺序容纳全部管理器模块的列表。</param>
        protected virtual void CollectBusinessModules(List<ILogicModule> modules)
        {
        }

        /// <summary>按“基础模块 → 业务模块”的顺序组装模块列表。</summary>
        /// <returns>交由 GameWorld 初始化与驱动的模块列表。</returns>
        List<ILogicModule> CreateModules()
        {
            List<ILogicModule> modules = new List<ILogicModule>(CommonDefine.s_ListConst_16);
            CollectBaseModules(modules);
            CollectBusinessModules(modules);

            return modules;
        }

        /// <summary>创建框架基础模块，顺序固定为异步任务、资源、UI、消息分发、网络、启动流程。</summary>
        /// <param name="modules">按顺序容纳全部管理器模块的列表。</param>
        void CollectBaseModules(List<ILogicModule> modules)
        {
            IResourceConfig resourceConfig = CreateResourceConfig();
            if (resourceConfig == null)
            {
                STLogger.LogError("GameLauncher.CreateResourceConfig 返回 null，基础模块创建被跳过。");
                return;
            }

            AsyncTaskManager asyncTaskManager = new AsyncTaskManager();
            modules.Add(asyncTaskManager);

            ResourceLoad resourceLoad = new ResourceLoad();
            resourceLoad.SetConfig(resourceConfig);
            modules.Add(resourceLoad);

            UIRoot uiRoot = ResolveUIRoot();
            UIManager uiManager = new UIManager();
            uiManager.Setup(resourceLoad, uiRoot);
            modules.Add(uiManager);

            MsgDispatcher msgDispatcher = new MsgDispatcher();
            modules.Add(msgDispatcher);

            NetManager netManager = new NetManager();
            modules.Add(netManager);

            LaunchModule launchModule = new LaunchModule();
            modules.Add(launchModule);
        }

        /// <summary>解析 UI 根节点；默认从场景查找 UIRoot，派生类可重写以使用自定义来源。</summary>
        /// <returns>场景中的 <see cref="UIRoot"/>；未找到时返回 null。</returns>
        protected virtual UIRoot ResolveUIRoot()
        {
            UIRoot uiRoot = FindObjectOfType<UIRoot>();
            if (uiRoot == null)
            {
                STLogger.LogWarning("GameLauncher 未找到 UIRoot，UIManager 打开面板时将会失败。");
            }

            return uiRoot;
        }
    }
}
