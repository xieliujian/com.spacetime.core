using System;

namespace ST.Core
{
    /// <summary>
    /// 高性能轻量级列表，以固定数组 + 计数器替代 List&lt;T&gt;，减少频繁堆分配与 GC 压力。
    /// 适用于热路径中需要反复复用同一块内存的场景。
    /// </summary>
    public class RapidList<T>
    {
        // ──────────────────────────────────────────
        // 字段
        // ──────────────────────────────────────────

        /// <summary>内部数组缓冲区。</summary>
        T[] m_Items;

        /// <summary>当前有效元素数量。</summary>
        int m_Count;

        // ──────────────────────────────────────────
        // 属性
        // ──────────────────────────────────────────

        /// <summary>当前有效元素数量。</summary>
        public int Count => m_Count;

        /// <summary>当前有效元素数量（小写别名，兼容旧代码）。</summary>
        public int count => m_Count;

        /// <summary>当前分配的容量。</summary>
        public int Capacity => m_Items.Length;

        /// <summary>按索引访问元素（不做边界检查以保持高性能）。</summary>
        public T this[int index]
        {
            get => m_Items[index];
            set => m_Items[index] = value;
        }

        // ──────────────────────────────────────────
        // 构造
        // ──────────────────────────────────────────

        /// <summary>
        /// 以默认初始容量 16 创建 RapidList。
        /// </summary>
        public RapidList() : this(16) { }

        /// <summary>
        /// 以指定初始容量创建 RapidList。
        /// </summary>
        /// <param name="capacity">初始容量，必须大于 0。</param>
        public RapidList(int capacity)
        {
            m_Items = new T[capacity];
            m_Count = 0;
        }

        // ──────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 向列表末尾追加一个元素；容量不足时自动扩容（翻倍）。
        /// </summary>
        /// <param name="item">要追加的元素。</param>
        public void Add(T item)
        {
            if (m_Count >= m_Items.Length)
                Array.Resize(ref m_Items, m_Items.Length * 2);

            m_Items[m_Count++] = item;
        }

        /// <summary>
        /// 批量追加数组中的所有元素。
        /// </summary>
        /// <param name="items">要追加的元素数组。</param>
        public void AddItems(T[] items)
        {
            if (items == null) return;
            foreach (var item in items)
                Add(item);
        }

        /// <summary>
        /// 将有效元素数量重置为 0，不清除底层数组内容（保留内存以复用）。
        /// </summary>
        public void Clear()
        {
            m_Count = 0;
        }

        /// <summary>
        /// 将有效元素复制到目标数组的起始位置。
        /// </summary>
        /// <param name="array">目标数组，长度须 &gt;= Count。</param>
        public void CopyTo(T[] array)
        {
            Array.Copy(m_Items, array, m_Count);
        }

        /// <summary>
        /// 返回底层数组的引用（长度为 Capacity，有效元素范围为 [0, Count)）。
        /// </summary>
        public T[] GetInternalArray()
        {
            return m_Items;
        }

        /// <summary>
        /// 底层数组的引用（兼容旧代码的别名）。
        /// </summary>
        public T[] buffer => m_Items;

        /// <summary>
        /// 判断有效元素中是否包含指定值。
        /// </summary>
        public bool Contains(T item)
        {
            var comparer = System.Collections.Generic.EqualityComparer<T>.Default;
            for (int i = 0; i < m_Count; i++)
            {
                if (comparer.Equals(m_Items[i], item))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 将有效元素复制到新数组并返回。
        /// </summary>
        public T[] ToArray()
        {
            T[] result = new T[m_Count];
            Array.Copy(m_Items, result, m_Count);
            return result;
        }
    }
}
