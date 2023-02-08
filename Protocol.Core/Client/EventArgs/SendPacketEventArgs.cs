using MinecraftProtocol.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class SendPacketEventArgs : PacketEventArgs
    {
        public virtual IPacket Packet { get; }
            
        public virtual bool IsBlock => _isBlock;
        protected bool _isBlock;

        /// <summary>
        /// 拦截数据包的发送
        /// </summary>
        public virtual void Block()
        {
            _isBlock = true;
        }

        public SendPacketEventArgs(IPacket packet) : this(packet, DateTime.Now) { }
        public SendPacketEventArgs(IPacket packet, DateTime time) : base(time)
        {
            this.Packet = packet;
        }

    }
}
