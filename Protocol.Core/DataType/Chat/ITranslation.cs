﻿using System.Collections.Generic;

namespace MinecraftProtocol.DataType.Chat
{
    public interface ITranslation
    {
        string ToString(Dictionary<string, string> lang);
    }
}
