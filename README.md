# Minecraft Protocol
![](https://github.com/chawolbaka/MinecraftProtocol/workflows/build/badge.svg)  
当前支持的版本：1.7-1.18.2

这个项目是给我的其它软件使用的, 不会兼容全部版本, 不会有完整的MC协议实现，还会经常因为看着自己以前写的太差直接重写导致没法保证向后兼容。    
ps：英文极差，所以你可能会看见各种奇怪的英文。C#只学了一点点(真的非常少, 因为摸鱼非常严重)，里面大量代码都是从别人那边抄来的(来源我记得的话会尽量写在下面的)

### 简单的演示代码
```C#
IPAddress ip;      //服务器IP地址
ushort port;       //服务器端口号
string playerName; //玩家名
SimpleClient simpleClient = new SimpleClient(host, ip, port);
MinecraftClient client = simpleClient.Client;

//监听收到的数据包
client.PacketReceived += (m, args) =>
{
    if (!m.Joined)
        return;

    //如果收到从服务器发送给客户端的聊天信息数据包就反序列化成ChatComponent并输出到命令行
    if (ServerChatMessagePacket.TryRead(args.Packet, out ServerChatMessagePacket scmp))
    {
        string chatJson = scmp.Json;
        if (!string.IsNullOrWhiteSpace(chatJson))
        {
		ChatComponent chatMessage = ChatComponent.Deserialize(chatJson);
		string message = chatMessage.ToString();
		Console.WriteLine(message);
        }
    }
};

//监听登录成功事件
client.LoginSuccess += (m, args) =>
{
    Task.Run(async () =>
    {
        await Task.Delay(0);
        while (Client.Joined)
        {
		//循环监听命令行输入，并作为聊天信息发送至服务器
		string Input = Console.ReadLine();
		await Client.GetPlayer()?.SendMessageAsync(Input);
        }
    });
};

Client.Connect(); //连接到服务器（Tcp握手）
if (Client.Join(playerName)) //进入服务器（发送登录包）
{
    Client.StartListen(); //开始监听数据包（如果不监听PacketReceived事件不会触发）
}
else
{
    Console.WriteLine($"登陆失败");
}
```    
            
### 抄袭列表
https://github.com/Naamloos/Obsidian  
https://github.com/Nsiso/MinecraftOutClient  
https://github.com/ORelio/Minecraft-Console-Client  
https://gist.github.com/csh/2480d14fbbb33b4bbae3  
https://gist.github.com/acapola/d5b940da024080dfaf5f    
https://gist.github.com/games647/2b6a00a8fc21fd3b88375f03c9e2e603  
https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example  
http://www.bouncycastle.org/csharp/  
http://dotnetzip.codeplex.com  
(数据包压缩部分我现在是直接复制了Minecraft-Console-Client这个项目里面的代码,不知道来源是不是这个)  
### 参考资料
https://wiki.vg/Protocol  
https://github.com/bangbang93/minecraft-protocol  
https://github.com/yushijinhun/authlib-injector/wiki
https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.197.pdf    
https://blessing.studio/minecraft-yggdrasil-api-third-party-implementation/  
https://software.intel.com/sites/landingpage/IntrinsicsGuide/#=undefined&cats=Cryptography  
https://www.intel.com/content/dam/doc/white-paper/advanced-encryption-standard-new-instructions-set-paper.pdf  
