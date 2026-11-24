namespace ST.Core
{
    /// <summary>动态命令参数的运算符追加部分，运算符左侧为参数列表，右侧为要追加的值。</summary>
    public sealed partial class VarList
    {
        /// <summary>追加布尔参数。</summary>
        public static VarList operator <(VarList list, bool value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddBool(value);
            return result;
        }

        /// <summary>追加 32 位整数参数。</summary>
        public static VarList operator <(VarList list, int value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddInt(value);
            return result;
        }

        /// <summary>追加 64 位整数参数。</summary>
        public static VarList operator <(VarList list, long value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddInt64(value);
            return result;
        }

        /// <summary>追加单精度浮点参数。</summary>
        public static VarList operator <(VarList list, float value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddFloat(value);
            return result;
        }

        /// <summary>追加双精度浮点参数。</summary>
        public static VarList operator <(VarList list, double value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddDouble(value);
            return result;
        }

        /// <summary>追加字符串参数。</summary>
        public static VarList operator <(VarList list, string value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddString(value);
            return result;
        }

        /// <summary>追加二进制参数。</summary>
        public static VarList operator <(VarList list, byte[] value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddBinary(value);
            return result;
        }

        /// <summary>追加布尔参数。</summary>
        public static VarList operator >(VarList list, bool value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddBool(value);
            return result;
        }

        /// <summary>追加 32 位整数参数。</summary>
        public static VarList operator >(VarList list, int value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddInt(value);
            return result;
        }

        /// <summary>追加 64 位整数参数。</summary>
        public static VarList operator >(VarList list, long value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddInt64(value);
            return result;
        }

        /// <summary>追加单精度浮点参数。</summary>
        public static VarList operator >(VarList list, float value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddFloat(value);
            return result;
        }

        /// <summary>追加双精度浮点参数。</summary>
        public static VarList operator >(VarList list, double value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddDouble(value);
            return result;
        }

        /// <summary>追加字符串参数。</summary>
        public static VarList operator >(VarList list, string value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddString(value);
            return result;
        }

        /// <summary>追加二进制参数。</summary>
        public static VarList operator >(VarList list, byte[] value)
        {
            bool isListNull = list == null;
            if (isListNull)
                return null;

            VarList result = list.AddBinary(value);
            return result;
        }
    }
}
