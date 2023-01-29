using System.Collections.Generic;

namespace MinecraftProtocol.Chat
{
    public interface ITranslation
    {
        string ToString(Dictionary<string, string> lang);
    }
}
