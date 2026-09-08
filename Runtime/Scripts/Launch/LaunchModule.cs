using System;
using ST.Core.Logging;

namespace ST.Core.Launch
{
    /// <summary>
    /// 构建并驱动标准游戏启动节点树。
    /// </summary>
    public sealed class LaunchModule : IManager
    {
        /// <summary>标准启动根顺序节点。</summary>
        LaunchNode m_RootNode;

        /// <summary>模块当前状态。</summary>
        LaunchNodeState m_State;

        /// <summary>启动失败信息；无错误时为空字符串。</summary>
        string m_Error = string.Empty;

        /// <summary>是否已经记录根节点失败。</summary>
        bool m_IsFailureLogged;

        /// <summary>模块当前状态。</summary>
        public LaunchNodeState state { get { return m_State; } }

        /// <summary>启动失败信息；无错误时为空字符串。</summary>
        public string error { get { return m_Error; } }

        /// <summary>启动流程是否进入终态。</summary>
        public bool isDone
        {
            get
            {
                bool isTerminal = m_State == LaunchNodeState.Succeeded
                                  || m_State == LaunchNodeState.Failed
                                  || m_State == LaunchNodeState.Stopped;
                return isTerminal;
            }
        }

        /// <summary>启动流程是否成功完成。</summary>
        public bool isSucceeded { get { return m_State == LaunchNodeState.Succeeded; } }

        /// <summary>IManager 初始化入口；节点树由 Launch 创建。</summary>
        public override void DoInit() { }

        /// <summary>驱动运行中的启动节点树。</summary>
        public override void DoUpdate()
        {
            if (m_State != LaunchNodeState.Running || m_RootNode == null)
            {
                return;
            }

            m_RootNode.DoUpdate();
            SyncRootState();
        }

        /// <summary>启动树没有滞后帧任务。</summary>
        public override void DoLateUpdate() { }

        /// <summary>停止尚未完成的启动节点树。</summary>
        public override void DoClose()
        {
            if (m_State == LaunchNodeState.Ready)
            {
                m_State = LaunchNodeState.Stopped;
                return;
            }

            if (m_State != LaunchNodeState.Running)
            {
                return;
            }

            if (m_RootNode != null)
            {
                m_RootNode.Stop();
            }

            m_State = LaunchNodeState.Stopped;
        }

        /// <summary>构建并启动标准节点树；重复调用会被忽略。</summary>
        public void Launch()
        {
            if (m_State != LaunchNodeState.Ready)
            {
                return;
            }

            try
            {
                m_RootNode = CreateRootNode();
                m_State = LaunchNodeState.Running;
                m_RootNode.Start();
                SyncRootState();
            }
            catch (Exception exception)
            {
                m_Error = exception.ToString();
                m_State = LaunchNodeState.Failed;
                m_IsFailureLogged = true;
                Logger.LogException(exception);
            }
        }

        /// <summary>创建标准根节点树。</summary>
        SequenceNode CreateRootNode()
        {
            SequenceNode rootNode = new SequenceNode("LaunchNode");
            rootNode.Append(CreateNode(LaunchNodeType.WaitFrame));
            rootNode.Append(CreateNode(LaunchNodeType.AppInit));

            ParallelNode splashNode = new ParallelNode("Splash");
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
        /// 创建指定类型的具体启动节点。
        /// </summary>
        /// <param name="type">有效的标准启动节点类型。</param>
        /// <returns>与类型对应的新节点。</returns>
        LaunchNode CreateNode(LaunchNodeType type)
        {
            switch (type)
            {
                case LaunchNodeType.WaitFrame:
                    return new WaitFrameNode();
                case LaunchNodeType.AppInit:
                    return new AppInitNode();
                case LaunchNodeType.OpenLaunchShell:
                    return new OpenLaunchShellNode();
                case LaunchNodeType.SplashGlobalResLoad:
                    return new SplashGlobalResLoadNode();
                case LaunchNodeType.DownloadServerConfig:
                    return new DownloadServerConfigNode();
                case LaunchNodeType.TableLoad:
                    return new TableLoadNode();
                case LaunchNodeType.CheckDownloadServerConfigSuccess:
                    return new CheckDownloadServerConfigSuccessNode();
                case LaunchNodeType.BeforeLoginGlobalResLoad:
                    return new BeforeLoginGlobalResLoadNode();
                case LaunchNodeType.DownloadServerDynamic:
                    return new DownloadServerDynamicNode();
                case LaunchNodeType.CheckDownloadServerDynamicSuccess:
                    return new CheckDownloadServerDynamicSuccessNode();
                case LaunchNodeType.SDKInit:
                    return new SDKInitNode();
                case LaunchNodeType.SDKInitCheck:
                    return new SDKInitCheckNode();
                case LaunchNodeType.CheckVersion:
                    return new CheckVersionNode();
                case LaunchNodeType.Update:
                    return new UpdateNode();
                case LaunchNodeType.ReloadRes:
                    return new ReloadResNode();
                case LaunchNodeType.CompileShader:
                    return new CompileShaderNode();
                case LaunchNodeType.CheckSubpackUpdate:
                    return new CheckSubpackUpdateNode();
                case LaunchNodeType.LaunchGC:
                    return new LaunchGCNode();
                case LaunchNodeType.OpenLoginPanel:
                    return new OpenLoginPanelNode();
                case LaunchNodeType.SDKLogin:
                    return new SDKLoginNode();
                case LaunchNodeType.End:
                    return new EndNode();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown launch node type.");
            }
        }

        /// <summary>同步根节点终态，并只记录一次失败。</summary>
        void SyncRootState()
        {
            if (m_RootNode == null || !m_RootNode.isDone)
            {
                return;
            }

            m_State = m_RootNode.state;
            m_Error = m_RootNode.error;

            if (m_State != LaunchNodeState.Failed || m_IsFailureLogged)
            {
                return;
            }

            m_IsFailureLogged = true;
            Logger.LogError(m_Error);
        }
    }
}
