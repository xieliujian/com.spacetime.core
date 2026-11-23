using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;

namespace ST.Core
{
    /// <summary>
    /// 基于 <see cref="MemoryStream"/> 的二进制读写缓冲，用于网络包体等场景的序列化与反序列化（部分数值采用大端写入以配合协议）。
    /// </summary>
    public class ByteBuffer
    {
        /// <summary>底层内存流。</summary>
        MemoryStream m_Stream;
        /// <summary>写入端（构造为写模式或从空数据创建时使用）。</summary>
        BinaryWriter m_Writer;
        /// <summary>读取端（从已有字节构造时使用）。</summary>
        BinaryReader m_Reader;

        /// <summary>
        /// 创建空缓冲并进入写入模式。
        /// </summary>
        public ByteBuffer()
        {
            m_Stream = new MemoryStream();
            m_Writer = new BinaryWriter(m_Stream);
        }

        /// <summary>
        /// 从已有字节创建缓冲：<paramref name="data"/> 非空时为只读解析模式，否则等同无参构造。
        /// </summary>
        /// <param name="data">可选的初始字节数组</param>
        public ByteBuffer(byte[] data)
        {
            if (data != null)
            {
                m_Stream = new MemoryStream(data);
                m_Reader = new BinaryReader(m_Stream);
            }
            else
            {
                m_Stream = new MemoryStream();
                m_Writer = new BinaryWriter(m_Stream);
            }
        }

        /// <summary>
        /// 关闭读写器并释放底层流引用。
        /// </summary>
        public void Close()
        {
            if (m_Writer != null) 
                m_Writer.Close();

            if (m_Reader != null) 
                m_Reader.Close();

            m_Stream.Close();
            m_Writer = null;
            m_Reader = null;
            m_Stream = null;
        }

        /// <summary>写入单字节。</summary>
        /// <param name="v">字节值</param>
        public void WriteByte(byte v)
        {
            m_Writer.Write(v);
        }

        /// <summary>写入 32 位有符号整数（小端）。</summary>
        /// <param name="v">整数值</param>
        public void WriteInt(int v)
        {
            m_Writer.Write((int)v);
        }
        
        /// <summary>写入 16 位无符号整数（小端）。</summary>
        /// <param name="v">数值</param>
        public void WriteShort(UInt16 v)
        {
            m_Writer.Write((UInt16)v);
        }

        /// <summary>写入 64 位有符号整数（小端）。</summary>
        /// <param name="v">数值</param>
        public void WriteLong(long v)
        {
            m_Writer.Write((long)v);
        }

        /// <summary>写入 64 位无符号整数（小端）。</summary>
        /// <param name="v">数值</param>
        public void WriteUlong(ulong v)
        {
            m_Writer.Write(v);
        }

        /// <summary>写入单精度浮点，按大端字节序写入底层。</summary>
        /// <param name="v">数值</param>
        public void WriteFloat(float v)
        {
            byte[] temp = BitConverter.GetBytes(v);
            Array.Reverse(temp);
            m_Writer.Write(BitConverter.ToSingle(temp, 0));
        }

        /// <summary>写入双精度浮点，按大端字节序写入底层。</summary>
        /// <param name="v">数值</param>
        public void WriteDouble(double v)
        {
            byte[] temp = BitConverter.GetBytes(v);
            Array.Reverse(temp);
            m_Writer.Write(BitConverter.ToDouble(temp, 0));
        }

        /// <summary>先写 2 字节长度（<see cref="ushort"/>），再写 UTF-8 正文。</summary>
        /// <param name="v">字符串，可为 null（按空串处理）</param>
        public void WriteString(string v)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(v);
            m_Writer.Write((ushort)bytes.Length);
            m_Writer.Write(bytes);
        }

        /// <summary>将字节数组全部写入当前流。</summary>
        /// <param name="v">字节数组</param>
        public void WriteBytes(byte[] v)
        {
            m_Writer.Write(v);
        }

        /// <summary>写入字节数组指定区段。</summary>
        /// <param name="buffer">源数组</param>
        /// <param name="index">起始下标</param>
        /// <param name="count">长度</param>
        public void WriteBytes(byte[] buffer, int index, int count)
        {
            m_Writer.Write(buffer, index, count);
        }

        /// <summary>读取单字节。</summary>
        /// <returns>字节值</returns>
        public byte ReadByte()
        {
            return m_Reader.ReadByte();
        }

        /// <summary>读取 32 位有符号整数（小端）。</summary>
        /// <returns>整数值</returns>
        public int ReadInt()
        {
            return (int)m_Reader.ReadInt32();
        }

        /// <summary>读取 16 位无符号整数（小端）。</summary>
        /// <returns>数值</returns>
        public ushort ReadShort()
        {
            return (ushort)m_Reader.ReadInt16();
        }

        /// <summary>读取 64 位有符号整数（小端）。</summary>
        /// <returns>数值</returns>
        public long ReadLong()
        {
            return (long)m_Reader.ReadInt64();
        }

        /// <summary>读取单精度浮点（按大端语义反转后解析）。</summary>
        /// <returns>数值</returns>
        public float ReadFloat()
        {
            byte[] temp = BitConverter.GetBytes(m_Reader.ReadSingle());
            Array.Reverse(temp);
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>读取双精度浮点（按大端语义反转后解析）。</summary>
        /// <returns>数值</returns>
        public double ReadDouble()
        {
            byte[] temp = BitConverter.GetBytes(m_Reader.ReadDouble());
            Array.Reverse(temp);
            return BitConverter.ToDouble(temp, 0);
        }

        /// <summary>先读 2 字节长度，再读对应长度的 UTF-8 字符串。</summary>
        /// <returns>解码后的字符串</returns>
        public string ReadString()
        {
            ushort len = ReadShort();
            byte[] buffer = new byte[len];
            buffer = m_Reader.ReadBytes(len);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>先读 4 字节长度，再读对应长度的字节数组。</summary>
        /// <returns>字节数组</returns>
        public byte[] ReadBytes()
        {
            int len = ReadInt();
            return m_Reader.ReadBytes(len);
        }

        /// <summary>读取指定长度的字节数组。</summary>
        /// <param name="len">字节数</param>
        /// <returns>字节数组</returns>
        public byte[] ReadBytes(int len)
        {
            return m_Reader.ReadBytes(len);
        }

        /// <summary>刷新写入器并返回当前流中的全部字节副本。</summary>
        /// <returns>完整缓冲内容</returns>
        public byte[] ToBytes()
        {
            m_Writer.Flush();
            return m_Stream.ToArray();
        }

        /// <summary>将写入器缓冲刷入内存流。</summary>
        public void Flush()
        {
            m_Writer.Flush();
        }
    }
}