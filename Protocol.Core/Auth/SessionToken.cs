using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MinecraftProtocol.Auth
{
    [Serializable]
    public class SessionToken : IEquatable<SessionToken>
    {
        public string AccessToken { get; set; }
        public string ClientToken { get; set; }
        public string PlayerName { get; set; }
        public string PlayerUUID { get; set; }

        public SessionToken() { }
        public SessionToken(string accessToken) : this(accessToken, string.Empty, string.Empty, string.Empty) { }
        /// <param name="accessToken">访问令牌</param>      
        /// <param name="playerUUID">玩家UUID(无符号)</param>
        /// <param name="playerName">玩家名</param>
        /// <param name="clientToken">客户端令牌</param>
        public SessionToken(string accessToken, string playerName, string playerUUID, string clientToken)
        {
            this.AccessToken = accessToken;
            this.PlayerName = playerName;
            this.PlayerUUID = playerUUID;
            this.ClientToken = clientToken;
        }
        public override string ToString() => ToString(Formatting.Indented);
        public string ToString(Formatting formatting) => JsonConvert.SerializeObject(this, formatting);
        public override bool Equals(object obj) => obj is SessionToken se ? se.Equals(this) : false;
        public static bool operator ==(SessionToken left, SessionToken right)
        {
            if (object.ReferenceEquals(left, null))
                return object.ReferenceEquals(right, null);
            else
                return left.Equals(right);
        }
        public static bool operator !=(SessionToken left, SessionToken right) => !(left == right);
        public bool Equals(SessionToken other)
        {
            if (object.ReferenceEquals(this, other))
                return true;
            else
                return other != null &&
                       AccessToken == other.AccessToken &&
                       ClientToken == other.ClientToken &&
                       PlayerName == other.PlayerName &&
                       PlayerUUID == other.PlayerUUID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AccessToken, ClientToken, PlayerName, PlayerUUID);
        }        
    }
}
