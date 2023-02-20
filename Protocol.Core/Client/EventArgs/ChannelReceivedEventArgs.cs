using MinecraftProtocol.IO;
using System;

namespace MinecraftProtocol.Client
{
    public class ChannelReceivedEventArgs : PacketEventArgs
    {
        /// <summary>
        /// 读取频道的消息（如果需要连续读取请存储Reader，每次get都是全新的ByteReader）
        /// </summary>
        public ByteReader Reader => new ByteReader(_data);

        private byte[] _data;

        public ChannelReceivedEventArgs(byte[] data) : this(data, DateTime.Now) { }
        public ChannelReceivedEventArgs(byte[] data, DateTime time) : base(time)
        {
            _data = data;
        }
    }
}
