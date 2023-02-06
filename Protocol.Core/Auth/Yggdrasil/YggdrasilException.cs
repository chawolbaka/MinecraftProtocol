using System;
using System.Net;
using System.Net.Http;

namespace MinecraftProtocol.Auth.Yggdrasil
{
    public class YggdrasilException:Exception
    {
        public YggdrasilError Error { get; }
        public HttpResponseMessage Response { get; }
       
        /// <summary>
        /// Http Response Content
        /// </summary>
        public string Json { get; }

        public YggdrasilException(YggdrasilError error) : base()
        {
            this.Error = error;
        }

        public YggdrasilException(string message, YggdrasilError error) : base(message)
        {
            this.Error = error;
        }
        public YggdrasilException(string message, YggdrasilError error, HttpResponseMessage response) : base(message)
        {
            this.Error = error;
            this.Response = response;
        }
    }
}
