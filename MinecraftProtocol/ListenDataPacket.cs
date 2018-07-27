using System;
using System.Collections.Generic;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.DataType;
using System.Net.Sockets;
using System.Threading;


namespace MinecraftProtocol
{
    public class ListenDataPacket
    {
        public delegate void EventPacketReceived(Packet packet, ConnectionPayload info);
        public event EventPacketReceived PacketReceived;
        public ConnectionPayload CommunicationInfo { get; set; } = new ConnectionPayload();
        public int Times { get; set; } = -1;
        private bool StopListen = false;
        public ListenDataPacket(ConnectionPayload info)
        {
            if (info.Session != null)
                this.CommunicationInfo.Session = info.Session;
            else
                throw new ArgumentNullException($"Param \"{info}.{nameof(info.Session)}\" is null.");
            this.CommunicationInfo.CompressionThreshold = info.CompressionThreshold;
            this.CommunicationInfo.ProtocolVersion = info.ProtocolVersion;
        }
        public void Start()
        {
            StopListen = false;
            while (!StopListen&&Times!=0)
            {
                int PacketLength = ProtocolHandler.GetPacketLength(this.CommunicationInfo.Session);
                byte[] Packet = new byte[PacketLength];
                Receive(Packet, 0, PacketLength, SocketFlags.None);
                List<byte> data = new List<byte>(Packet);
                if (this.CommunicationInfo.CompressionThreshold == -1)
                {
                    int PacketID = VarInt.Read(Packet, 0, out int end);
                    data.RemoveRange(0, end);
                    if (CommunicationInfo.ProtocolVersion >= ProtocolVersionNumbers.V1_8 && PacketID == 0x03 && data.Count !=16)
                    {
                        this.CommunicationInfo.CompressionThreshold = VarInt.Read(data.ToArray());
                        CommunicationInfo.CompressionThreshold = this.CommunicationInfo.CompressionThreshold;
                        #if DEBUG == true
                        Console.WriteLine("数据包压缩已启动:" + this.CommunicationInfo.CompressionThreshold);
                        #endif
                    }
                    PacketReceived(new Packet(PacketID, data),CommunicationInfo);
                }
                else
                {
                    int DataLength = VarInt.Read(Packet, 0, out int end);//Read Field:DataLength
                    data.RemoveRange(0, end);//Remove Field:DataLength
                    if (DataLength == 0)
                    {
                        int PacketID = VarInt.Read(data.ToArray(), 0, out end);
                        data.RemoveRange(0, end);
                        PacketReceived(new Packet(PacketID, data), CommunicationInfo);
                    }
                    else
                    {
                        data = new List<byte>(ZlibUtils.Decompress(data.ToArray(),DataLength));
                        int PacketID = VarInt.Read(data.ToArray(), 0, out end);
                        data.RemoveRange(0, end);
                        PacketReceived(new Packet(PacketID, data), CommunicationInfo);
                    }

                }
                if (Times != 0||Times!=-1) Times--;
            }
        }
        public void StartThread()
        {
            Thread ListenThread = new Thread(Start);
            ListenThread.Start();
        }
        public void Stop() => StopListen = true;
        private int Receive(byte[] buffer, int start, int offset, SocketFlags flags)
        {
            int read = 0;
            while (read < offset)
            {
                //我看不懂这个.这是在读取所以数据吗
                try
                {
                    read += this.CommunicationInfo.Session.Client.Receive(buffer, start + read, offset - read, flags);
                }
                catch (SocketException se)
                {
                    //有毒呀,linux的timeout居然是110,然后不是硬编码呀, .net core在linux那边不改的吗
                    if (se.ErrorCode == (int)SocketError.TimedOut || se.ErrorCode == 110)
                    {
                        Console.WriteLine("Error:Receive Time out");
                        Console.WriteLine("Please Restart or Wait.");
                        return -1;
                    }
                    else
                        throw se;
                }
            }
            return 0;
        }
    }
}
