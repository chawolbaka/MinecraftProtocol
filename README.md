# Minecraft Protocol
这个项目是给我的其它软件使用的,所以不会有完整的版本兼容,不会支持所以包。
我其它的软件用到了什么就去研究那部分要怎么写,如果发现写不出来就暂时放置写点其它的试试看。  
  
所以不会去实现完整的MC协议,如果需要使用完整的,兼容性好的请去找其它的,这个写的非常辣鸡！！  
(QAQ所以如果你不小心搜到了不要来看的代码,写的太辣鸡了)  
  
ps:不会排版，英文完全不会，C#只学了一点点(真的非常少,摸鱼非常严重)，里面大量代码的从别人那边抄来的(来源我记得的话会尽量写在下面的)

### Building
[安装.Net Core SDK 2.2](https://www.microsoft.com/net/download/dotnet-core/2.2 "安装.Net Core SDK 2.2")

    git clone https://github.com/chawolbaka/PlayersMonitor.git
    cd MinecraftProtocol\PlayersMonitor
    sudo dotnet publish -c Release
编译好后你可以在: bin\Release\netcoreapp2.2\publish 里面找到文件

## 抄袭项目
https://github.com/Nsiso/MinecraftOutClient  
https://gist.github.com/csh/2480d14fbbb33b4bbae3  
https://github.com/ORelio/Minecraft-Console-Client  
### 引用的开源库
https://www.newtonsoft.com  
http://dotnetzip.codeplex.com  
(数据包压缩部分我现在是直接复制了Minecraft-Console-Client这个项目里面的代码,不知道来源是不是这个)
### 参考资料
https://wiki.vg/Protocol  
https://github.com/bangbang93/minecraft-protocol   