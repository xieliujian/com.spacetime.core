namespace ST.Core
{
    /// <summary>
    /// 资源加载模块的路径模板、容器容量与数据库解析常量，集中存放避免散落在实现中。
    /// </summary>
    public static class ResourceDefine
    {
        // 路径定义
        public const string BUNDLE_FULL_PATH_FORMAT = "{0}{1}/{2}"; // 根目录 + 应用子目录 + 相对路径
        public const string APP_RES_PATH_FORMAT = "{0}{1}/"; // 根目录 + 小写应用子目录

        // 容器定义
        public const int BUNDLE_DICT_CAPACITY = 1024; // Bundle 字典默认容量
        public const int BUNDLE_INFO_DICT_CAPACITY = 4096; // 数据库 Bundle 信息字典默认容量
        public const int DEPEND_LIST_CAPACITY = 8; // 单个 Bundle 的依赖列表默认容量
        public const int PENDING_REQUEST_LIST_CAPACITY = 8; // Bundle 加载完成前挂起请求的列表容量
        public const int DECORATOR_LIST_CAPACITY = 8; // 资产装饰器列表默认容量
        public const int TASK_LIST_CAPACITY = 100; // 异步任务列表默认容量
        public const int TEMP_TASK_LIST_CAPACITY = 16; // 已结束任务暂存列表默认容量

        // 数据库定义
        public const char FIELD_SEPARATOR = '\t'; // 数据库字段分隔符
        public const string DEPEND_ATTR_HEADER = "\tDepend:"; // 依赖属性行头

        // 日志定义
        public const string LOG_DB_FILE_NOT_FOUND = "AssetBundleDBMgr can not load {0}"; // 数据库文件缺失
        public const string LOG_DB_INDEX_OUT_OF_RANGE =
            "AssetBundleDBMgr GetAssetBundleName index out of range: {0}"; // 依赖索引越界
    }
}
