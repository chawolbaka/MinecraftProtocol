using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    /// <summary>
    /// 好像是用于过滤聊天信息的(只针对左下角那块聊天框内的内容,无法过滤右上角的那个通知)
    /// </summary>
    public enum ClientChatMode : int
    {
        /// <summary>显示所有聊天信息</summary>
        Full,
        /// <summary>屏蔽所有玩家说的话(仅显示命令消息)</summary>
        System,
        /// <summary>屏蔽所有聊天信息</summary>
        None
    }
}
