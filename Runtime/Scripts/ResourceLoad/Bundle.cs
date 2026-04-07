using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    public class Bundle
    {
        enum State { Unloaded, Loading, Loaded }

        State m_State = State.Unloaded;
        string m_Path;
        AssetBundleLoad m_Load;
        FilePathHelper m_FilePathHelper;
        List<Bundle> m_DependList = new List<Bundle>(CommonDefine.s_ListConst_8);
        AssetBundle m_AssetBundle;
        List<AsyncTask> m_PendingLoadList = new List<AsyncTask>(CommonDefine.s_ListConst_8);

        public AssetBundleLoad load
        {
            get { return m_Load; }
            set { m_Load = value; }
        }

        public AssetBundle assetBundle
        {
            get { return m_AssetBundle; }
        }

        public bool isLoaded
        {
            get { return m_State == State.Loaded; }
        }

        public bool dependIsLoaded
        {
            get
            {
                foreach (var depend in m_DependList)
                {
                    if (depend == null) continue;
                    if (!depend.isLoaded) return false;
                }
                return true;
            }
        }

        public Bundle(string path, FilePathHelper filePathHelper)
        {
            m_Path = path;
            m_FilePathHelper = filePathHelper;
        }

        public void InitDependencies(AssetBundleManifest manifest)
        {
            if (m_Load == null || manifest == null)
                return;

            var dependarray = manifest.GetAllDependencies(m_Path);
            if (dependarray == null) return;

            foreach (var dependname in dependarray)
            {
                if (dependname == null) continue;
                var bundle = m_Load.GetBundle(dependname);
                if (bundle == null) continue;
                m_DependList.Add(bundle);
            }
        }

        public void LoadBundleAsync()
        {
            if (m_State == State.Unloaded)
            {
                m_State = State.Loading;

                foreach (var depend in m_DependList)
                {
                    if (depend == null) continue;
                    depend.LoadBundleAsync();
                }

                AsyncTask asynctask = new AsyncBundleRequest(this, m_Path, m_FilePathHelper, OnAsyncLoaded);
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            }
        }

        public void LoadBundleSync()
        {
            if (m_State == State.Unloaded)
            {
                m_State = State.Loaded;

                foreach (var depend in m_DependList)
                {
                    if (depend == null) continue;
                    depend.LoadBundleSync();
                }

                var fullpath = m_FilePathHelper.GetBundleFullPath(m_Path);
                m_AssetBundle = AssetBundle.LoadFromFile(fullpath);
            }
        }

        public object[] LoadAllSync()
        {
            LoadBundleSync();
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAllAssets();
        }

        public object LoadSync(string resname, Type type)
        {
            LoadBundleSync();
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAsset(resname, type);
        }

        public void LoadAsync(string resname, Type type, ResourceLoadComplete callback)
        {
            LoadBundleAsync();

            AsyncTask asynctask = new AsyncAssetRequest(this, resname, type, callback);
            if (isLoaded)
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            else
                m_PendingLoadList.Add(asynctask);
        }

        public void LoadSceneAsync(string resname, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            LoadBundleAsync();

            AsyncTask asynctask = new AsyncSceneRequest(this, resname, progress, complete);
            if (isLoaded)
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            else
                m_PendingLoadList.Add(asynctask);
        }

        public AssetBundleRequest LoadAssetAsyncFromBundle(string resname, Type type)
        {
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAssetAsync(resname, type);
        }

        void OnAsyncLoaded(object obj)
        {
            if (obj == null) return;
            var bundle = (AssetBundle)obj;
            if (bundle == null) return;

            m_State = State.Loaded;
            m_AssetBundle = bundle;

            foreach (var load in m_PendingLoadList)
            {
                if (load == null) continue;
                BaseAsyncTaskManager.instance.AddTask(load);
            }

            m_PendingLoadList.Clear();
        }
    }
}
