using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public class ServerBasePayload
    {
        /// <summary>
        /// 服务器IP地址
        /// </summary>
        public string ServerIPAddress { get; set; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public ushort ServerPort { get; set; }
    }
}
