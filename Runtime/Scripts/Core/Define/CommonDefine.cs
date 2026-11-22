using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST.Core
{
    /// <summary>
    /// 框架内常用的预分配容量与列表初始大小等常量，避免魔法数字分散在业务代码中。
    /// </summary>
    public class CommonDefine
    {
        /// <summary>小集合默认容量 8。</summary>
        public static readonly int s_ListConst_8 = 8;
        /// <summary>集合默认容量 16。</summary>
        public static readonly int s_ListConst_16 = 16;
        /// <summary>集合默认容量 32。</summary>
        public static readonly int s_ListConst_32 = 32;
        /// <summary>集合默认容量 64。</summary>
        public static readonly int s_ListConst_64 = 64;
        /// <summary>消息字典等中等集合初始容量 100。</summary>
        public static readonly int s_ListConst_100 = 100;
        /// <summary>FlatBuffer 构建缓冲等较大缓冲默认字节数 1024。</summary>
        public static readonly int s_ListConst_1024 = 1024;
    }
}
