using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO.Pools
{
    public interface IPool<T>
    {
        T Rent();
        void Return(T obj);
    }
}
