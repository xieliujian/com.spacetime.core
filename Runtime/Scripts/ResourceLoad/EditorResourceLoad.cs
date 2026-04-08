using System;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 编辑器模式下通过 <c>AssetDatabase</c> 同步/异步加载资源；路径前缀来自 <see cref="IResourceConfig.editorPathPrefix"/>。
    /// </summary>
    public class EditorResourceLoad
    {
        /// <summary>资源配置，提供编辑器路径前缀等参数。</summary>
        readonly IResourceConfig m_Config;

        /// <summary>创建编辑器加载器并注入配置。</summary>
        /// <param name="config">资源配置。</param>
        public EditorResourceLoad(IResourceConfig config)
        {
            m_Config = config;
        }

        /// <summary>同步加载编辑器内资产（非 Editor 平台下始终返回 <c>null</c>）。</summary>
        /// <param name="realpath">相对 <see cref="IResourceConfig.editorPathPrefix"/> 的逻辑路径。</param>
        /// <param name="suffix">资源扩展名。</param>
        /// <param name="type">目标类型。</param>
        public object LoadSync(string realpath, string suffix, Type type)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            return UnityEditor.AssetDatabase.LoadAssetAtPath(loadpath, type);
#else
            return null;
#endif
        }

        /// <summary>加载指定路径下全部子资源（编辑器）。</summary>
        public object[] LoadAllSync(string realpath, string suffix)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(loadpath);
#else
            return null;
#endif
        }

        /// <summary>通过 <see cref="EditorAsyncAssetRequest"/> 在任务队列中完成回调。</summary>
        public void LoadAsync(string realpath, string suffix, Type type, ResourceLoadComplete callback)
        {
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            AsyncTask asynctask = new EditorAsyncAssetRequest(loadpath, type);
            if (callback != null)
                asynctask.completeEvent = callback;
            BaseAsyncTaskManager.instance.AddTask(asynctask);
        }

        /// <summary>编辑器 Play 模式异步加载场景。</summary>
        public void LoadSceneAsync(string realpath, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            AsyncTask asynctask = new EditorAsyncSceneRequest(loadpath);
            asynctask.progressEvent = progress;
            asynctask.completeEvent = complete;
            BaseAsyncTaskManager.instance.AddTask(asynctask);
#endif
        }
    }
}
