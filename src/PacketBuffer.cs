using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SharpNetty
{
    public sealed class PacketBuffer
    {
        // Our packet buffer, holds the assembly of bytes.
        private byte[] _buffer;

        // Holds our current offset in the byte array.
        private int _offset;

        /// <summary>
        ///
        /// </summary>
        /// <param name="packetIndex">Index of the binded packet located on the remote machine</param>
        public PacketBuffer(short packetIndex)
        {
            // Always preallocate 2 bytes by default.
            PreAllocate(2);

            WriteShort(packetIndex);
        }

        public PacketBuffer()
        {
            // Always preallocate 2 bytes by default.
            PreAllocate(2);
        }

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
        /// <param name="value">Byte value to write into the buffer.</param>
        public void WriteByte(byte value)
        {
            _buffer[_offset++] = value;
        }

        /// <summary>
        /// Writes a 64 bit integer into the buffer.
        /// </summary>
        public void WriteLong(long value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            Resize(_buffer.Length + tmp.Length);

            for (int i = 0; i < 8; i++)
            {
                _buffer[_offset++] = tmp[i];
            }
        }

        /// <summary>
        /// Writes a 16 bit integer into the buffer.
        /// </summary>
        public void WriteShort(short value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            Resize(_buffer.Length + tmp.Length);

            for (int i = 0; i < 2; i++)
            {
                _buffer[_offset++] = tmp[i];
            }
        }

        /// <summary>
        /// Writes a string into the buffer.
        /// </summary>
        public void WriteString(string value)
        {
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(value);
            WriteByte((byte)tmp.Length);
            Resize(_buffer.Length + tmp.Length);

            for (int i = 0; i < tmp.Length; i++)
            {
                _buffer[_offset++] = tmp[i];
            }
        }

        /// <summary>
        /// Writes a 32 bit integer into the buffer
        /// </summary>
        public void WriteInteger(int value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            Resize(_buffer.Length + tmp.Length);

            for (int i = 0; i < 4; i++)
            {
                _buffer[_offset++] = tmp[i];
            }
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

        //public int WriteBytes(byte[] bytes, int srcOffset, int length)
        //{
        //    Resize(_buffer.Length + length);

        //    for (int i = 0; i < length; i++)
        //        _buffer[_offset++] = bytes[srcOffset++];

        //    return _buffer.Length;
        //}

        public void WriteBytes(byte[] bytes, int destOffset)
        {
            Resize(_buffer.Length + bytes.Length);

            for (int i = 0; i < bytes.Length; i++)
                _buffer[destOffset++] = bytes[i];
        }

        public void WriteBytes(byte[] bytes)
        {
            Resize(_buffer.Length + bytes.Length);

            for (int i = 0; i < bytes.Length; i++)
                _buffer[_offset++] = bytes[i];
        }

        /// <summary>
        /// Reads a 16 bit integer from the buffer.
        /// </summary>
        public short ReadShort()
        {
            return (short)(_buffer[_offset++] + _buffer[_offset++]);
        }

        /// <summary>
        /// Reads a 32 bit integer from the buffer
        /// </summary>
        public int ReadInteger()
        {
            return _buffer[_offset++] + _buffer[_offset++] + _buffer[_offset++] + _buffer[_offset++];
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
            long value = 0;

            for (int i = 0; i < 8; i++)
            {
                value += (long)_buffer[_offset++];
            }

            return value;
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