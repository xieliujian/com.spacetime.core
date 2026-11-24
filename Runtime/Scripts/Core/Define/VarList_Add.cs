namespace ST.Core
{
    /// <summary>动态命令参数的追加部分，按加入顺序写入参数值。</summary>
    public sealed partial class VarList
    {
        /// <summary>追加布尔参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddBool(bool value)
        {
            m_Values.Add(value);
            return this;
        }

        /// <summary>追加 32 位整数参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddInt(int value)
        {
            m_Values.Add(value);
            return this;
        }

        /// <summary>追加 64 位整数参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddInt64(long value)
        {
            m_Values.Add(value);
            return this;
        }

        /// <summary>追加单精度浮点参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddFloat(float value)
        {
            m_Values.Add(value);
            return this;
        }

        /// <summary>追加双精度浮点参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddDouble(double value)
        {
            m_Values.Add(value);
            return this;
        }

        /// <summary>追加字符串参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddString(string value)
        {
            m_Values.Add(value);
            return this;
        }

        /// <summary>追加二进制参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddBinary(byte[] value)
        {
            m_Values.Add(value);
            return this;
        }

        /// <summary>追加自定义对象参数。</summary>
        /// <param name="value">参数值。</param>
        /// <returns>当前参数列表，便于链式调用。</returns>
        public VarList AddUserData(object value)
        {
            m_Values.Add(value);
            return this;
        }
    }
}
