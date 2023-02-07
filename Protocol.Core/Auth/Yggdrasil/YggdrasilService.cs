using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MinecraftProtocol.Auth.Yggdrasil
{

    public static class YggdrasilService
    {
        private const string API_AUTH_SERVER_STATUS = "https://authserver.mojang.com/";
        private const string API_SESSION_SERVER_STATUS = "https://sessionserver.mojang.com/";
        private const string API_JOIN = "https://sessionserver.mojang.com/session/minecraft/join";
        private const string API_HAS_JOINED = "https://sessionserver.mojang.com/session/minecraft/hasJoined";

        public static Encoding HttpPostEncoding { get; set; } = Encoding.UTF8;
        public static string UserAgent { get; set; }

        public static async Task<bool> IsAuthServerAvailable() => JsonNode.Parse(await GetAuthServerStatus())["Status"].GetValue<string>() == "OK";
        public static async Task<bool> IsSessionAvailable() => JsonNode.Parse(await GetSessionServerStatus())["Status"].GetValue<string>() == "OK";
        public static async Task<string> GetAuthServerStatus()
        {
            using HttpClient hc = new HttpClient();
            return await hc.GetStringAsync(API_AUTH_SERVER_STATUS);
        }
        public static async Task<string> GetSessionServerStatus()
        {
            using HttpClient hc = new HttpClient();
            return await hc.GetStringAsync(API_SESSION_SERVER_STATUS);
        }



        /// <summary>
        /// 申请会话
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static Task<bool> JoinAsync(SessionToken token, string serverHash) => JoinAsync(token.AccessToken, token.PlayerUUID, serverHash);
        
        /// <summary>
        /// 申请会话
        /// </summary>
        /// <param name="playerUUID">玩家UUID(无符号)</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static async Task<bool> JoinAsync(string accessToken, string playerUUID, string serverHash)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken));
            if (string.IsNullOrWhiteSpace(playerUUID))
                throw new ArgumentNullException(nameof(playerUUID));
            if (string.IsNullOrWhiteSpace(serverHash))
                throw new ArgumentNullException(nameof(serverHash));

            JsonObject json = new JsonObject
            {
                ["accessToken"] = accessToken,
                ["selectedProfile"] = playerUUID,
                ["serverId"] = serverHash
            };

            using var postResponse = await PostJsonAsync(API_JOIN, json.ToJsonString());
            if (postResponse.StatusCode == HttpStatusCode.Forbidden && (await postResponse.Content.ReadAsStringAsync()).Contains("Invalid token"))
                throw new YggdrasilException("Invalid token.", YggdrasilError.InvalidToken, postResponse);
            else if(postResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new YggdrasilException("Service unavailable.", YggdrasilError.ServiceUnavailable, postResponse);
            else
                return postResponse.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// 检查会话的有效性
        /// </summary>
        /// <param name="playerName">不区分大小写的玩家名</param>
        /// <param name="serverHash">由serverID、secretKey、publicKey组合起来的hash</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        /// <returns>如果会话有效那么就返回令牌所绑定角色的完整信息，否则为null</returns>
        public static Task<string> HasJoinedAsync(string playerName, string serverHash) => HasJoinedAsync(playerName, serverHash, string.Empty);

        /// <summary>
        /// 检查会话的有效性
        /// </summary>
        /// <param name="playerName">不区分大小写的玩家名</param>
        /// <param name="serverHash">由serverID、secretKey、publicKey组合起来的hash</param>
        /// <param name="clientIP">如果不为空就会检查是不是这个IP发送的加入会话请求</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        /// <returns>如果会话有效那么就返回令牌所绑定角色的完整信息，否则为null</returns>
        public static async Task<string> HasJoinedAsync(string playerName, string serverHash, string clientIP)
        {
            //大概可以验证2分钟内的(随便测的,不准确)
            if (string.IsNullOrWhiteSpace(serverHash))
                throw new ArgumentNullException(nameof(serverHash));
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentNullException(nameof(playerName));

            using HttpClient hc = new HttpClient();
            using HttpResponseMessage HttpResponse = await hc.GetAsync($"{API_HAS_JOINED}?username={playerName}&serverId={serverHash}{(!string.IsNullOrWhiteSpace(clientIP)? $"&ip={clientIP}" : "")}");

            if (HttpResponse.StatusCode == HttpStatusCode.OK)
                return await HttpResponse.Content.ReadAsStringAsync();
            else if (HttpResponse.StatusCode == HttpStatusCode.NoContent)
                return null;
            else if (HttpResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new YggdrasilException("Service unavailable.", YggdrasilError.ServiceUnavailable, HttpResponse);
            else
                throw new YggdrasilException("Unknown error", YggdrasilError.Unknown, HttpResponse);
        }

        private static async Task<HttpResponseMessage> PostJsonAsync(string url, string json)
        {
            using (HttpClient hc = new HttpClient())
            {
                if (!string.IsNullOrEmpty(UserAgent))
                    hc.DefaultRequestHeaders.Add("UserAgent",UserAgent);
                hc.DefaultRequestHeaders.ConnectionClose = true;
                return await hc.PostAsync(url, new StringContent(json, HttpPostEncoding, "application/json"));
            }
        }
    }
}
