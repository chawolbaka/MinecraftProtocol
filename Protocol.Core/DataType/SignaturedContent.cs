using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.DataType
{
    public class SignaturedContent<T>
    {
        public T Content { get; set; }
        public byte[] Signature { get; set; }

        public SignaturedContent()
        {
        }

        public SignaturedContent(T content, byte[] signature)
        {
            Content = content;
            Signature = signature;
        }
    }
}
