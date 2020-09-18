using System;

namespace MinecraftProtocol.Auth.Yggdrasil
{
    public enum YggdrasilError
    {
        Unknown,
        BadRequest,
        InvalidResponse,
        UserMigrated,
        InvalidToken,
        TooManyRequest,
        InvalidUsernameOrPassword,
        ServiceUnavailable
    }
}
