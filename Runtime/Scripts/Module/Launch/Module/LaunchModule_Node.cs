using System;
using System.Collections.Generic;

namespace ST.Core.Launch
{
    /// <summary>
    /// 标准启动节点部分，负责按节点类型创建实例并组装标准节点树。
    /// </summary>
    public sealed partial class LaunchModule
    {
        /// <summary>启动节点创建表，按节点类型索引对应的无参构造，避免维护 switch 分支。</summary>
        static readonly Dictionary<LaunchNodeType, RFunc<LaunchNode>> s_NodeCreators =
            new Dictionary<LaunchNodeType, RFunc<LaunchNode>>
            {
                { LaunchNodeType.WaitFrame, () => new WaitFrameNode() },
                { LaunchNodeType.AppInit, () => new AppInitNode() },
                { LaunchNodeType.OpenLaunchShell, () => new OpenLaunchShellNode() },
                { LaunchNodeType.SplashGlobalResLoad, () => new SplashGlobalResLoadNode() },
                { LaunchNodeType.DownloadServerConfig, () => new DownloadServerConfigNode() },
                { LaunchNodeType.TableLoad, () => new TableLoadNode() },
                { LaunchNodeType.CheckDownloadServerConfigSuccess, () => new CheckDownloadServerConfigSuccessNode() },
                { LaunchNodeType.BeforeLoginGlobalResLoad, () => new BeforeLoginGlobalResLoadNode() },
                { LaunchNodeType.DownloadServerDynamic, () => new DownloadServerDynamicNode() },
                { LaunchNodeType.CheckDownloadServerDynamicSuccess, () => new CheckDownloadServerDynamicSuccessNode() },
                { LaunchNodeType.SDKInit, () => new SDKInitNode() },
                { LaunchNodeType.SDKInitCheck, () => new SDKInitCheckNode() },
                { LaunchNodeType.CheckVersion, () => new CheckVersionNode() },
                { LaunchNodeType.Update, () => new UpdateNode() },
                { LaunchNodeType.ReloadRes, () => new ReloadResNode() },
                { LaunchNodeType.CompileShader, () => new CompileShaderNode() },
                { LaunchNodeType.CheckSubpackUpdate, () => new CheckSubpackUpdateNode() },
                { LaunchNodeType.LaunchGC, () => new LaunchGCNode() },
                { LaunchNodeType.OpenLoginPanel, () => new OpenLoginPanelNode() },
                { LaunchNodeType.SDKLogin, () => new SDKLoginNode() },
                { LaunchNodeType.End, () => new EndNode() },
            };

        /// <summary>创建标准根节点树；首屏阶段并行执行，其余节点按顺序串联。</summary>
        SequenceNode CreateRootNode()
        {
            SequenceNode rootNode = new SequenceNode(LaunchDefine.k_LaunchRoot);
            rootNode.Append(CreateNode(LaunchNodeType.WaitFrame));
            rootNode.Append(CreateNode(LaunchNodeType.AppInit));

            ParallelNode splashNode = new ParallelNode(LaunchDefine.k_Splash);
            splashNode.Append(CreateNode(LaunchNodeType.OpenLaunchShell));
            splashNode.Append(CreateNode(LaunchNodeType.SplashGlobalResLoad));
            splashNode.Append(CreateNode(LaunchNodeType.DownloadServerConfig));
            splashNode.Append(CreateNode(LaunchNodeType.TableLoad));
            rootNode.Append(splashNode);

            rootNode.Append(CreateNode(LaunchNodeType.CheckDownloadServerConfigSuccess));
            rootNode.Append(CreateNode(LaunchNodeType.BeforeLoginGlobalResLoad));
            rootNode.Append(CreateNode(LaunchNodeType.DownloadServerDynamic));
            rootNode.Append(CreateNode(LaunchNodeType.CheckDownloadServerDynamicSuccess));
            rootNode.Append(CreateNode(LaunchNodeType.SDKInit));
            rootNode.Append(CreateNode(LaunchNodeType.SDKInitCheck));
            rootNode.Append(CreateNode(LaunchNodeType.CheckVersion));
            rootNode.Append(CreateNode(LaunchNodeType.Update));
            rootNode.Append(CreateNode(LaunchNodeType.ReloadRes));
            rootNode.Append(CreateNode(LaunchNodeType.CompileShader));
            rootNode.Append(CreateNode(LaunchNodeType.CheckSubpackUpdate));
            rootNode.Append(CreateNode(LaunchNodeType.LaunchGC));
            rootNode.Append(CreateNode(LaunchNodeType.OpenLoginPanel));
            rootNode.Append(CreateNode(LaunchNodeType.SDKLogin));
            rootNode.Append(CreateNode(LaunchNodeType.End));
            return rootNode;
        }

        /// <summary>
        /// 按节点类型创建新的启动节点；未登记的类型视为非法参数。
        /// </summary>
        /// <param name="type">有效的标准启动节点类型。</param>
        /// <returns>与类型对应的新节点实例。</returns>
        LaunchNode CreateNode(LaunchNodeType type)
        {
            RFunc<LaunchNode> creator;
            bool hasCreator = s_NodeCreators.TryGetValue(type, out creator);

            if (!hasCreator)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown launch node type.");
            }

            LaunchNode node = creator();
            return node;
        }
    }
}
