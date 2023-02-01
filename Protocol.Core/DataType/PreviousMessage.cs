using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.DataType
{
    public class PreviousMessage
    {
        public PreviousMessage()
        {
        }

        public PreviousMessage(int messageId, byte[] signature)
        {
            MessageId = messageId;
            Signature = signature;
        }

        public int MessageId { get; set; }
        public byte[] Signature { get; set; }
    }
}
