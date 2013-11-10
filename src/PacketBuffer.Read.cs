using System;

using System.IO;

namespace SharpNetty
{
    public class PacketBuffer : BinaryReader
    {
        public PacketBuffer(MemoryStream input)
            : base(input)
        {
        }
    }
}