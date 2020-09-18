using System;
using System.Net;

namespace MinecraftProtocol.Auth.Yggdrasil
{
    public class YggdrasilException:Exception
    {

        public YggdrasilError Error { get; }
        public HttpStatusCode HttpCode { get; }
        /// <summary> Http Response Content</summary>
        public string Json { get; }

        public YggdrasilException(YggdrasilError error) : base()
        {
            this.Error = error;
        }
        public YggdrasilException(HttpStatusCode httpCode, string json) : base()
        {
            this.HttpCode = httpCode;
            this.Json = json;
        }
        public YggdrasilException(string message, YggdrasilError error) : base(message)
        {
            this.Error = error;
        }
        public YggdrasilException(string message, YggdrasilError error, HttpStatusCode httpCode, string json) : base(message)
        {
            this.Error = error;
            this.HttpCode = httpCode;
            this.Json = json;
        }
        public YggdrasilException(string message, HttpStatusCode httpCode, string json) : base(message)
        {
            this.HttpCode = httpCode;
            this.Json = json;
        }
    }
}
