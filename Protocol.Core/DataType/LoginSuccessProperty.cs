using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.DataType
{
    public class LoginSuccessProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public byte[] Signature { get; set; }

    }
}
