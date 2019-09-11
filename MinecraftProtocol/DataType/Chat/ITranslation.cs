using System.Collections.Generic;

namespace MinecraftProtocol.DataType.Chat
{
    /// <summary>
    /// 当字典找不到key时应该拿什么去填充value
    /// </summary>
    public enum TranslationOptions
    {
        /// <summary>用Empty的去填value</summary>
        WriteEmpty,
        /// <summary>用key去填value</summary>
        WriteOriginal,
        /// <summary>用异常信息去填value</summary>
        WriteExceptionMessage,
        /// <summary>抛出异常</summary>
        ThrowException
    }
    public interface ITranslation
    {
#if DEBUG
        public const TranslationOptions DefaultOption = TranslationOptions.ThrowException;
#else
        public const TranslationOptions DefaultOption = TranslationOptions.WriteOriginal;
#endif
        string ToString(Dictionary<string, string> lang);
        string ToString(Dictionary<string, string> lang, TranslationOptions option);
    }
}
