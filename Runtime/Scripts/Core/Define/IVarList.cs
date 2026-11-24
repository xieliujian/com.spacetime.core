namespace ST.Core
{
    /// <summary>
    /// 动态命令参数的只读访问接口，按索引读取常用类型值。
    /// </summary>
    public interface IVarList
    {
        /// <summary>参数个数。</summary>
        int count { get; }

        /// <summary>读取布尔参数，类型不匹配时返回 false。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的布尔值。</returns>
        bool GetBool(int index);

        /// <summary>读取 32 位整数参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的整数值。</returns>
        int GetInt32(int index);

        /// <summary>读取 64 位整数参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的整数值。</returns>
        long GetInt64(int index);

        /// <summary>读取单精度浮点参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的浮点值。</returns>
        float GetFloat(int index);

        /// <summary>读取双精度浮点参数，类型不匹配时返回 0。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的浮点值。</returns>
        double GetDouble(int index);

        /// <summary>读取字符串参数，类型不匹配时返回空字符串。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的字符串。</returns>
        string GetString(int index);

        /// <summary>读取二进制参数，类型不匹配时返回 null。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的字节数组。</returns>
        byte[] GetBinary(int index);

        /// <summary>读取自定义对象参数，类型不匹配时返回 null。</summary>
        /// <param name="index">参数下标。</param>
        /// <returns>读取到的对象。</returns>
        object GetUserData(int index);
    }
}
