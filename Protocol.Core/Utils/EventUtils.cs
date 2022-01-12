using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Utils
{
    public delegate void CommonEventHandler<TSender, TEventArgs>(TSender sender, TEventArgs e);
    public delegate Task AsyncCommonEventHandler<TSender, TEventArgs>(TSender sender, TEventArgs e);

    public static class EventUtils
    {
        public static bool InvokeCancelEvent<TSender, TEventArgs>(CommonEventHandler<TSender, TEventArgs> handler, TSender sender, TEventArgs e) => InvokeCancelEvent(handler, sender, e, null);
        public static bool InvokeCancelEvent<TSender, TEventArgs>(CommonEventHandler<TSender, TEventArgs> handler, TSender sender, TEventArgs e, Action<TSender, TEventArgs> actionBeforeEveryInvoke)
        {
            if (handler == null || e == null)
                return false;

            if (e is ICancelEvent eventArgs)
            {
                foreach (CommonEventHandler<TSender, TEventArgs> Method in handler.GetInvocationList())
                {
                    actionBeforeEveryInvoke?.Invoke(sender, e);
                    Method.Invoke(sender, e);
                    if (eventArgs.IsCancelled)
                        return true;
                }
            }
            else
            {
                handler?.Invoke(sender, e);
            }

            return false;
        }
        public static async Task<bool> InvokeCancelEventAsync<TSender, TEventArgs>(AsyncCommonEventHandler<TSender, TEventArgs> handler, TSender sender, TEventArgs e) => await InvokeCancelEventAsync(handler, sender, e, null);
        public static async Task<bool> InvokeCancelEventAsync<TSender, TEventArgs>(AsyncCommonEventHandler<TSender, TEventArgs> handler, TSender sender, TEventArgs e, Action<TSender, TEventArgs> actionBeforeEveryInvoke)
        {
            if (handler == null || e == null)
                return false;

            if (e is ICancelEvent eventArgs)
            {
                foreach (AsyncCommonEventHandler<TSender, TEventArgs> Method in handler.GetInvocationList())
                {
                    actionBeforeEveryInvoke?.Invoke(sender, e);
                    await Method.Invoke(sender, e);
                    if (eventArgs.IsCancelled)
                        return true;
                }
            }
            else
            {
                handler?.Invoke(sender, e);
            }

            return false;
        }

        public static bool InvokeCancelEvent<T>(EventHandler<T> handler, object sender, T e) => InvokeCancelEvent(handler, sender, e, null);
        public static bool InvokeCancelEvent<T>(EventHandler<T> handler, object sender, T e, Action<object,T> actionBeforeEveryInvoke)
        {
            if (handler == null || e == null)
                return false;

            if (e is ICancelEvent eventArgs)
            {
                foreach (EventHandler<T> Method in handler.GetInvocationList())
                {
                    actionBeforeEveryInvoke?.Invoke(sender, e);
                    Method.Invoke(sender, e);
                    if (eventArgs.IsCancelled)
                        return true;
                }
            }
            else
            {
                handler?.Invoke(sender, e);
            }

            return false;
        }
    }
}
