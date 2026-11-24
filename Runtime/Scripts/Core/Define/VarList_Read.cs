namespace ST.Core
{
    /// <summary>动态命令参数的读取部分，按索引读取常用类型值，类型不匹配时返回对应默认值。</summary>
    public sealed partial class VarList
    {
        /// <summary>读取布尔参数，类型不匹配时返回 false。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的布尔值。</returns>
        public bool GetBool(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return false;

            bool isBool = value is bool;
            if (!isBool)
                return false;

            bool result = (bool)value;
            return result;
        }

        /// <summary>读取 32 位整数参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的整数值。</returns>
        public int GetInt32(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return 0;

            bool isInt32 = value is int;
            if (!isInt32)
                return 0;

            int result = (int)value;
            return result;
        }

        /// <summary>读取 64 位整数参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的整数值。</returns>
        public long GetInt64(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return 0L;

            bool isInt64 = value is long;
            if (!isInt64)
                return 0L;

            long result = (long)value;
            return result;
        }

        /// <summary>读取单精度浮点参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的浮点值。</returns>
        public float GetFloat(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return 0f;

            bool isFloat = value is float;
            if (!isFloat)
                return 0f;

            float result = (float)value;
            return result;
        }

        /// <summary>读取双精度浮点参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的浮点值。</returns>
        public double GetDouble(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return 0.0;

            bool isDouble = value is double;
            if (!isDouble)
                return 0.0;

            double result = (double)value;
            return result;
        }

        /// <summary>读取字符串参数，类型不匹配时返回空字符串。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的字符串。</returns>
        public string GetString(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return string.Empty;

            string result = value as string;
            bool isString = result != null;
            if (!isString)
                result = string.Empty;

            return result;
        }

        /// <summary>读取二进制参数，类型不匹配时返回 null。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的字节数组。</returns>
        public byte[] GetBinary(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return null;

            byte[] result = value as byte[];
            return result;
        }

        /// <summary>读取自定义对象参数，类型不匹配时返回 null。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的对象。</returns>
        public object GetUserData(int index)
        {
            object value;
            bool hasValue = TryGetValue(index, out value);
            if (!hasValue)
                return null;

            return value;
        }

        /// <summary>按索引读取原始参数值。</summary>
        /// <param name="index">参数下标。</param>
        /// <param name="value">读取到的值。</param>
        /// <returns>下标有效时返回 true。</returns>
        bool TryGetValue(int index, out object value)
        {
            bool isValid = index >= 0 && index < m_Values.Count;
            if (!isValid)
            {
                value = null;
                return false;
            }

            value = m_Values[index];
            return true;
        }
    }
}
