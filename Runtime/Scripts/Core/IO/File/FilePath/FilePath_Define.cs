namespace ST.Core
{
    /// <summary>路径状态定义部分，集中存放根目录缓存与平台实现引用。</summary>
    public static partial class FilePath
    {
        /// <summary>平台相关目录与包内读取的实现，未注入时使用 Unity 默认行为。</summary>
        static IPlatformFileProvider s_PlatformProvider;

        /// <summary>外存根目录缓存。</summary>
        static string s_PersistentDataPath;

        /// <summary>包内根目录缓存。</summary>
        static string s_StreamingAssetsPath;

        /// <summary>application dataPath 缓存。</summary>
        static string s_DataPath;

        /// <summary>外存资源目录缓存。</summary>
        static string s_PersistentDataPath4Res;

        /// <summary>外存 Bin 目录缓存。</summary>
        static string s_PersistentDataPath4Bin;

        /// <summary>外存聊天媒体目录缓存。</summary>
        static string s_PersistentDataPath4ChatMedia;

        /// <summary>外存拍照目录缓存。</summary>
        static string s_PersistentDataPath4CameraMedia;

        /// <summary>外存日志目录缓存。</summary>
        static string s_PersistentDataPath4Log;

        /// <summary>外存临时目录缓存。</summary>
        static string s_PersistentDataPath4Temp;

        /// <summary>外存网络贴图目录缓存。</summary>
        static string s_PersistentDataPath4NetTexture;

        /// <summary>平台实现，供文件层读取包内文件使用。</summary>
        internal static IPlatformFileProvider platformProvider
        {
            get { return s_PlatformProvider; }
        }
    }
}

