namespace ST.Core
{
    /// <summary>资源加载进度回调。</summary>
    /// <param name="progress">进度值，通常为 0~1。</param>
    public delegate void ResourceLoadProgress(float progress);

    /// <summary>资源或场景加载完成回调。</summary>
    /// <param name="asset">加载结果对象，可能经 <see cref="IAssetDecorator"/> 转换类型。</param>
    public delegate void ResourceLoadComplete(object asset);
}
