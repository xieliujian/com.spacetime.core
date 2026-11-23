using System;

namespace ST.Core.IO
{
    /// <summary>
    /// 按位写入器，以直观方式逐位写入数据，用于高效的数据存储。
    /// </summary>
    public class BitStreamWriter
    {
        // ──────────────────────────────────────────
        // 字段
        // ──────────────────────────────────────────

        /// <summary>内部缓冲区。</summary>
        byte[] m_Buf = new byte[65536];

        /// <summary>当前正在构造的字节。</summary>
        byte m_CurrentByte;

        /// <summary>当前字节内已写入的位偏移。</summary>
        int m_CurrentBitPos = 0;

        /// <summary>已写入的完整字节数。</summary>
        int m_CurrentBufPos = 0;

        /// <summary>单位掩码，用于提取最低位。</summary>
        const uint SingleBitMask = 0x00000001;

        // ──────────────────────────────────────────
        // 属性
        // ──────────────────────────────────────────

        /// <summary>内部缓冲区（只读引用，有效范围 [0, Length)）。</summary>
        public byte[] Buffer => m_Buf;

        /// <summary>已写入的完整字节数。</summary>
        public int Length => m_CurrentBufPos;

        // ──────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 将写入状态重置为初始值，以便复用同一实例。
        /// </summary>
        public void Reset()
        {
            m_CurrentByte = 0;
            m_CurrentBitPos = 0;
            m_CurrentBufPos = 0;
        }

        /// <summary>
        /// 向流中写入 <paramref name="bits"/> 个低位。
        /// </summary>
        /// <param name="value">要写入的值。</param>
        /// <param name="bits">写入的位数（0–32）。</param>
        /// <returns>实际写入的位数。</returns>
        public int Write(uint value, int bits)
        {
#if UNITY_EDITOR
            if (bits > 32 || bits < 0)
                throw new SystemException($"Invalid number of bits: {bits}");
#endif
            int bitsWritten = 0;
            for (int i = 0; i < bits; ++i)
            {
                WriteBit((value >> i) & SingleBitMask);
                ++bitsWritten;
            }
            return bitsWritten;
        }

        /// <summary>
        /// 将当前未满字节刷入缓冲区，完成写入。
        /// </summary>
        public void Flush()
        {
            if (m_CurrentBitPos != 0)
                AppendCurrentByte();

            m_CurrentByte = 0;
            m_CurrentBitPos = 0;
        }

        // ──────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────

        /// <summary>向当前字节写入一位；字节满时自动提交并重置。</summary>
        void WriteBit(uint singleBit)
        {
            if (m_CurrentBitPos == 8)
            {
                AppendCurrentByte();
                m_CurrentByte = 0;
                m_CurrentBitPos = 0;
            }

            if (singleBit != 0)
                m_CurrentByte |= (byte)(singleBit << m_CurrentBitPos);

            ++m_CurrentBitPos;
        }

        /// <summary>将当前字节追加到缓冲区并移动指针。</summary>
        void AppendCurrentByte()
        {
            m_Buf[m_CurrentBufPos] = m_CurrentByte;
            ++m_CurrentBufPos;
        }
    }

    /// <summary>
    /// 按位读取器，以直观方式逐位读取数据，用于高效的数据存储。
    /// </summary>
    public class BitStreamReader
    {
        // ──────────────────────────────────────────
        // 字段
        // ──────────────────────────────────────────

        /// <summary>每字节的位数常量。</summary>
        const int NumberOfBitsInByte = 8;

        /// <summary>外部传入的字节缓冲区。</summary>
        byte[] m_Buffer;

        /// <summary>当前字节内剩余可读位数。</summary>
        int m_CurrentBits;

        /// <summary>当前读取的字节下标。</summary>
        int m_BufPos;

        // ──────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 从流中读取 <paramref name="bits"/> 个位并返回对应的无符号整数值。
        /// </summary>
        /// <param name="bits">读取的位数（1–8）。</param>
        public uint Read(int bits)
        {
#if UNITY_EDITOR
            if (bits > 8 || bits <= 0)
                throw new SystemException($"Invalid number of bits: {bits}");
#endif
            uint value = 0;
            switch (bits)
            {
                case 1: value |= ReadBit() << 0; break;
                case 2: value |= ReadBit() << 0 | ReadBit() << 1; break;
                case 3: value |= ReadBit() << 0 | ReadBit() << 1 | ReadBit() << 2; break;
                case 4: value |= ReadBit() << 0 | ReadBit() << 1 | ReadBit() << 2 | ReadBit() << 3; break;
                case 5: value |= ReadBit() << 0 | ReadBit() << 1 | ReadBit() << 2 | ReadBit() << 3 | ReadBit() << 4; break;
                case 6: value |= ReadBit() << 0 | ReadBit() << 1 | ReadBit() << 2 | ReadBit() << 3 | ReadBit() << 4 | ReadBit() << 5; break;
                case 7: value |= ReadBit() << 0 | ReadBit() << 1 | ReadBit() << 2 | ReadBit() << 3 | ReadBit() << 4 | ReadBit() << 5 | ReadBit() << 6; break;
                case 8: value |= ReadBit() << 0 | ReadBit() << 1 | ReadBit() << 2 | ReadBit() << 3 | ReadBit() << 4 | ReadBit() << 5 | ReadBit() << 6 | ReadBit() << 7; break;
            }
            return value;
        }

        /// <summary>
        /// 重置读取状态并绑定新的字节缓冲区。
        /// </summary>
        /// <param name="buffer">要读取的字节数组。</param>
        public void Reset(byte[] buffer)
        {
            m_Buffer = buffer;
            m_BufPos = 0;
            m_CurrentBits = NumberOfBitsInByte;
        }

        // ──────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────

        /// <summary>从当前字节读取一位；字节耗尽时自动移至下一字节。</summary>
        uint ReadBit()
        {
            if (m_CurrentBits == 0)
            {
                ++m_BufPos;
                m_CurrentBits = NumberOfBitsInByte - 1;
                return (uint)(m_Buffer[m_BufPos] >> 0) & 1;
            }

            uint value = (uint)(m_Buffer[m_BufPos] >> (NumberOfBitsInByte - m_CurrentBits)) & 1;
            --m_CurrentBits;
            return value;
        }
    }
}
