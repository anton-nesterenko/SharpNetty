using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNetty
{
    public class PacketBuffer : BinaryWriter
    {
        public PacketBuffer()
            : base(new MemoryStream())
        {
        }
    }
}