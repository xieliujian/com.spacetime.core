using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;

namespace ST.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ByteBuffer
    {
        /// <summary>
        /// 
        /// </summary>
        MemoryStream m_Stream;
        BinaryWriter m_Writer;
        BinaryReader m_Reader;

        /// <summary>
        /// 
        /// </summary>
        public ByteBuffer()
        {
            m_Stream = new MemoryStream();
            m_Writer = new BinaryWriter(m_Stream);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
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
        /// 
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteByte(byte v)
        {
            m_Writer.Write(v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteInt(int v)
        {
            m_Writer.Write((int)v);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteShort(UInt16 v)
        {
            m_Writer.Write((UInt16)v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteLong(long v)
        {
            m_Writer.Write((long)v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteUlong(ulong v)
        {
            m_Writer.Write(v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteFloat(float v)
        {
            byte[] temp = BitConverter.GetBytes(v);
            Array.Reverse(temp);
            m_Writer.Write(BitConverter.ToSingle(temp, 0));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteDouble(double v)
        {
            byte[] temp = BitConverter.GetBytes(v);
            Array.Reverse(temp);
            m_Writer.Write(BitConverter.ToDouble(temp, 0));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteString(string v)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(v);
            m_Writer.Write((ushort)bytes.Length);
            m_Writer.Write(bytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void WriteBytes(byte[] v)
        {
            m_Writer.Write(v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void WriteBytes(byte[] buffer, int index, int count)
        {
            m_Writer.Write(buffer, index, count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            return m_Reader.ReadByte();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int ReadInt()
        {
            return (int)m_Reader.ReadInt32();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ushort ReadShort()
        {
            return (ushort)m_Reader.ReadInt16();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long ReadLong()
        {
            return (long)m_Reader.ReadInt64();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public float ReadFloat()
        {
            byte[] temp = BitConverter.GetBytes(m_Reader.ReadSingle());
            Array.Reverse(temp);
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double ReadDouble()
        {
            byte[] temp = BitConverter.GetBytes(m_Reader.ReadDouble());
            Array.Reverse(temp);
            return BitConverter.ToDouble(temp, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            ushort len = ReadShort();
            byte[] buffer = new byte[len];
            buffer = m_Reader.ReadBytes(len);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            int len = ReadInt();
            return m_Reader.ReadBytes(len);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public byte[] ReadBytes(int len)
        {
            return m_Reader.ReadBytes(len);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            m_Writer.Flush();
            return m_Stream.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Flush()
        {
            m_Writer.Flush();
        }
    }
}