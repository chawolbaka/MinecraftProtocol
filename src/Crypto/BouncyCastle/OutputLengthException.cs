using System;

namespace BouncyCastle.Crypto
{
#if !(NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE)
    [Serializable]
#endif
#pragma warning disable CA2229 // Implement serialization constructors
    public class OutputLengthException : DataLengthException
#pragma warning restore CA2229 // Implement serialization constructors
    {
        public OutputLengthException() { }
        public OutputLengthException(string message): base(message) { }
        public OutputLengthException(string message, Exception exception): base(message, exception) { }
    }
}
