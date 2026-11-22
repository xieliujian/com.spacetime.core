namespace ST.Core.Launch
{
    /// <summary>
    /// 启动流程定义，集中存放节点稳定名称和节点枚举，避免定义散落在节点实现与业务代码中。
    /// </summary>
    public static class LaunchDefine
    {
        // 变量定义
        public const string k_LaunchRoot = "LaunchNode"; // 启动根节点
        public const string k_Splash = "Splash"; // 首屏分组
        public const string k_WaitFrame = "WaitFrame"; // 等待一帧
        public const string k_AppInit = "AppInit"; // 应用初始化
        public const string k_OpenLaunchShell = "OpenLaunchShell"; // 打开启动界面
        public const string k_SplashGlobalResLoad = "SplashGlobalResLoad"; // 首屏全局资源
        public const string k_DownloadServerConfig = "DownloadServerConfig"; // 下载服务器配置
        public const string k_TableLoad = "TableLoad"; // 数据表加载
        public const string k_CheckDownloadServerConfigSuccess = "CheckDownloadServerConfigSuccess"; // 检查配置下载
        public const string k_BeforeLoginGlobalResLoad = "BeforeLoginGlobalResLoad"; // 登录前全局资源
        public const string k_DownloadServerDynamic = "DownloadServerDynamic"; // 下载动态配置
        public const string k_CheckDownloadServerDynamicSuccess = "CheckDownloadServerDynamicSuccess"; // 检查动态配置
        public const string k_SDKInit = "SDKInit"; // SDK 初始化
        public const string k_SDKInitCheck = "SDKInitCheck"; // 检查 SDK 初始化
        public const string k_CheckVersion = "CheckVersion"; // 检查版本
        public const string k_Update = "Update"; // 资源更新
        public const string k_ReloadRes = "ReloadRes"; // 重新加载资源
        public const string k_CompileShader = "CompileShader"; // 预编译着色器
        public const string k_CheckSubpackUpdate = "CheckSubpackUpdate"; // 检查分包更新
        public const string k_LaunchGC = "LaunchGC"; // 启动阶段垃圾回收
        public const string k_OpenLoginPanel = "OpenLoginPanel"; // 打开登录界面
        public const string k_SDKLogin = "SDKLogin"; // SDK 登录
        public const string k_End = "End"; // 结束启动流程
    }

    /// <summary>
    /// 启动节点生命周期状态。
    /// </summary>
    public enum LaunchNodeState : byte
    {
        Ready, // 尚未启动
        Running, // 正在执行
        Succeeded, // 执行成功
        Failed, // 执行失败
        Stopped, // 已被停止
    }

    /// <summary>
    /// 标准启动节点类型。
    /// </summary>
    public enum LaunchNodeType : byte
    {
        None, // 无效类型
        WaitFrame, // 等待一帧
        AppInit, // 初始化应用
        OpenLaunchShell, // 打开启动界面
        SplashGlobalResLoad, // 加载首屏全局资源
        DownloadServerConfig, // 下载服务器配置
        TableLoad, // 加载数据表
        CheckDownloadServerConfigSuccess, // 检查服务器配置下载结果
        BeforeLoginGlobalResLoad, // 加载登录前全局资源
        DownloadServerDynamic, // 下载服务器动态配置
        CheckDownloadServerDynamicSuccess, // 检查动态配置下载结果
        SDKInit, // 初始化 SDK
        SDKInitCheck, // 检查 SDK 初始化结果
        CheckVersion, // 检查版本
        Update, // 执行资源更新
        ReloadRes, // 重新加载资源
        CompileShader, // 预编译着色器
        CheckSubpackUpdate, // 检查分包更新
        LaunchGC, // 执行启动阶段垃圾回收
        OpenLoginPanel, // 打开登录界面
        SDKLogin, // 执行 SDK 登录
        End, // 结束启动流程
        Max, // 类型数量边界
    }
}
