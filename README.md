# PlayersMonitor
QAQ不要看这个项目的代码,写的太辣鸡了。
这是我为了学习一下怎么使用git所以丢github上来试试看的(不过应该不会删除,感觉git好好用的呀,可以随便删代码啦qwq)

### Building
[安装.Net Core SDK 2.1](https://www.microsoft.com/net/download/dotnet-core/2.1 "安装.Net Core SDK 2.1")

    git clone https://github.com/chawolbaka/PlayersMonitor.git
    cd MinecraftProtocol\PlayersMonitor
    sudo dotnet publish -c Release -r win-x86
编译好后你可以在: bin\Release\netcoreapp2.1\win-x86\publish 里找到"PlayersMonitor.exe" 
(其它文件无法删除,如果需要单文件编译请使用:https://github.com/dotnet/corert)

其它平台的编译请参考这个文档,把 -r 后面的参数改成对应的平台.
https://docs.microsoft.com/en-us/dotnet/core/rid-catalog

## 抄袭项目
https://github.com/Nsiso/MinecraftOutClient
https://github.com/ORelio/Minecraft-Console-Client
### 参考资料
https://wiki.vg/Protocol
https://github.com/bangbang93/minecraft-protocol
