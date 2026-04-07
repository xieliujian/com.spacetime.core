using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ST.Core
{
    public class EditorResourceLoad
    {
        readonly IResourceConfig m_Config;

        public EditorResourceLoad(IResourceConfig config)
        {
            m_Config = config;
        }

        public object LoadSync(string realpath, string suffix, Type type)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            return UnityEditor.AssetDatabase.LoadAssetAtPath(loadpath, type);
#else
            return null;
#endif
        }

        public object[] LoadAllSync(string realpath, string suffix)
        {
#if UNITY_EDITOR
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(loadpath);
#else
            return null;
#endif
        }

        public void LoadAsync(string realpath, string suffix, Type type, ResourceLoadComplete callback)
        {
            string loadpath = m_Config.editorPathPrefix + realpath + suffix;
            AsyncTask asynctask = new EditorAsyncAssetRequest(loadpath, type);
            if (callback != null)
                asynctask.completeEvent = callback;
            BaseAsyncTaskManager.instance.AddTask(asynctask);
        }

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
