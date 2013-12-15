using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SharpNetty
{
    public sealed class DataBuffer
    {
        // Our packet buffer, holds the assembly of bytes.
        private byte[] _buffer;

        // Holds our current offset in the byte array.
        private int _offset;

        /// <summary>
        ///
        /// </summary>
        /// <param name="packetIndex">Index of the binded packet located on the remote machine</param>
        public DataBuffer(short packetIndex)
        {
            // Always preallocate 2 bytes by default.
            PreAllocate(2);

            WriteShort(packetIndex);
        }

        /// <summary>
        /// Get the current read/write offset in the buffer.
        /// </summary>
        /// <returns>Int value that represents the read/write offset in the buffer.</returns>
        public int GetOffset()
        {
            return _offset;
        }

        public DataBuffer()
        {
            // Always preallocate 2 bytes by default.
            PreAllocate(2);
        }

        /// <summary>
        /// Preallocates the buffer's size to a specified value.
        /// </summary>
        /// <param name="size">Value that specifies the size of the buffer.</param>
        public void PreAllocate(int size)
        {
            _buffer = new byte[size];
        }

        public void SetOffset(int value)
        {
            _offset = value;
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
            Resize(_offset + 1);

            _buffer[_offset++] = value;
        }

        /// <summary>
        /// Writes a 64 bit integer into the buffer.
        /// </summary>
        /// <param name="value">Int-64 value to be written into the buffer.</param>
        public void WriteLong(long value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            Resize(_offset + tmp.Length);

            for (int i = 0; i < 8; i++)
            {
                _buffer[_offset++] = tmp[i];
            }
        }

        /// <summary>
        /// Writes a 16 bit integer into the buffer.
        /// </summary>
        /// <param name="value">Int-16 value to be written into the buffer.</param>
        public void WriteShort(short value)
        {
            Resize(_offset + 2);

            _buffer[_offset++] = (byte)(value);
            _buffer[_offset++] = (byte)(value >> 8);
        }

        /// <summary>
        /// Writes a string into the buffer.
        /// </summary>
        /// <param name="value">String value to be written into the buffer.</param>
        public void WriteString(string value)
        {
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(value);
            WriteByte((byte)tmp.Length);
            Resize(_offset + tmp.Length);

            for (int i = 0; i < tmp.Length; i++)
            {
                _buffer[_offset++] = tmp[i];
            }
        }

        /// <summary>
        /// Writes a 32 bit integer into the buffer
        /// </summary>
        /// <param name="value">Int-32 value to be written into the buffer.</param>
        public void WriteInteger(int value)
        {
            Resize(_offset + 4);

            _buffer[_offset++] = (byte)(value);
            _buffer[_offset++] = (byte)(value >> 8);
            _buffer[_offset++] = (byte)(value >> 16);
            _buffer[_offset++] = (byte)(value >> 24);
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
            if (_buffer.Length > _offset)
            {
                byte[] tmp = new byte[_offset];

                for (int i = 0; i < _offset; i++)
                {
                    tmp[i] = _buffer[i];
                }

                return tmp;
            }
            else return _buffer;
        }

        public void FillBuffer(byte[] bytes)
        {
            _buffer = bytes;
            _offset = 0;
        }

        public void WriteBytes(byte[] bytes, int destOffset)
        {
            Resize(_offset + bytes.Length);

            for (int i = 0; i < bytes.Length; i++)
                _buffer[destOffset++] = bytes[i];
        }

        public void WriteBytes(byte[] bytes)
        {
            Resize(_offset + bytes.Length);

            for (int i = 0; i < bytes.Length; i++)
                _buffer[_offset++] = bytes[i];
        }

        /// <summary>
        /// Reads a 16 bit integer from the buffer.
        /// </summary>
        public short ReadShort()
        {
            return (short)(_buffer[_offset++] | _buffer[_offset++] << 8);
        }

        /// <summary>
        /// Reads a 32 bit integer from the buffer
        /// </summary>
        public int ReadInteger()
        {
            return (int)(_buffer[_offset++] | _buffer[_offset++] << 8 | _buffer[_offset++] << 16 | _buffer[_offset++] << 24);
        }

        /// <summary>
        /// Reads a byte from the buffer
        /// </summary>
        public byte ReadByte()
        {
            return _buffer[_offset++];
        }

        /// <summary>
        /// Reads a string from the buffer
        /// </summary>
        public string ReadString()
        {
            byte size = ReadByte();
            byte[] tmpData = new byte[size];

            for (int i = 0; i < size; i++)
            {
                tmpData[i] = _buffer[_offset++];
            }

            return System.Text.ASCIIEncoding.ASCII.GetString(tmpData).TrimEnd('\0').TrimStart('\0');
        }

        /// <summary>
        /// Reads a 64 bit integer from the buffer
        /// </summary>
        public long ReadLong()
        {
            long value = BitConverter.ToInt64(_buffer, _offset);

            _offset += 8;

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
        public void CompressPacket(CompressionLevel compressionLevel)
        {
            MemoryStream stream = new MemoryStream();
            GZipStream gStream = new GZipStream(stream, compressionLevel);
            gStream.Write(_buffer, 0, _buffer.Length);
            gStream.Close();
            _buffer = stream.ToArray();
            stream.Close();
            _offset = _buffer.Length;
        }

        public void DecompressPacket(CompressionLevel compressionLevel)
        {
            MemoryStream stream = new MemoryStream();
            GZipStream gStream = new GZipStream(stream, compressionLevel);
            gStream.Read(_buffer, 0, _buffer.Length);
            _buffer = stream.ToArray();
            gStream.Close();
            stream.Close();
        }

        private void Resize(int newSize)
        {
            if (newSize < _buffer.Length) return;

            byte[] tmp = new byte[_buffer.Length];

            while (newSize >= tmp.Length)
                tmp = new byte[(tmp.Length << 1)];

            for (int i = 0; i < _buffer.Length; i++)
            {
                tmp[i] = _buffer[i];
            }

            _buffer = tmp;
        }
    }
}