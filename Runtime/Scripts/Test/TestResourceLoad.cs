using UnityEngine;
using Logger = ST.Core.Logging.Logger;

namespace ST.Core.Test
{
    /// <summary>
    /// 资源加载模块示例：OnGUI 按钮触发同步/异步资源与场景加载，仅用于本地联调演示。
    /// </summary>
    public class TestResourceLoad : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // Inspector 配置
        // ──────────────────────────────────────────

        /// <summary>测试用 Prefab 所在目录（相对于 editorPathPrefix）。</summary>
        public string prefabPath = "prefabs/";
        /// <summary>测试用 Prefab 文件名（不含扩展名）。</summary>
        public string prefabName = "TestPrefab";
        /// <summary>测试用 Prefab 扩展名。</summary>
        public string prefabSuffix = ".prefab";

        /// <summary>测试用 Texture 所在目录。</summary>
        public string texturePath = "ui/icon/";
        /// <summary>测试用 Texture 文件名（不含扩展名）。</summary>
        public string textureName = "hero_icon";
        /// <summary>测试用 Texture 扩展名。</summary>
        public string textureSuffix = ".png";

        /// <summary>测试用场景所在目录。</summary>
        public string scenePath = "scene/";
        /// <summary>测试用场景文件名（不含扩展名）。</summary>
        public string sceneName = "TestScene";
        /// <summary>测试用场景扩展名。</summary>
        public string sceneSuffix = ".unity";

        /// <summary>测试用 Lua 资源所在目录。</summary>
        public string luaPath = "config/lua/";
        /// <summary>测试用 Lua 文件名（不含扩展名）。</summary>
        public string luaName = "luapackage";
        /// <summary>测试用 Lua 文件扩展名。</summary>
        public string luaSuffix = ".asset";

        // ──────────────────────────────────────────
        // 私有状态
        // ──────────────────────────────────────────

        /// <summary>异步任务管理器。</summary>
        AsyncTaskManager m_AsyncTaskMgr;
        /// <summary>异步加载进度（0~1），用于 OnGUI 进度条显示。</summary>
        float m_AsyncProgress = 0f;
        /// <summary>当前操作日志，显示在屏幕状态面板中。</summary>
        string m_Log = "等待操作...";

        // ──────────────────────────────────────────
        // 生命周期
        // ──────────────────────────────────────────

        /// <summary>初始化异步任务管理器与资源加载器。</summary>
        void Start()
        {
            m_AsyncTaskMgr = new AsyncTaskManager();
            m_AsyncTaskMgr.DoInit();

            var resLoad = new ResourceLoad();
            resLoad.SetConfig(new TestResourceConfig());
            resLoad.DoInit();

            m_Log = "初始化完成，点击按钮开始测试。";
            Logger.Log("[TestResourceLoad] 初始化完成");
        }

        /// <summary>每帧驱动异步任务队列。</summary>
        void Update()
        {
            m_AsyncTaskMgr.DoUpdate();
        }

