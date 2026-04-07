using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 资源加载配置接口，由业务项目实现，提供应用名称、Bundle 路径等配置。
    /// </summary>
    public interface IResourceConfig
    {
        /// <summary>应用/游戏名称，用于 StreamingAssets 子目录名。</summary>
        string appName { get; }

        /// <summary>AssetBundle 输出目录名（相对于工程根目录）。</summary>
        string assetDir { get; }

        /// <summary>AssetBundle 文件后缀，例如 ".unity3d"。</summary>
        string bundleSuffix { get; }

        /// <summary>编辑器模式下资源路径前缀，例如 "Assets/Package/"。</summary>
        string editorPathPrefix { get; }
    }
}