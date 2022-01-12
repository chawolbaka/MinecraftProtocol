namespace MinecraftProtocol.Utils
{
    public interface ICancelEvent
    {
        bool IsCancelled { get; }

        /// <summary>
        /// 阻止事件被继续执行下去
        /// </summary>
        void Cancel();
    }
}