        /// <summary>绘制测试按钮与状态面板。</summary>
        void OnGUI()
        {
            float btnW = 420f;
            float btnH = 90f;
            float x = 10f;
            float y = 10f;
            float gap = 10f;

            var btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 24;

            var labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 22;

            var boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 22;

            if (GUI.Button(new Rect(x, y, btnW, btnH), "同步加载 Prefab", btnStyle))
                LoadPrefabSync();

            y += btnH + gap;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "异步加载 Prefab", btnStyle))
                LoadPrefabAsync();

            y += btnH + gap;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "同步加载 Texture", btnStyle))
                LoadTextureSync();

            y += btnH + gap;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "异步加载 Texture", btnStyle))
                LoadTextureAsync();

            y += btnH + gap;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "同步加载全部资产", btnStyle))
                LoadAllSync();

            y += btnH + gap;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "异步加载场景", btnStyle))
                LoadSceneAsync();

            y += btnH + gap;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "同步加载 Lua (byte[])", btnStyle))
                LoadLuaSync();

            y += btnH + gap;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "切换 AssetBundle 模式", btnStyle))
                ToggleAssetBundleMode();

            float panelY = y + btnH + gap * 2;
            GUI.Box(new Rect(x, panelY, btnW, 120f), "", boxStyle);
            GUI.Label(new Rect(x + 8f, panelY + 8f, btnW - 16f, 104f), m_Log, labelStyle);

            if (m_AsyncProgress > 0f && m_AsyncProgress < 1f)
            {
                float barY = panelY + 128f;
                GUI.Box(new Rect(x, barY, btnW, 36f), "", boxStyle);
                GUI.Box(new Rect(x, barY, btnW * m_AsyncProgress, 36f), "", boxStyle);
                GUI.Label(new Rect(x + 8f, barY + 8f, btnW, 24f),
                    string.Format("加载中 {0:F0}%", m_AsyncProgress * 100f), labelStyle);
            }
        }

        // ──────────────────────────────────────────
        // 测试方法
        // ──────────────────────────────────────────

        /// <summary>同步加载 Prefab，成功时打印对象名称。</summary>
        void LoadPrefabSync()
        {
            var obj = ResourceLoad.instance.LoadResourceSync(
                prefabPath, prefabName, prefabSuffix) as GameObject;

            if (obj != null)
            {
                m_Log = string.Format("[同步 Prefab] 成功：{0}", obj.name);
                Logger.LogInfoF("[TestResourceLoad] 同步加载 Prefab 成功：{0}", obj.name);
            }
            else
            {
                m_Log = "[同步 Prefab] 失败：返回 null";
                Logger.LogWarning("[TestResourceLoad] 同步加载 Prefab 失败");
            }
        }

        /// <summary>异步加载 Prefab，完成后打印对象名称。</summary>
        void LoadPrefabAsync()
        {
            m_Log = "[异步 Prefab] 请求已发出...";
            m_AsyncProgress = 0.01f;

            ResourceLoad.instance.LoadResourceAsync(
                prefabPath, prefabName, prefabSuffix,
                (obj) =>
                {
                    m_AsyncProgress = 1f;
                    var go = obj as GameObject;
                    if (go != null)
                    {
                        m_Log = string.Format("[异步 Prefab] 成功：{0}", go.name);
                        Logger.LogInfoF("[TestResourceLoad] 异步加载 Prefab 成功：{0}", go.name);
                    }
                    else
                    {
                        m_Log = "[异步 Prefab] 失败：返回 null";
                        Logger.LogWarning("[TestResourceLoad] 异步加载 Prefab 失败");
                    }
                });
        }

        /// <summary>同步加载 Texture，成功时打印尺寸信息。</summary>
        void LoadTextureSync()
        {
            var tex = ResourceLoad.instance.LoadResourceSync(
                texturePath, textureName, textureSuffix) as Texture2D;

            if (tex != null)
            {
                m_Log = string.Format("[同步 Texture] 成功：{0}  {1}x{2}", tex.name, tex.width, tex.height);
                Logger.LogInfoF("[TestResourceLoad] 同步加载 Texture 成功：{0}", tex.name);
            }
            else
            {
                m_Log = "[同步 Texture] 失败：返回 null";
                Logger.LogWarning("[TestResourceLoad] 同步加载 Texture 失败");
            }
        }

        /// <summary>异步加载 Texture，完成后打印尺寸信息。</summary>
        void LoadTextureAsync()
        {
            m_Log = "[异步 Texture] 请求已发出...";
            m_AsyncProgress = 0.01f;

            ResourceLoad.instance.LoadResourceAsync(
                texturePath, textureName, textureSuffix,
                (obj) =>
                {
                    m_AsyncProgress = 1f;
                    var tex = obj as Texture2D;
                    if (tex != null)
                    {
                        m_Log = string.Format("[异步 Texture] 成功：{0}  {1}x{2}",
                            tex.name, tex.width, tex.height);
                        Logger.LogInfoF("[TestResourceLoad] 异步加载 Texture 成功：{0}", tex.name);
                    }
                    else
                    {
                        m_Log = "[异步 Texture] 失败：返回 null";
                        Logger.LogWarning("[TestResourceLoad] 异步加载 Texture 失败");
                    }
                });
        }

        /// <summary>同步加载指定路径下全部资源，打印每项名称。</summary>
        void LoadAllSync()
        {
            var objs = ResourceLoad.instance.LoadAllResourceSync(
                prefabPath, prefabName, prefabSuffix);

            if (objs != null && objs.Length > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(string.Format("[LoadAll] 共 {0} 项：", objs.Length));
                foreach (var o in objs)
                {
                    var uobj = o as Object;
                    if (uobj != null)
                        sb.AppendLine(string.Format("  · {0}", uobj.name));
                }
                m_Log = sb.ToString();
                Logger.LogInfoF("[TestResourceLoad] LoadAll 成功，共 {0} 项", objs.Length);
            }
            else
            {
                m_Log = "[LoadAll] 失败：返回 null 或空数组";
                Logger.LogWarning("[TestResourceLoad] LoadAll 失败");
            }
        }

        /// <summary>异步加载场景，进度条实时更新，完成后打印场景名。</summary>
        void LoadSceneAsync()
        {
            m_Log = "[异步场景] 请求已发出...";
            m_AsyncProgress = 0.01f;

            ResourceLoad.instance.LoadSceneAsync(
                scenePath, sceneName, sceneSuffix,
                (progress) =>
                {
                    m_AsyncProgress = progress;
                },
                (obj) =>
                {
                    m_AsyncProgress = 1f;
                    m_Log = string.Format("[异步场景] 加载完成：{0}", sceneName);
                    Logger.LogInfoF("[TestResourceLoad] 异步场景加载完成：{0}", sceneName);
                });
        }

        /// <summary>同步加载 Lua 字节码（ResourceType.Bytes），打印字节长度。</summary>
        void LoadLuaSync()
        {
            var bytes = ResourceLoad.instance.LoadResourceSync(
                luaPath, luaName, luaSuffix,
                ResourceType.Bytes) as byte[];

            if (bytes != null && bytes.Length > 0)
            {
                m_Log = string.Format("[Lua byte[]] 成功：{0} 字节", bytes.Length);
                Logger.LogInfoF("[TestResourceLoad] Lua 字节码加载成功：{0} 字节", bytes.Length);
            }
            else
            {
                m_Log = "[Lua byte[]] 失败：返回 null 或空";
                Logger.LogWarning("[TestResourceLoad] Lua 字节码加载失败");
            }
        }

        /// <summary>切换编辑器下是否强制走 AssetBundle 模式并刷新日志。</summary>
        void ToggleAssetBundleMode()
        {
            ResourceLoad.useAssetBundle = !ResourceLoad.useAssetBundle;
            m_Log = string.Format("useAssetBundle = {0}", ResourceLoad.useAssetBundle);
            Logger.LogInfoF("[TestResourceLoad] 切换 AssetBundle 模式：{0}", ResourceLoad.useAssetBundle);
        }

        // ══════════════════════════════════════════
        // 内嵌配置实现，仅供本测试使用
        // ══════════════════════════════════════════

        /// <summary>测试专用资源配置，路径与应用名可按实际工程调整。</summary>
        class TestResourceConfig : IResourceConfig
        {
            /// <inheritdoc />
            public string appName { get { return "testgame"; } }
            /// <inheritdoc />
            public string assetDir { get { return "assetBundle"; } }
            /// <inheritdoc />
            public string bundleSuffix { get { return ".unity3d"; } }
            /// <inheritdoc />
            public string editorPathPrefix { get { return "Assets/Package/"; } }
        }
    }
}