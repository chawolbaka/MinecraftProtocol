using MinecraftProtocol.Utils;
using System;
using System.Net.Sockets;

namespace MinecraftProtocol.IO.Pools
{
    public class SocketAsyncEventArgsPool : ObjectPool<SocketAsyncEventArgs>
    {
        private const int Configuring = -1;
        private const int Free = 0;
        private const int InProgress = 1;
        private const int Disposed = 2;

        private static Func<SocketAsyncEventArgs, int> GetField_operating = ExpressionTreeUtils.CreateGetFieldMethodFormInstance<SocketAsyncEventArgs, int>("_operating");
        public override void Return(SocketAsyncEventArgs item)
        {
            int operating = GetField_operating(item);
            if (operating == Free && item != null)
            {
                base.Return(item);
            }
            else
            {
                item?.Dispose();
            }
        }
        //public override SocketAsyncEventArgs Rent()
        //{
        //    if(!_objects.TryTake(out SocketAsyncEventArgs item))
        //        return new SocketAsyncEventArgs();

        //    int operating = GetField_operating(item);
        //    if (operating is InProgress or Configuring)
        //    {
        //        _objects.Add(item);
        //        return new SocketAsyncEventArgs();
        //    }
        //    else if (operating == Free)
        //    {
        //        return item;
        //    }
        //    else
        //    {
        //        return new SocketAsyncEventArgs();
        //    }
        //}
    }
}
