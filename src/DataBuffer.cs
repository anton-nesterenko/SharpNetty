using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SharpNetty
{
    public sealed class DataBuffer
    {
        // Our packet buffer, holds the assembly of bytes.
        private byte[] m_buffer;

        // Holds our current offset in the byte array.
        private int m_offset;

        /// <summary>
        ///
        /// </summary>
        /// <param name="packetIndex">Index of the binded packet located on the remote machine</param>
        public DataBuffer(short packetIndex)
        {
            // Always preallocate 2 bytes by default.
            this.PreAllocate(2);

            this.WriteShort(packetIndex);
        }

        /// <summary>
        /// Get the current read/write offset in the buffer.
        /// </summary>
        /// <returns>Int value that represents the read/write offset in the buffer.</returns>
        public int GetOffset()
        {
            return m_offset;
        }

        public DataBuffer()
        {
            // Always preallocate 2 bytes by default.
            this.PreAllocate(2);
        }

        /// <summary>
        /// Preallocates the buffer's size to a specified value.
        /// </summary>
        /// <param name="size">Value that specifies the size of the buffer.</param>
        public void PreAllocate(int size)
        {
            m_buffer = new byte[size];
        }

        public void SetOffset(int value)
        {
            m_offset = value;
        }

        public void Flush()
        {
            PreAllocate(2);
            SetOffset(0);
        }

        /// <summary>
        /// Writes a byte into the buffer.
        /// </summary>
        /// <param name="value">Byte value to be written into the buffer.</param>
        public void WriteByte(byte value)
        {
            this.Resize(m_offset + 1);

            m_buffer[m_offset++] = value;
        }

        /// <summary>
        /// Writes a 64 bit integer into the buffer.
        /// </summary>
        /// <param name="value">Int-64 value to be written into the buffer.</param>
        public void WriteLong(long value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            this.Resize(m_offset + tmp.Length);

            for (int i = 0; i < 8; i++)
            {
                m_buffer[m_offset++] = tmp[i];
            }
        }

        /// <summary>
        /// Writes a 16 bit integer into the buffer.
        /// </summary>
        /// <param name="value">Int-16 value to be written into the buffer.</param>
        public void WriteShort(short value)
        {
            this.Resize(m_offset + 2);

            m_buffer[m_offset++] = (byte)(value);
            m_buffer[m_offset++] = (byte)(value >> 8);
        }

        /// <summary>
        /// Writes a string into the buffer.
        /// </summary>
        /// <param name="value">String value to be written into the buffer.</param>
        public void WriteString(string value)
        {
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(value);
            this.WriteByte((byte)tmp.Length);
            this.Resize(m_offset + tmp.Length);

            for (int i = 0; i < tmp.Length; i++)
            {
                m_buffer[m_offset++] = tmp[i];
            }
        }

        /// <summary>
        /// Writes a 32 bit integer into the buffer
        /// </summary>
        /// <param name="value">Int-32 value to be written into the buffer.</param>
        public void WriteInteger(int value)
        {
            this.Resize(m_offset + 4);

            m_buffer[m_offset++] = (byte)(value);
            m_buffer[m_offset++] = (byte)(value >> 8);
            m_buffer[m_offset++] = (byte)(value >> 16);
            m_buffer[m_offset++] = (byte)(value >> 24);
        }

        /// <summary>
        /// Writes a bool into the buffer.
        /// </summary>
        /// <param name="value">Boolean value to be written into the buffer.</param>

        public void WriteBool(bool value)
        {
            if (value)
                this.WriteByte(1);
            else
                this.WriteByte(0);
        }

        /// <summary>
        /// Grabs the buffer for usage.
        /// </summary>
        /// <returns>A byte array containing the buffer's stored values</returns>
        public byte[] ReadBytes()
        {
            if (m_buffer.Length > m_offset)
            {
                byte[] tmp = new byte[m_offset];

                for (int i = 0; i < m_offset; i++)
                {
                    tmp[i] = m_buffer[i];
                }

                return tmp;
            }
            else return m_buffer;
        }

        public void FillBuffer(byte[] bytes)
        {
            m_buffer = bytes;
            m_offset = 0;
        }

        public void WriteBytes(byte[] bytes, int destOffset)
        {
            this.Resize(m_offset + bytes.Length);

            for (int i = 0; i < bytes.Length; i++)
                m_buffer[destOffset++] = bytes[i];
        }

        public void WriteBytes(byte[] bytes)
        {
            this.Resize(m_offset + bytes.Length);

            for (int i = 0; i < bytes.Length; i++)
                m_buffer[m_offset++] = bytes[i];
        }

        /// <summary>
        /// Reads a 16 bit integer from the buffer.
        /// </summary>
        public short ReadShort()
        {
            return (short)(m_buffer[m_offset++] | m_buffer[m_offset++] << 8);
        }

        /// <summary>
        /// Reads a 32 bit integer from the buffer
        /// </summary>
        public int ReadInteger()
        {
            return (int)(m_buffer[m_offset++] | m_buffer[m_offset++] << 8 | m_buffer[m_offset++] << 16 | m_buffer[m_offset++] << 24);
        }

        /// <summary>
        /// Reads a byte from the buffer
        /// </summary>
        public byte ReadByte()
        {
            return m_buffer[m_offset++];
        }

        /// <summary>
        /// Reads a string from the buffer
        /// </summary>
        public string ReadString()
        {
            byte size = this.ReadByte();
            byte[] tmpData = new byte[size];

            for (int i = 0; i < size; i++)
            {
                tmpData[i] = m_buffer[m_offset++];
            }

            return System.Text.ASCIIEncoding.ASCII.GetString(tmpData).TrimEnd('\0').TrimStart('\0');
        }

        /// <summary>
        /// Reads a 64 bit integer from the buffer
        /// </summary>
        public long ReadLong()
        {
            long value = BitConverter.ToInt64(m_buffer, m_offset);

            m_offset += 8;

            return value;
        }

        public bool ReadBool()
        {
            return (this.ReadByte() == 1);
        }

        /// <summary>
        /// Utilizes Microsoft's GZipStream library to compress the packet's byte array size
        /// </summary>
        /// <param name="compressionLevel">Level of compression peformend on the packet's byte array</param>
        public void CompressPacket(CompressionMode compressionMode)
        {
            MemoryStream stream = new MemoryStream();
            GZipStream gStream = new GZipStream(stream, compressionMode);
            gStream.Write(m_buffer, 0, m_buffer.Length);
            gStream.Close();
            m_buffer = stream.ToArray();
            stream.Close();
            m_offset = m_buffer.Length;
        }

        public void DecompressPacket(CompressionMode compressionMode)
        {
            MemoryStream stream = new MemoryStream();
            GZipStream gStream = new GZipStream(stream, compressionMode);
            gStream.Read(m_buffer, 0, m_buffer.Length);
            m_buffer = stream.ToArray();
            gStream.Close();
            stream.Close();
        }

        private void Resize(int newSize)
        {
            if (newSize < m_buffer.Length) return;

            byte[] tmp = new byte[m_buffer.Length];

            while (newSize >= tmp.Length)
                tmp = new byte[(tmp.Length << 1)];

            for (int i = 0; i < m_buffer.Length; i++)
            {
                tmp[i] = m_buffer[i];
            }

            m_buffer = tmp;
        }
    }
}