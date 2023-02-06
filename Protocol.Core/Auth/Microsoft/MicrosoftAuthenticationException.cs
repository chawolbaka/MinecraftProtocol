using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Auth.Microsoft
{
    public class MicrosoftAuthenticationException : Exception
    {
        public MicrosoftAuthenticationException()
        {
        }

        public MicrosoftAuthenticationException(string message) : base(message)
        {
        }

        public MicrosoftAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MicrosoftAuthenticationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
