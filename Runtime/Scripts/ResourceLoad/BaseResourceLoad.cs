using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    public delegate void ResourceLoadProgress(float progress);

    public delegate void ResourceLoadComplete(object asset);

    public enum ResourceType : byte
    {
        Default,
        String,
        Bytes,
        GameObject,
        Scene,
        Texture,
        Sprite,
        Material,
        Shader,
        AnimationClip,
        AudioClip,
        ScriptableObject,
    }

    public abstract class BaseResourceLoad : IManager
    {
        protected static BaseResourceLoad s_Instance = null;

        protected readonly List<IAssetDecorator> m_Decorators = new List<IAssetDecorator>(CommonDefine.s_ListConst_8);

        protected IResourceConfig m_Config;
        protected FilePathHelper m_FilePathHelper;

        public static BaseResourceLoad instance
        {
            get { return s_Instance; }
        }

        public BaseResourceLoad()
        {
            s_Instance = this;
            m_Decorators.Clear();
        }

        public void SetConfig(IResourceConfig config)
        {
            m_Config = config;
            m_FilePathHelper = new FilePathHelper(config);
        }

        public abstract object LoadResourceSync(string path, string filename, string suffix, ResourceType restype = ResourceType.Default);
        public abstract object[] LoadAllResourceSync(string path, string filename, string suffix);
        public abstract void LoadResourceAsync(string path, string filename, string suffix, ResourceLoadComplete callback, ResourceType restype = ResourceType.Default);
        public abstract void LoadSceneAsync(string path, string filename, string suffix, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null);

        public void InstallDecorator(IAssetDecorator decorator)
        {
            if (m_Decorators.Contains(decorator))
                return;
            m_Decorators.Add(decorator);
        }

        public override void DoLateUpdate() { }

        protected Type Type2Native(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.String:
                    return typeof(string);
                case ResourceType.Bytes:
                    return typeof(byte[]);
                default:
                    return typeof(object);
            }
        }
    }
}
