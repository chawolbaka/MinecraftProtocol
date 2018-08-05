using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public class PlayerInfo
    {
        public PlayerInfo()
        {

        }
        public PlayerInfo(string playerName,string uuid)
        {
            Name = playerName;
            UUID = uuid;
        }
        public string UUID { get; set; }//我知道命名规则冲突了呀QAQ,可是Uuid看着好丑的感觉
        public string Name { get; set; }
        public int Blood { get; set; }
    }
}
