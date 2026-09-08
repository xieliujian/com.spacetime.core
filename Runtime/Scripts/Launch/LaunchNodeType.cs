namespace ST.Core.Launch
{
    /// <summary>
    /// 标准启动节点类型。
    /// </summary>
    public enum LaunchNodeType : byte
    {
        /// <summary>无效类型。</summary>
        None,
        /// <summary>等待一帧。</summary>
        WaitFrame,
        /// <summary>初始化应用。</summary>
        AppInit,
        /// <summary>打开启动界面。</summary>
        OpenLaunchShell,
        /// <summary>加载首屏全局资源。</summary>
        SplashGlobalResLoad,
        /// <summary>下载服务器配置。</summary>
        DownloadServerConfig,
        /// <summary>加载数据表。</summary>
        TableLoad,
        /// <summary>检查服务器配置下载结果。</summary>
        CheckDownloadServerConfigSuccess,
        /// <summary>加载登录前全局资源。</summary>
        BeforeLoginGlobalResLoad,
        /// <summary>下载服务器动态配置。</summary>
        DownloadServerDynamic,
        /// <summary>检查动态配置下载结果。</summary>
        CheckDownloadServerDynamicSuccess,
        /// <summary>初始化 SDK。</summary>
        SDKInit,
        /// <summary>检查 SDK 初始化结果。</summary>
        SDKInitCheck,
        /// <summary>检查版本。</summary>
        CheckVersion,
        /// <summary>执行资源更新。</summary>
        Update,
        /// <summary>重新加载资源。</summary>
        ReloadRes,
        /// <summary>预编译着色器。</summary>
        CompileShader,
        /// <summary>检查分包更新。</summary>
        CheckSubpackUpdate,
        /// <summary>执行启动阶段垃圾回收。</summary>
        LaunchGC,
        /// <summary>打开登录界面。</summary>
        OpenLoginPanel,
        /// <summary>执行 SDK 登录。</summary>
        SDKLogin,
        /// <summary>结束启动流程。</summary>
        End,
        /// <summary>类型数量边界。</summary>
        Max,
    }
}
