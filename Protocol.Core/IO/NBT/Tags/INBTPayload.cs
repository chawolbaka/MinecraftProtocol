﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public interface INBTPayload<T>
    {
       T Payload { get; set; }
    }
}
