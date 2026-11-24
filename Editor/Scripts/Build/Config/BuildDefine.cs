namespace ST.Core
{
    /// <summary>
    /// 资源打包目标平台。
    /// </summary>
    public enum EResourceBuildPlatform
    {
        Android = 0, // Android 平台
        IOS = 1, // iOS 平台
        Windows = 2, // Windows 64 位平台
    }

    /// <summary>
    /// 资源打包类别，决定启用哪个构建器以及 AssetBundle 写入哪个客户端数据库。
    /// </summary>
    public enum EResourceBuildScope
    {
        Scene = 0, // 场景与全局资源，写入场景库
        Ui = 1, // UI 资源，写入 UI 库
        Other = 2, // 贴图与技能资源，写入 other 库
        Character = 3, // 角色资源，写入角色库
        Particle = 4, // 特效资源，写入特效库
        Story = 5, // 剧情资源，写入场景库
        Audio = 6, // 音频资源，写入 other 库
        Shader = 7, // shader 共享依赖，复制到全部五个数据库
    }

    /// <summary>
    /// assetbundledb 文本换行策略。
    /// </summary>
    public enum ELineEnding
    {
        Lf = 0, // LF 换行
        Crlf = 1, // CRLF 换行
    }

    /// <summary>
    /// 资源打包常量：数据库文件名、路径前缀、校验阈值、命令行参数与日志模板。
    /// </summary>
    public static class BuildDefine
    {
        // 数据库文件名
        public const string DB_SCENE = "assetbundledb_scene.txt"; // 场景、剧情与全局资源库
        public const string DB_UI = "assetbundledb_ui.txt"; // UI 资源库
        public const string DB_OTHER = "assetbundledb_other.txt"; // 贴图与技能资源库
        public const string DB_CHARACTER = "assetbundledb_character.txt"; // 角色资源库
        public const string DB_PARTICLE = "assetbundledb_particle.txt"; // 特效资源库

        // Bundle 定义
        public const string BUNDLE_EXTENSION = ".assetbundle"; // AssetBundle 文件后缀
        public const string MANIFEST_EXTENSION = ".manifest"; // Unity 清单文件后缀
        public const int MAX_BUNDLE_NAME_LENGTH = 165; // Bundle 名长度上限
        public const string BUILD_LIST_FILE_NAME = "builds.txt"; // 构建清单文件名
        public const string LINE_ENDING_LF = "\n"; // LF 换行
        public const string LINE_ENDING_CRLF = "\r\n"; // CRLF 换行

        // 容器与协议定义
        public const int DATABASE_COUNT = 5; // 客户端数据库数量
        public const int BUILD_LIST_CAPACITY = 256; // 构建项列表初始容量
        public const int ASSET_MAP_CAPACITY = 4096; // 资源归属映射初始容量
        public const int BUNDLE_NAME_LIST_CAPACITY = 4096; // 全量 Bundle 名列表初始容量
        public const int MESSAGE_LIST_CAPACITY = 16; // 错误与警告列表初始容量
        public const string DEPEND_LINE_PREFIX = "\tDepend:"; // 数据库依赖行前缀
        public const string REDIRECT_LINE_PREFIX = "\tRedirect:"; // 数据库 Redirect 行前缀

        // 路径前缀
        public const string PREFIX_SCENE = "scene/"; // 场景
        public const string PREFIX_STORY = "story/"; // 剧情
        public const string PREFIX_GLOBAL = "lingren/resources/global/"; // 全局资源
        public const string PREFIX_UI = "ui/"; // UI
        public const string PREFIX_TEXTURE = "texture/"; // 贴图
        public const string PREFIX_SKILL = "skill/"; // 技能
        public const string PREFIX_CHARACTER = "character/"; // 角色
        public const string PREFIX_PARTICLE = "particle/"; // 特效
        public const string PREFIX_AUDIO = "audio/"; // 音频
        public const string PREFIX_DEPEND = "depend/"; // 公共依赖
        public const string PREFIX_SHADER_DEPEND = "depend/shader_"; // shader 公共依赖

        // 命令行参数
        public const string ARG_BUILD_UI = "-BUILD_UI"; // 打包 UI
        public const string ARG_BUILD_CHARACTER = "-BUILD_CHARACTER"; // 打包角色
        public const string ARG_BUILD_PARTICLE = "-BUILD_PARTICLE"; // 打包特效
        public const string ARG_BUILD_SCENE = "-BUILD_SCENE"; // 打包场景
        public const string ARG_BUILD_STORY = "-BUILD_STORY"; // 打包剧情
        public const string ARG_BUILD_AUDIO = "-BUILD_AUDIO"; // 打包音频
        public const string ARG_BUILD_SHADER = "-BUILD_SHADER"; // 打包 shader 共享依赖
        public const string ARG_TARGET_NAME = "BUILD_TARGET_NAME"; // 旧工程目标平台参数
        public const string ARG_OUTPUT_PATH = "RES_OUT_PUT_DIR_PATH"; // 旧工程输出目录参数
        public const string ARG_PLATFORM = "-resourcePlatform"; // 新式目标平台参数
        public const string ARG_OUTPUT = "-resourceOutput"; // 新式输出目录参数
        public const string ARG_SCOPES = "-resourceScopes"; // 新式类别参数
        public const string ARG_CLEAN = "-resourceClean"; // 打包前清空输出目录

        // 平台文本值
        public const string PLATFORM_VALUE_ANDROID = "android"; // Android 平台文本
        public const string PLATFORM_VALUE_IOS = "ios"; // iOS 平台文本
        public const string PLATFORM_VALUE_IPHONE = "iphone"; // iPhone 平台文本，等价于 ios
        public const string PLATFORM_VALUE_STANDALONE = "standalone"; // Standalone 平台文本
        public const string PLATFORM_VALUE_WINDOWS = "windows"; // Windows 平台文本

        // 日志模板
        public const string LOG_OPTIONS_NULL = "资源打包：选项为空。"; // 选项为空
        public const string LOG_OUTPUT_MISSING = "资源打包：输出根目录为空。"; // 缺输出目录
        public const string LOG_BUILD_STARTED = "资源打包开始：平台 {0}，输出 {1}。"; // 开始
        public const string LOG_BUILD_SUCCEEDED = "资源打包完成：{0} 个 Bundle，{1} 个数据库。"; // 完成
        public const string LOG_BUILD_FAILED = "资源打包失败：{0} 个错误。"; // 失败
        public const string LOG_PREPARE_FAILED = "资源打包：无法准备输出目录。{0} {1}"; // 目录失败
        public const string LOG_SWITCH_TARGET_FAILED = "资源打包：无法切换构建目标。{0}"; // 切平台失败
        public const string LOG_NO_BUILD_ENTRY = "资源打包：没有收集到任何 AssetBundle。"; // 空构建
        public const string LOG_BUILD_PIPELINE_NULL = "资源打包：BuildPipeline 没有返回清单。"; // 构建失败
        public const string LOG_TASK_COLLECTED = "资源打包：{0} 收集 {1} 个 Bundle。"; // 收集统计
        public const string LOG_EMPTY_BUNDLE = "资源打包：Bundle 没有任何资源，已跳过。{0}"; // 空包
        public const string LOG_UNKNOWN_PREFIX = "资源打包：AssetBundle 路径无法归类。{0}"; // 未知前缀
        public const string LOG_UNKNOWN_DEPEND = "资源打包：depend 路径没有共享规则。{0}"; // 未知依赖
        public const string LOG_DUPLICATE_ASSET = "资源打包：资源被重复引用。{0} -> {1}"; // 重复资源
        public const string LOG_NAME_EMPTY = "资源打包：Bundle 名为空。"; // 空名字
        public const string LOG_NAME_TOO_LONG = "资源打包：Bundle 名超过 {0} 字符。{1}"; // 名过长
        public const string LOG_EXTENSION_MISSING = "资源打包：Bundle 名缺少后缀。{0} {1}"; // 缺后缀
        public const string LOG_DUPLICATE_BUNDLE = "资源打包：数据库内 Bundle 名重复。{0}"; // 重复包名
        public const string LOG_MISSING_DEPEND = "资源打包：依赖不在数据库内。{0} -> {1}"; // 依赖缺失
        public const string LOG_DEPEND_CYCLE = "资源打包：依赖存在循环。{0}"; // 依赖环
        public const string LOG_MISSING_REDIRECT = "资源打包：Redirect 容器不在数据库内。{0} -> {1}"; // 容器缺失
        public const string LOG_INVALID_REDIRECT = "资源打包：Redirect 切片无效。{0}"; // 切片非法
        public const string LOG_DB_WRITTEN = "资源打包：数据库已写入 {0}，共 {1} 条记录。"; // 数据库写入
        public const string LOG_BUILD_LIST_WRITTEN = "资源打包：构建清单已写入 {0}。"; // 清单写入
        public const string LOG_SCOPE_UNKNOWN = "资源打包：忽略无法识别的类别。{0}"; // 类别无法识别
        public const string LOG_PLATFORM_UNKNOWN = "资源打包：忽略无法识别的平台。{0}"; // 平台无法识别
        public const string LOG_DEPEND_NOT_SHARED = "资源打包：非 shader 的 depend 包不会被收集。{0}"; // 非共享依赖
        public const string LOG_DB_NULL = "资源打包：数据库为空，无法编码。"; // 数据库为空
        public const string LOG_RECORD_NAME_EMPTY = "资源打包：数据库记录名为空。"; // 记录名为空
        public const string LOG_MANIFEST_NULL = "资源打包：清单为空，无法生成数据库。"; // 清单为空
    }
}
