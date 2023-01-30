using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MinecraftProtocol.Auth;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Entity;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.Client
{

    /// <summary>
    /// 一个简化的客户端，会通过ServerListPing的结果来创建VanillaClient或ForgeClient，并添加一些简化的操作
    /// </summary>
    public class SimpleClient : MinecraftClient
    {
        public override string ServerHost           { get => Client.ServerHost; }
        public override IPAddress ServerIP          { get => Client.ServerIP; }
        public override ushort ServerPort           { get => Client.ServerPort; }
        public override int CompressionThreshold    { get => Client.CompressionThreshold; set => Client.CompressionThreshold = value; }
        public override int ProtocolVersion         { get => Client.ProtocolVersion; set => Client.ProtocolVersion = value; }


        public override bool Connected =>   Client.Connected;
        public override bool Joined    =>   Client.Joined;

        public override event CommonEventHandler<MinecraftClient, PacketReceivedEventArgs> PacketReceived { add => Client.PacketReceived += value; remove => Client.PacketReceived -= value; }
        public override event CommonEventHandler<MinecraftClient, SendPacketEventArgs> PacketSend { add => Client.PacketSend += value; remove => Client.PacketSend -= value; }
        public override event CommonEventHandler<MinecraftClient, LoginEventArgs> LoginSuccess { add => Client.LoginSuccess += value; remove => Client.LoginSuccess -= value; }
        public override event CommonEventHandler<MinecraftClient, DisconnectEventArgs> Disconnected { add => Client.Disconnected += value; remove => Client.Disconnected -= value; }

        public MinecraftClient Client { get; }

        public SimpleClient(string host, IPAddress serverIP, ushort serverPort)
        {
            ServerListPing slp = new ServerListPing(host, serverIP, serverPort);
            slp.EnableDelayDetect = false;
            slp.EnableDnsRoundRobin = false;

            PingReply PingResult = slp.Send();
            int protocolVersion = PingResult.Version.Protocol;
            
            if (protocolVersion == -1 && !string.IsNullOrWhiteSpace(PingResult.Version.Name))
                protocolVersion = ProtocolVersions.SearchByName(PingResult.Version.Name);

            if (PingResult.Forge == null)
            {
                if (protocolVersion != -1)
                    Client = new VanillaClient(host, serverIP, serverPort, protocolVersion >= ProtocolVersions.V1_12_pre3 ? ClientSettings.Default : ClientSettings.LegacyDefault, protocolVersion);
                else
                    throw new NotSupportedException("无法从ServerListPing中获取到协议号");
            }
            else
            {
                if (PingResult.Forge.ModList == null)
                    throw new NotSupportedException("无法从ServerListPing中获取到ModList");

                if (protocolVersion == -1)              
                    protocolVersion = ProtocolVersions.SearchByName(PingResult.Forge.ModList.First(m => m.Name.ToLower().Trim().StartsWith("minecraft")).Version);
                if (protocolVersion != -1)
                    Client = new ForgeClient(host, serverIP, serverPort, new ModList(PingResult.Forge.ModList), protocolVersion >= ProtocolVersions.V1_12_pre3 ? ClientSettings.Default : ClientSettings.LegacyDefault, protocolVersion);
                else
                    throw new NotSupportedException("无法从ServerListPing中获取到协议号");
            }
        }
        public SimpleClient(string host, ushort serverPort) : this(host, Dns.GetHostEntry(host).AddressList[0], serverPort) { }
        public SimpleClient(string host, IPEndPoint remoteEP) : this(host, remoteEP.Address,(ushort)remoteEP.Port) { }
        public SimpleClient(IPAddress serverIP, ushort serverPort) : this(serverIP.ToString(), serverIP, serverPort) { }
        public SimpleClient(IPEndPoint remoteEP) : this(remoteEP.Address.ToString(), remoteEP.Address, (ushort)remoteEP.Port) { }


        public override bool Connect() => Client.Connect();

        public override bool Join(string playerName) => Client.Join(playerName);
        public virtual bool Join(string email, string password) => Join(email, password, out _);
        public virtual bool Join(string email, string password, out SessionToken token)
        {
            if (Client is VanillaClient vanillaClient)
                return vanillaClient.Join(email, password, out token);
            else
                throw new NotSupportedException();
        }
        public virtual bool Join(SessionToken token)
        {
            if (Client is VanillaClient vanillaClient)
                return vanillaClient.Join(token);
            else
                throw new NotSupportedException();
        }
        public override void Disconnect() => Client.Disconnect();
        
        public override void SendPacket(IPacket packet) => Client.SendPacket(packet);
        public override void StartListen(CancellationTokenSource cancellationToken = default) => Client.StartListen(cancellationToken);
        public override void StopListen() => Client.StopListen();
        
        public override Socket GetSocket() => Client.GetSocket();
        public override Player GetPlayer() => Client.GetPlayer();

        public override string ToString() => Client.ToString();

        public static explicit operator VanillaClient(SimpleClient client) => (VanillaClient)client.Client;
        public static explicit operator ForgeClient(SimpleClient client) => (ForgeClient)client.Client;
    }
}
