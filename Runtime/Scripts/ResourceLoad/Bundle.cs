using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 单个 AssetBundle 的逻辑封装：维护依赖列表、同步/异步加载、以及依赖未就绪时挂起的子任务队列。
    /// </summary>
    public class Bundle
    {
        /// <summary>Bundle 文件加载阶段。</summary>
        enum State
        {
            /// <summary>尚未加载。</summary>
            Unloaded,
            /// <summary>异步加载中。</summary>
            Loading,
            /// <summary>已加载完毕（同步或异步皆可到达此状态）。</summary>
            Loaded
        }

        /// <summary>当前加载阶段。</summary>
        State m_State = State.Unloaded;
        /// <summary>该包在清单中的相对路径（同时作为 <see cref="AssetBundleLoad"/> 字典 key）。</summary>
        string m_Path;
        /// <summary>所属加载中枢，用于按名称查找依赖包。</summary>
        AssetBundleLoad m_Load;
        /// <summary>磁盘完整路径拼接工具。</summary>
        FilePathHelper m_FilePathHelper;
        /// <summary>此包的直接依赖包列表。</summary>
        List<Bundle> m_DependList = new List<Bundle>(CommonDefine.s_ListConst_8);
        /// <summary>已加载完成的原生 Unity AssetBundle 对象。</summary>
        AssetBundle m_AssetBundle;
        /// <summary>本包异步加载完成前挂起的资产/场景请求，完成后统一提交给任务管理器。</summary>
        List<AsyncTask> m_PendingLoadList = new List<AsyncTask>(CommonDefine.s_ListConst_8);

        /// <summary>所属 <see cref="AssetBundleLoad"/>，用于按名查找依赖包。</summary>
        public AssetBundleLoad load
        {
            get { return m_Load; }
            set { m_Load = value; }
        }

        /// <summary>已加载的 Unity <see cref="AssetBundle"/>。</summary>
        public AssetBundle assetBundle
        {
            get { return m_AssetBundle; }
        }

        /// <summary>是否已完成同步或异步加载并持有 <see cref="assetBundle"/>。</summary>
        public bool isLoaded
        {
            get { return m_State == State.Loaded; }
        }

        /// <summary>所有依赖包是否均已 <see cref="isLoaded"/>。</summary>
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

        /// <param name="path">在清单中的 Bundle 相对路径。</param>
        /// <param name="filePathHelper">磁盘完整路径解析。</param>
        public Bundle(string path, FilePathHelper filePathHelper)
        {
            m_Path = path;
            m_FilePathHelper = filePathHelper;
        }

        /// <summary>根据依赖名称数组解析并缓存依赖包引用（通过 <see cref="load"/> 按名查表）。</summary>
        /// <param name="dependNames">依赖的 Bundle 名称数组，由 <see cref="AssetBundleDBMgr"/> 提供。</param>
        public void InitDependencies(string[] dependNames)
        {
            if (m_Load == null || dependNames == null)
                return;

            foreach (var dependname in dependNames)
            {
                if (dependname == null) continue;
                var bundle = m_Load.GetBundle(dependname);
                if (bundle == null) continue;
                m_DependList.Add(bundle);
            }
        }

        /// <summary>异步加载本包及依赖（先递归依赖 <see cref="LoadBundleAsync"/>，再提交 <see cref="AsyncBundleRequest"/>）。</summary>
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

        /// <summary>同步加载本包及依赖链。</summary>
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

        /// <summary>同步加载包后读取其中全部资源。</summary>
        public object[] LoadAllSync()
        {
            LoadBundleSync();
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAllAssets();
        }

        /// <summary>同步按名与类型加载单个资源。</summary>
        public object LoadSync(string resname, Type type)
        {
            LoadBundleSync();
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAsset(resname, type);
        }

        /// <summary>异步加载资源；若本包未就绪则将 <see cref="AsyncAssetRequest"/> 加入挂起队列。</summary>
        public void LoadAsync(string resname, Type type, ResourceLoadComplete callback)
        {
            LoadBundleAsync();

            AsyncTask asynctask = new AsyncAssetRequest(this, resname, type, callback);
            if (isLoaded)
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            else
                m_PendingLoadList.Add(asynctask);
        }

        /// <summary>异步加载场景（逻辑同 <see cref="LoadAsync"/> 的挂起机制）。</summary>
        public void LoadSceneAsync(string resname, ResourceLoadProgress progress = null, ResourceLoadComplete complete = null)
        {
            LoadBundleAsync();

            AsyncTask asynctask = new AsyncSceneRequest(this, resname, progress, complete);
            if (isLoaded)
                BaseAsyncTaskManager.instance.AddTask(asynctask);
            else
                m_PendingLoadList.Add(asynctask);
        }

        /// <summary>在已加载的 <see cref="assetBundle"/> 上发起 Unity 异步资源请求。</summary>
        public AssetBundleRequest LoadAssetAsyncFromBundle(string resname, Type type)
        {
            if (m_AssetBundle == null) return null;
            return m_AssetBundle.LoadAssetAsync(resname, type);
        }

        /// <summary>卸载已加载的 AssetBundle 并重置状态为 <see cref="State.Unloaded"/>，使其可以重新加载。</summary>
        /// <param name="unloadAllLoadedObjects">是否同时卸载从该包实例化的所有对象。</param>
        public void Unload(bool unloadAllLoadedObjects)
        {
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(unloadAllLoadedObjects);
                m_AssetBundle = null;
            }
            m_State = State.Unloaded;
            m_PendingLoadList.Clear();
        }

        /// <summary>
        /// <see cref="AsyncBundleRequest"/> 完成后的回调：更新状态、缓存 <see cref="m_AssetBundle"/>，
        /// 并将 <see cref="m_PendingLoadList"/> 中所有挂起任务提交给 <see cref="BaseAsyncTaskManager"/>。
        /// </summary>
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
