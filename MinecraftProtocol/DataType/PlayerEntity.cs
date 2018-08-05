using System;
using System.Collections.Generic;
using System.Text;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.DataType
{
    public class PlayerEntity:PlayerInfo
    {
        public Coords Coords { get; }
        private ConnectionPayload Connect;

        public PlayerEntity(string name, ConnectionPayload connection)
        {
            Name = name;
            Connect = connection;
        }
        /// <summary>
        /// 把这个玩家加入到服务器内
        /// </summary>
        /// <param name="password">如果不写的话就使用离线登陆</param>
        public void Join(string password=null)
        {
            Login login = new Login(Connect.Session,null);
            login.Handshake();
            login.LoginStart(Name);
        }
    }
}
