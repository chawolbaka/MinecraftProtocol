using System;

namespace MinecraftProtocol.IO
{
    public class UnhandledIOExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; }

        /// <summary>
        /// 异常发生的时间
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// 阻止异常被抛出
        /// </summary>
        public bool Handled { get; set; }

        public UnhandledIOExceptionEventArgs(Exception exception)
        {
            Time = DateTime.Now;
            Exception = exception;
        }
    }
}
