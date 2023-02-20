using MinecraftProtocol.Compatible;
using System;
using System.Collections.Generic;

namespace MinecraftProtocol.Packets.Client
{

    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Handshake
    /// </summary>
    public partial class HandshakePacket : DefinedPacket
    {
        //握手包永远不会变ID(大概)
        private const int id = 0x00;
        public enum State : int
        {
            GetStatus = 1,
            Login = 2
        }

        [PacketProperty]
        internal string _serverAddress;
        
        [PacketProperty]
        internal ushort _serverPort;

        [PacketProperty]
        internal State _nextState;

        private int start = 0;
        private int offset = 0;

        /// <summary>
        /// 获取不带着额外参数的服务器地址
        /// </summary>
        public string GetServerAddressOnly()
        {
            if (_serverAddress.Contains('\0'))
                return _serverAddress.Substring(0, _serverAddress.IndexOf('\0'));
            else
                return _serverAddress;
        }

        protected override void CheckProperty()
        {
            if (string.IsNullOrEmpty(_serverAddress))
                throw new ArgumentNullException(nameof(ServerAddress));
        }

        protected override void SetProperty(string propertyName, object newValue)
        {
            //这边仅用于演示如何重写该方法，实际上删了效率上也没多大差距（甚至这写法放在这个方法上还有点负优化，后面几个属性写起来也影响不了多少性能）
            if (start > 0 && offset > 0 && nameof(ServerAddress).Equals(propertyName))
            {
                //取出不需要重写的部分
                Span<byte> temp = _data.AsSpan(offset);
                //创建临时数组（不这样子如果新_serverAddress长度变多会覆盖掉，所以必须复制到一个临时数组）
                byte[] sawp = new byte[temp.Length];
                temp.CopyTo(sawp.AsSpan());
                //跳过不需要重写的部分
                _size = start;
                //重写新数据
                WriteString(newValue.ToString());
                //把之前的数据复制回去并向池归还临时数组
                WriteBytes(sawp);
            }
            else
            {
                //未处理的属性就按默认方法来重写（清空+调用Write）
                //一般上面那种写法仅用于被属性后面的数据重写代价过高且被设置属性经常被修改才需要那样优化
                base.SetProperty(propertyName, newValue);
            }
        }

        protected override void Write()
        {
            WriteVarInt(ProtocolVersion);
            start = _size;
            WriteString(_serverAddress);
            offset = _size;
            WriteUnsignedShort(_serverPort);
            WriteVarInt((int)_nextState);
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            ProtocolVersion = reader.ReadVarInt();
            _serverAddress = reader.ReadString();
            _serverPort = reader.ReadUnsignedShort();
            _nextState = (State)reader.ReadVarInt();
        }

        public static int GetPacketId(int protocolVersion) => id;
        public static int GetPacketId() => id;

    }
}
