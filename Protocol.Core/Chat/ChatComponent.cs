using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinecraftProtocol.Chat
{
    /*
     * (つ∀ ⊂ )感谢这个让我看懂了大概的结构(以前试着写过一次,看着wiki.vg上面的看的一脸懵逼完全脑补不出来Chat的结构)
     * https://github.com/Naamloos/Obsidian/tree/c74ecbc33a4c9aaa714d1021eb7d930b45e78d40/Obsidian/Chat
     */
    [JsonConverter(typeof(ChatComponentConverter))]
    public class ChatComponent
    {
        public bool HasColorCode => !string.IsNullOrEmpty(Color);

        public bool HasFormatCode => Bold || Italic || Underline || Strikethrough || Obfuscated;

        public bool IsSimpleText => !HasColorCode && !HasFormatCode && Extra == null && Translate == null && TranslateParameters == null && HoverEvent == null && ClickEvent == null && Insertion == null;
        
        /// <summary>粗体</summary>
        public bool Bold;

        /// <summary>斜体</summary>
        public bool Italic;

        /// <summary>下划线</summary>
        public bool Underline;

        /// <summary>删除线</summary>
        public bool Strikethrough;

        /// <summary>随机</summary>
        public bool Obfuscated;

        /// <summary>颜色</summary>
        public string Color;

        public string Text;

        public string Insertion;

        public string Translate;

        public List<ChatComponent> TranslateParameters;

        public List<ChatComponent> Extra;

        public EventComponent ClickEvent;

        public EventComponent HoverEvent;

        public ChatComponent() { }
        public ChatComponent(string text) { Text = text; }
        public ChatComponent(string text, ChatColor color) : this(text) { Color = color.ToString(); }
        public ChatComponent(string text, ChatColor color, ChatFormat format) : this(text, format) { Color = color.ToString(); }
        public ChatComponent(string text, ChatFormat format) : this(text)
        {
            if (text.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(text), "使用样式代码的情况下必须要有文字.");
            SetFormat(format);
        }
        public ChatComponent(ReadOnlySpan<char> text) : this(text.ToString()) { }
        public ChatComponent(ReadOnlySpan<char> text, ChatColor color) : this(text.ToString(), color) { }
        public ChatComponent(ReadOnlySpan<char> text, ChatColor color, ChatFormat format) : this(text.ToString(), color, format) { }
        public ChatComponent(ReadOnlySpan<char> text, ChatFormat format) : this(text.ToString(), format) { }

       


        /// <summary>
        /// 把含有样式代码的聊天信息转换为<see cref="ChatComponent"/>
        /// </summary>
        /// <param name="message">含有样式代码的聊天信息(不包含应该也能用)</param>
        /// <param name="sectionSign">分节符号</param>
        /// <param name="compressExtra">清除那些看不见的空格</param>
        public static ChatComponent Parse(string message, char sectionSign = '§', bool compressExtra = true)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));


            if (message[0] != sectionSign && message.Length < 3)
                return new ChatComponent(message);

            ChatComponent result = new ChatComponent(""); //防止空json, 好像MC至少需要有个Text组件
            ChatComponent ChatComponet = new ChatComponent();
            StringBuilder sb = new StringBuilder();
            bool LastIsFormatCode = false; //用于样式代码的叠加，变量名乱写的。

            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == sectionSign && i + 1 < message.Length)
                {
                    //转义符
                    if (i + 2 < message.Length && message[i + 1] == sectionSign && IsFormattingCode(message[i + 2]))
                    {
                        sb.Append(message[++i]);
                        sb.Append(message[++i]);
                    }
                    else if (IsFormattingCode(message[i + 1]))
                    {
                        /*
                         * 如果前面已经有字了但是又没设置过颜色或者样式就先建立一个纯文本的Extra然后清空StringBuilder再设置样式或者颜色
                         * 如果前面已经有字并且设置过颜色或者样式了就开始叠加样式代码和覆盖颜色
                         * &c&n可以叠加,&nxxxx&exxxx不能叠加,它们会被分割成
                         * Extra[0] = &nxxxx
                         * Extra[1] = &exxxx
                         */
                        if (!string.IsNullOrWhiteSpace(sb.ToString()) && !(ChatComponet.HasColorCode || ChatComponet.HasFormatCode) ||
                            !LastIsFormatCode && sb.Length > 0)
                        {
                            ChatComponet.Text = sb.ToString();
                            result.AddExtra(ChatComponet); sb.Clear();
                            ChatComponet = new ChatComponent();
                            LastIsFormatCode = true;
                        }
                        ChatComponet.SetFormat(message[++i]);
                    }
                    else
                    {
                        sb.Append(message[i]);
                    }
                }
                else
                {
                    sb.Append(message[i]);
                    LastIsFormatCode = false;
                }
            }
            if (sb.Length > 0 && ChatComponet.HasFormatCode || !string.IsNullOrWhiteSpace(sb.ToString()))
            {
                ChatComponet.Text = sb.ToString();
                result.AddExtra(ChatComponet);
            }
            if (compressExtra)
                result.CompressExtra();
            return result;
        }

        /// <summary>
        /// 清理Extra里面看不见的空格(尾部的那种)
        /// </summary>
        public void CompressExtra()
        {
            if (Extra == null)
                return;

            //清理纯空格又没指定样式的组件
            for (int i = Extra.Count - 1; i >= 0; i--)
            {
                if (!Extra[i].HasFormatCode && string.IsNullOrWhiteSpace(Extra[i].Text))
                    Extra.RemoveAt(i);
                else
                    break;
            }
            //如果清空了就直接改成null
            if (Extra.Count == 0)
                Extra = null;
            //如果最后一个组件没指定样式就把那个组件的Text后面的空格都清了
            else if (!Extra[Extra.Count - 1].HasFormatCode)
                Extra[Extra.Count - 1].Text = Extra[Extra.Count - 1].Text.TrimEnd();
        }


        public ChatComponent AddExtra(params ChatComponent[] messages) => AddExtra(messages.AsEnumerable());
        public ChatComponent AddExtra(IEnumerable<ChatComponent> messages)
        {
            (Extra ??= new()).AddRange(messages);
            return this;
        }
        public ChatComponent AddExtra(ChatComponent message)
        {
            (Extra ??= new()).Add(message);
            return this;
        }

        public ChatComponent AddTranslateParameters(params ChatComponent[] items) => AddTranslateParameters(items.AsEnumerable());
        public ChatComponent AddTranslateParameters(IEnumerable<ChatComponent> items)
        {
            (TranslateParameters ??= new()).AddRange(items);
            return this;
        }
        public ChatComponent AddTranslateParameter(ChatComponent item)
        {
            (TranslateParameters ??= new()).Add(item);
            return this;
        }


        private static JsonSerializerOptions jsonSerializerOptions = new() { Converters = { new ChatComponentConverter() } };
        private static JsonSerializerOptions writeIndentedJsonSerializerOptions = new() { Converters = { new ChatComponentConverter() }, WriteIndented = true };
        public string Serialize(bool writeIndented = false)
        {
            if (writeIndented)
                return JsonSerializer.Serialize(this, writeIndentedJsonSerializerOptions);
            else
                return JsonSerializer.Serialize(this, jsonSerializerOptions);
        }

        public static ChatComponent Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new ChatComponent();
            if (json[0] == '\"' && json[json.Length - 1] == '\"')
                return new ChatComponent(json.AsSpan().Slice(1, json.Length - 1));
            if (json[0] != '{' && json[json.Length - 1] != '}')
                return new ChatComponent(json);
            else
                return JsonSerializer.Deserialize<ChatComponent>(json, jsonSerializerOptions);
        }


        public override string ToString() => ToString(DefaultTranslation);
        public string ToString(Dictionary<string, string> lang) => ToString(lang, Color, this);
        private string ToString(Dictionary<string, string> lang, string lastColor, ChatComponent lastFormat)
        {
            StringBuilder sb = new StringBuilder();
            if (Bold)           sb.Append("§l");
            if (Italic)         sb.Append("§o");
            if (Underline)      sb.Append("§n");
            if (Strikethrough)  sb.Append("§m");
            if (Obfuscated)     sb.Append("§k");
            if (!string.IsNullOrEmpty(Color)) sb.Append(GetColorCode(Color));
            if (!string.IsNullOrEmpty(Text))  sb.Append(Text);
            if (!string.IsNullOrEmpty(Translate)) ResolveTranslate(Translate, lang, TranslateParameters, sb, lastColor is null ? Color : lastColor, lastFormat is null ? this : lastFormat);
            if (Extra != null && Extra.Count > 0)
            {
                /*
                 * 
                 * 如果一个组件它没指定样式的话就是默认样式,可以直接给所有没指定样式的组件加§r来表达这是默认样式
                 * 可是这样子就会有一堆§r,所以这边只在必要的时候才加§r
                 * 如果前一个指定过了样式并且现在这个是没指定样式的情况下才去添加§r来恢复默认样式
                 * (以我手上的样本只能猜到这种程度了,我猜错了的话请告诉我(1.7.10以下的版本不管))
                 * 
                 * ps:样式代码和颜色代码是独立的（上面写的"样式"把样式和颜色都包括进去了）
                 * 
                 * 比如"§n§eYellow§cRed"它应该是这样子的：
                 *   Yellow(下划线黄色)Red(红色无任何样式)
                 *   
                 * 比如"§n§eYellow§nRed"它应该是这样子的：
                 *   Yellow(下划线黄色)Red(下划线)
                 * 
                 * 对于默认样式的补充: 对于下级组件，在没有指定样式的情况下默认样式就是上一层的样式，所以不能直接加个§r来恢复默认样式，而是需要在递归的时候传递上一层的样式。
                 * lastColor  = 最后一个默认的颜色
                 * lastFormat = 最后一个默认的样式
                 * 这两个应该是最后出现过颜色/样式的一层，也就是说如果下层全是空那就会使用顶层的颜色和样式，如果顶层的颜色也是是空就使用§r，至于样式我暂时不知道该怎么处理。
                 *
                 */

                for (int i = 0; i < Extra.Count; i++)
                {
                    if (i == 0 && !HasFormatCode && lastFormat.HasFormatCode)
                        sb.Append("§r");
                    string buffer = Extra[i].ToString(lang);
                    if (buffer.Length > 0 && i > 0)
                    {
                        if (Extra[i - 1].HasFormatCode && !Extra[i].HasFormatCode)
                            AppendFormatCode(sb.Append("§r"), lastFormat);
                        if (Extra[i - 1].HasColorCode && !Extra[i].HasColorCode)
                            if (!string.IsNullOrEmpty(Color))
                                sb.Append(GetColorCode(Color));
                            else if (!string.IsNullOrEmpty(lastColor))
                                sb.Append(GetColorCode(lastColor));
                            else if (lastFormat != null)
                                AppendFormatCode(sb.Append("§r"), lastFormat);
                            else
                                sb.Append("§r");
                    }
                    sb.Append(buffer);
                }
            }
            return sb.ToString();
        }
        private void ResolveTranslate(string translate, Dictionary<string, string> lang, List<ChatComponent> translateParameters, StringBuilder sb, string lastColor, ChatComponent lastFormat)
        {
            /*
             * 这东西的结构大概是这样的:
             * 首先读取translate,然后从语言文件里面读取对于的值
             * 读取出来的值里面有几个%s就代表后面With数组的长度有多少，然后按顺序去替换掉%s
             * 
             * 例子:
             * 比如"chat.type.announcement"的值是"[%s: %s]"
             * 那么就是
             * 第一个%s取with[0]的内容替换掉
             * 第二个%s取with[1]的内容替换掉
             * 
             */
            string text = lang.ContainsKey(translate) ? lang[translate] : translate;

            //纯翻译,没有%s的那种
            if (translateParameters == null || translateParameters.Count == 0) { sb.Append(text); return; }

            bool RestoreColor = false, RestoreFormat = false;
            int WithCount = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (RestoreFormat)
                {
                    RestoreFormat = false;
                    AppendFormatCode(sb.Append("§r"), lastFormat);
                    if (!string.IsNullOrEmpty(lastColor))
                        sb.Append(GetColorCode(lastColor));

                }
                if (RestoreColor)
                {
                    RestoreColor = false;
                    //如果最后一层有设置过颜色就使用最后一层的颜色，如果没有就恢复到默认色
                    if (!string.IsNullOrEmpty(lastColor))
                        sb.Append(GetColorCode(lastColor));
                    else
                        AppendFormatCode(sb.Append("§r"), lastFormat);//直接加§r可能可能会导致样式的丢失，但这样子写我也不确定会不会恢复到正确的样式
                }

                if (text[i] == '%' && i + 1 != text.Length)
                {
                    //这个%d是在forge的语言文件里面看见的,原版好像没有?
                    if (text[i + 1] == 's' || text[i + 1] == 'd')
                    {
                        AppendWith(translateParameters[WithCount++]);
                        i++;
                    }
                    else if (text[i + 1] == '%')
                    {
                        //%%是转义符,用来表示这是一个百分比号
                        sb.Append('%'); i++;
                    }
                    else if (byte.TryParse(text[i + 1].ToString(), out byte number) && i + 3 != text.Length && text[i + 2] == '$' && text[i + 3] == 's')
                    {
                        //处理类型的: 给予%4$s时长为%5$s秒的%1$s（ID %2$s）*%3$s效果
                        //由于最高我只找到%5所以我不清楚这是16进制还是10进制,我暂时当成10进制处理了
                        //(还是最小只能9的10进制,超过9就解析不到了)
                        AppendWith(translateParameters[number - 1]);
                        i += 3;
                    }
                    else
                    {
                        sb.Append(text[i]);
                    }
                }
                else if (text[i] == '{' && i + 2 < text.Length && int.TryParse(text[i + 1].ToString(), out int index) && index < translateParameters.Count && text[i + 2] == '}')
                {
                    AppendWith(translateParameters[index]);
                    i += 2;
                }
                else
                {
                    sb.Append(text[i]);
                }
            }
            void AppendWith(ChatComponent cm)
            {
                RestoreColor = cm.HasColorCode;
                //第一天: 这么写有点针对性... 明天再想想怎么写的更通用?
                //第二天: 我写的什么东西? 不管啦，反正暂时没什么问题，等有bug了再说！
                RestoreFormat = lastFormat.HasFormatCode && !HasFormatCode && !cm.HasFormatCode;
                sb.Append(cm.ToString(lang, cm.HasColorCode ? null : lastColor, cm.HasFormatCode ? null : lastFormat));
            }
        }

        private static StringBuilder AppendFormatCode(StringBuilder stringBuilder, ChatComponent chatMessage)
        {
            if (chatMessage == null)       return stringBuilder;
            if (chatMessage.Bold)          stringBuilder.Append("§l");
            if (chatMessage.Italic)        stringBuilder.Append("§o");
            if (chatMessage.Underline)     stringBuilder.Append("§n");
            if (chatMessage.Strikethrough) stringBuilder.Append("§m");
            if (chatMessage.Obfuscated)    stringBuilder.Append("§k");
            return stringBuilder;
        }

        public void SetFormat(ChatFormat format)
        {
            Bold = format.HasFlag(ChatFormat.Bold);
            Italic = format.HasFlag(ChatFormat.Italic);
            Underline = format.HasFlag(ChatFormat.Underline);
            Strikethrough = format.HasFlag(ChatFormat.Strikethrough);
            Obfuscated = format.HasFlag(ChatFormat.Obfuscated);
        }

        public void SetStyle(ChatStyle style)
        {
            if (style is null)
                return;

            Bold = style.Bold;
            Italic = style.Italic;
            Underline = style.Underline;
            Strikethrough = style.Strikethrough;
            Obfuscated = style.Obfuscated;
            Color = style.Color;
        }

        private void SetFormat(char code)
        {
            switch (code)
            {
                case 'l': Bold = true; break;
                case 'o': Italic = true; break;
                case 'n': Underline = true; break;
                case 'm': Strikethrough = true; break;
                case 'k': Obfuscated = true; break;
                case '0': Color = "black"; break;
                case '1': Color = "dark_blue"; break;
                case '2': Color = "dark_green"; break;
                case '3': Color = "dark_aqua"; break;
                case '4': Color = "dark_red"; break;
                case '5': Color = "dark_purple"; break;
                case '6': Color = "gold"; break;
                case '7': Color = "gray"; break;
                case '8': Color = "dark_gray"; break;
                case '9': Color = "blue"; break;
                case 'a': Color = "green"; break;
                case 'b': Color = "aqua"; break;
                case 'c': Color = "red"; break;
                case 'd': Color = "light_purple"; break;
                case 'e': Color = "yellow"; break;
                case 'f': Color = "white"; break;
                case 'r': ResetFormat(); return;
                default: throw new InvalidCastException();
            }
        }

        /// <summary>
        /// 重置样式和颜色(不会处理Extra和With里面的)
        /// </summary>
        public void ResetFormat()
        {
            Bold = false;
            Italic = false;
            Underline = false;
            Strikethrough = false;
            Obfuscated = false;
            Color = null;
        }

        /// <summary>
        /// 默认翻译表(只添加了几行英文的英文,如果需要改成其它语言请先使用: ChatMeassge.DefaultTranslation.Clear();
        /// </summary>
        public static Dictionary<string, string> DefaultTranslation { get; } = new Dictionary<string, string>() {

            { "chat.type.text","<%s> %s"},
            { "chat.type.text.narrate","%s says %s"},
            { "chat.type.emote","* %s %s"},
            { "chat.type.announcement","[%s] %s"},
            { "chat.type.admin","[%s: %s]"},
            { "chat.type.advancement.task","%s has made the advancement %s"},
            { "chat.type.advancement.challenge","%s has completed the challenge %s"},
            { "chat.type.advancement.goal","%s has reached the goal %s"},

            { "chat.cannotSend","Cannot send chat message"},

            { "multiplayer.player.joined","%s joined the game" },
            { "multiplayer.player.joined.renamed=","%s (formerly known as %s) joined the game" },
            { "multiplayer.player.left","%s left the game" },

            { "multiplayer.disconnect.authservers_down","Authentication servers are down. Please try again later, sorry!" },
            { "multiplayer.disconnect.banned","You are banned from this server." },
            { "multiplayer.disconnect.duplicate_login","You logged in from another location" },
            { "multiplayer.disconnect.flying","Flying is not enabled on this server" },
            { "multiplayer.disconnect.generic","Disconnected" },
            { "multiplayer.disconnect.idling","You have been idle for too long!" },
            { "multiplayer.disconnect.illegal_characters","Illegal characters in chat" },
            { "multiplayer.disconnect.invalid_entity_attacked","Attempting to attack an invalid entity" },
            { "multiplayer.disconnect.invalid_player_movement","Invalid move player packet received" },
            { "multiplayer.disconnect.invalid_vehicle_movement","Invalid move vehicle packet received" },
            { "multiplayer.disconnect.ip_banned","You have been IP banned." },
            { "multiplayer.disconnect.kicked","Kicked by an operator." },
            { "multiplayer.disconnect.outdated_client","Outdated client! Please use %s" },
            { "multiplayer.disconnect.outdated_server","Outdated server! I'm still on %s" },
            { "multiplayer.disconnect.server_shutdown","Server closed" },
            { "multiplayer.disconnect.slow_login","Took too long to log in" },
            { "multiplayer.disconnect.unverified_username","Failed to verify username!" },

            { "connect.failed","Failed to connect to the server" },

            { "death.attack.player","%1$s was slain by %2$s" },
            { "death.attack.player.item","%1$s was slain by %2$s using %3$s" },

            { "commands.message.sameTarget","You can't send a private message to yourself!" },
            { "commands.message.display.incoming","%s whispers to you: %s" },
            { "commands.message.display.outgoing","You whisper to %s: %s" },
            { "commands.save.start","Saving..."},
            { "commands.save.success","Saved the world"},
            { "commands.save.failed","Saving failed: %s"},
            { "commands.save.flushStart","Flushing all saves..."},
            { "commands.save.flushEnd","Flushing completed"},
            { "commands.stop.start","Stopping the server"},
            { "commands.tp.success","Teleported %s to %s" },
            { "commands.tp.success.coordinates","Teleported %s to %s, %s, %s"},
            { "commands.teleport.success.coordinates","Teleported %s to %s, %s, %s"},
            { "commands.tp.notSameDimension","Unable to teleport because players are not in the same dimension"},
            { "commands.effect.success","Given %1$s (ID %2$s) * %3$s to %4$s for %5$s seconds" }
        };

        /// <summary>
        /// 用于查询存不存在这个样式/颜色代码
        /// </summary>
        public static bool IsFormattingCode(char c)
        {
            return c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c == 'l' || c == 'o' || c == 'n' || c == 'm' || c == 'k' || c == 'r';
        }

        /// <summary>
        /// 把颜色名称转换成§0这样子的代码
        /// </summary>
        private static string GetColorCode(string colorName)
        {
            return colorName switch
            {
                "black" => "§0",
                "dark_blue" => "§1",
                "dark_green" => "§2",
                "dark_aqua" => "§3",
                "dark_red" => "§4",
                "dark_purple" => "§5",
                "gold" => "§6",
                "gray" => "§7",
                "dark_gray" => "§8",
                "blue" => "§9",
                "green" => "§a",
                "aqua" => "§b",
                "red" => "§c",
                "light_purple" => "§d",
                "yellow" => "§e",
                "white" => "§f",
                _ => "",
            };
        }

    }
}
