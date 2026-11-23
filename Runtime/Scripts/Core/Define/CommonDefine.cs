namespace ST.Core
{
    /// <summary>
    /// 框架内常用的预分配容量与列表初始大小等常量，集中存放避免散落在业务代码中。
    /// </summary>
    public static class CommonDefine
    {
        // 变量定义
        public static readonly int s_ListConst_8 = 8; // 小集合默认容量 8
        public static readonly int s_ListConst_16 = 16; // 集合默认容量 16
        public static readonly int s_ListConst_32 = 32; // 集合默认容量 32
        public static readonly int s_ListConst_64 = 64; // 集合默认容量 64
        public static readonly int s_ListConst_100 = 100; // 消息字典等中等集合初始容量 100
        public static readonly int s_ListConst_1024 = 1024; // FlatBuffer 构建缓冲等较大缓冲默认字节数 1024
    }
}

