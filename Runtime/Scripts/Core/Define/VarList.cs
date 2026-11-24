using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>
    /// 动态命令参数容器，按加入顺序保存常用类型值。
    /// 事件参数属于低频数据，这里通过 <see cref="object"/> 复用同一套读取接口。
    /// 参数追加、读取与运算符的实现分别位于 VarList_Add.cs、VarList_Read.cs、VarList_Operator.cs。
    /// </summary>
    public sealed partial class VarList : IVarList
    {
        /// <summary>按加入顺序保存的参数值。</summary>
        readonly List<object> m_Values = new List<object>(CommonDefine.s_ListConst_8);

        /// <summary>参数个数。</summary>
        public int count
        {
            get { return m_Values.Count; }
        }

        /// <summary>创建一个空的参数列表。</summary>
        /// <returns>新的参数列表实例。</returns>
        public static VarList Create()
        {
            VarList result = new VarList();
            return result;
        }

        /// <summary>清空全部参数。</summary>
        public void Clear()
        {
            m_Values.Clear();
        }
    }
}
