namespace ST.Core
{
    /// <summary>
    /// 运行时文件路径工具，统一维护包内根目录、外存根目录与业务子目录，并对 Windows 超长路径做统一封装。
    /// </summary>
    public static partial class FilePath
    {
        /// <summary>外存根目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath
        {
            get
            {
                if (s_PersistentDataPath == null)
                {
                    s_PersistentDataPath = GetPersistentDataPath();
                }

                return s_PersistentDataPath;
            }
        }

        /// <summary>包内根目录，带 file 协议前缀，Android 上不带。</summary>
        public static string streamingAssetsPathWithScheme
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                string androidPath = streamingAssetsPath;
                return androidPath;
#else
                string rootPath = streamingAssetsPath;
                string schemePath = "file:///" + rootPath;
                return schemePath;
#endif
            }
        }

        /// <summary>包内根目录，以 <c>/</c> 结尾。</summary>
        public static string streamingAssetsPath
        {
            get
            {
                if (s_StreamingAssetsPath == null)
                {
                    s_StreamingAssetsPath = GetStreamingAssetsPath();
                }

                return s_StreamingAssetsPath;
            }
        }

        /// <summary>application dataPath，Android 分包时为主资源包路径。</summary>
        public static string dataPath
        {
            get
            {
                if (s_DataPath == null)
                {
                    s_DataPath = GetDataPath();
                }

                return s_DataPath;
            }
        }

        /// <summary>外存资源目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath4Res
        {
            get
            {
                if (s_PersistentDataPath4Res == null)
                {
                    s_PersistentDataPath4Res = GetPersistentResPath();
                    EnsureDirectoryExists(s_PersistentDataPath4Res);
                }

                return s_PersistentDataPath4Res;
            }
        }

        /// <summary>外存 Bin 目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath4Bin
        {
            get
            {
                if (s_PersistentDataPath4Bin == null)
                {
                    s_PersistentDataPath4Bin = GetPersistentBinPath();
                    EnsureDirectoryExists(s_PersistentDataPath4Bin);
                }

                return s_PersistentDataPath4Bin;
            }
        }

        /// <summary>外存聊天媒体目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath4ChatMedia
        {
            get
            {
                if (s_PersistentDataPath4ChatMedia == null)
                {
                    s_PersistentDataPath4ChatMedia = persistentDataPath + FileDefine.k_ChatMediaDirName;
                    EnsureDirectoryExists(s_PersistentDataPath4ChatMedia);
                }

                return s_PersistentDataPath4ChatMedia;
            }
        }

        /// <summary>外存拍照目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath4CameraMedia
        {
            get
            {
                if (s_PersistentDataPath4CameraMedia == null)
                {
                    s_PersistentDataPath4CameraMedia = persistentDataPath + FileDefine.k_CameraMediaDirName;
                    EnsureDirectoryExists(s_PersistentDataPath4CameraMedia);
                }

                return s_PersistentDataPath4CameraMedia;
            }
        }

        /// <summary>外存日志目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath4Log
        {
            get
            {
                if (s_PersistentDataPath4Log == null)
                {
                    s_PersistentDataPath4Log = persistentDataPath + FileDefine.k_LogDirName;
                    EnsureDirectoryExists(s_PersistentDataPath4Log);
                }

                return s_PersistentDataPath4Log;
            }
        }

        /// <summary>外存临时目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath4Temp
        {
            get
            {
                if (s_PersistentDataPath4Temp == null)
                {
                    s_PersistentDataPath4Temp = persistentDataPath + FileDefine.k_TempDirName;
                    EnsureDirectoryExists(s_PersistentDataPath4Temp);
                }

                return s_PersistentDataPath4Temp;
            }
        }

        /// <summary>外存网络贴图目录，以 <c>/</c> 结尾。</summary>
        public static string persistentDataPath4NetTexture
        {
            get
            {
                if (s_PersistentDataPath4NetTexture == null)
                {
                    s_PersistentDataPath4NetTexture = persistentDataPath + FileDefine.k_NetTextureDirName;
                    EnsureDirectoryExists(s_PersistentDataPath4NetTexture);
                }

                return s_PersistentDataPath4NetTexture;
            }
        }

        /// <summary>当前是否是 Android App Bundle 安装，其它平台恒为 false。</summary>
        public static bool isAppBundle
        {
            get
            {
                bool isBundle = false;

#if UNITY_ANDROID && !UNITY_EDITOR
                bool hasProvider = s_PlatformProvider != null;
                if (hasProvider)
                {
                    isBundle = s_PlatformProvider.IsAppBundle();
                }
#endif

                return isBundle;
            }
        }

        /// <summary>
        /// 注入平台实现。重复设置会清空已缓存的目录。
        /// </summary>
        /// <param name="provider">平台实现，允许为 null。</param>
        public static void SetPlatformProvider(IPlatformFileProvider provider)
        {
            s_PlatformProvider = provider;
            ResetCache();
        }

        /// <summary>清空全部根目录缓存，平台环境变化后调用一次。</summary>
        public static void ResetCache()
        {
            s_PersistentDataPath = null;
            s_StreamingAssetsPath = null;
            s_DataPath = null;
            s_PersistentDataPath4Res = null;
            s_PersistentDataPath4Bin = null;
            s_PersistentDataPath4ChatMedia = null;
            s_PersistentDataPath4CameraMedia = null;
            s_PersistentDataPath4Log = null;
            s_PersistentDataPath4Temp = null;
            s_PersistentDataPath4NetTexture = null;
        }
    }
}

