using System;

namespace MinecraftProtocol.IO
{
    public class UnhandledExceptionEventArgs : EventArgs
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

        public UnhandledExceptionEventArgs(Exception exception)
        {
            Time = DateTime.Now;
            Exception = exception;
        }
    }
}
