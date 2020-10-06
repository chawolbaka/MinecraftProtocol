using System;
using System.Collections;
using System.Linq;

namespace MinecraftProtocol.Packets
{
    public interface IPacket
    {
        byte this[int index] { get; set; }

        /// <summary>
        /// 获取数据包的ID
        /// </summary>
        int ID { get; }

        /// <summary>
        /// 获取数据包中Data的长度
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 创建完整的数据包
        /// </summary>
        /// <param name="compress">压缩阚值</param>
        byte[] Pack(int compress);

        /// <summary>
        /// 获取数据包的Data
        /// </summary>
        byte[] ToArray();
    }
}
