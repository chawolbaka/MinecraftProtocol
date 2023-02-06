using System;

namespace MinecraftProtocol.Auth.Microsoft
{
    public class MicrosoftOAuth2Token
    {
        public string Email { get; set; }
        public DateTime Time { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }

        public MicrosoftOAuth2Token(string email)
        {
            Email = email;
            Time = DateTime.Now;
        }

        public MicrosoftOAuth2Token(string email, string accessToken, string refreshToken, int expiresIn) : this(email)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ExpiresIn = expiresIn;
        }
    }
}
