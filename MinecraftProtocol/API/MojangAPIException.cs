using System;
using System.Text;
using System.Net;

namespace MinecraftProtocol.API
{
    public class MojangAPIException : Exception
    {
        public HttpStatusCode HttpCode { get; }
        public string Json { get; }
        
        public MojangAPIException(string message) : base(message) { }
        public MojangAPIException(string message, Exception innerException) : base(message, innerException) { }
        public MojangAPIException(string message, HttpStatusCode httpCode) : this(message, string.Empty, httpCode) { }
        public MojangAPIException(string message, string json, HttpStatusCode httpCode) : base(message)
        {
            this.HttpCode = httpCode;
            this.Json = json;
        }
    }
}
