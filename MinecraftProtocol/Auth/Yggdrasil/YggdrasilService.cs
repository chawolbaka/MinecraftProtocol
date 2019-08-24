using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MinecraftProtocol.Auth.Yggdrasil
{

    public static class YggdrasilService
    {
        private const string API_AUTH_SERVER_STATUS = "https://authserver.mojang.com/";
        private const string API_SESSION_SERVER_STATUS = "https://sessionserver.mojang.com/";
        private const string API_AUTHENTICATE = "https://authserver.mojang.com/authenticate";
        private const string API_REFRESH = "https://authserver.mojang.com/refresh";
        private const string API_VALIDATE = "https://authserver.mojang.com/validate";
        private const string API_INVALIDATE = "https://authserver.mojang.com/invalidate";
        private const string API_SIGNOUT = "https://authserver.mojang.com/signout";
        private const string API_JOIN = "https://sessionserver.mojang.com/session/minecraft/join";
        private const string API_HAS_JOINED = "https://sessionserver.mojang.com/session/minecraft/hasJoined";

        public static Encoding HttpPostEncoding { get; set; } = Encoding.UTF8;
        public static string UserAgent { get; set; }

        public static bool CheckAuthServerStatus() => GetAuthServerStatus() == "OK";
        public static bool CheckSessionServerStatus() => GetSessionServerStatus() == "OK";
        public static string GetAuthServerStatus()
        {
            HttpClient hc = new HttpClient();
            return JObject.Parse(hc.GetStringAsync(API_AUTH_SERVER_STATUS).Result)["Status"].ToString();
        }
        public static string GetSessionServerStatus()
        {
            HttpClient hc = new HttpClient();
            return JObject.Parse(hc.GetStringAsync(API_SESSION_SERVER_STATUS).Result)["Status"].ToString();
        }

        /// <summary>使用邮箱和密码进行身份验证，如果成功会分配一个新的令牌。</summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static SessionToken Authenticate(string email, string password) => Authenticate(email, password, string.Empty);
        /// <summary>使用邮箱和密码进行身份验证，如果成功会分配一个新的令牌。</summary>
        /// <param name="clientToken">由客户端指定的clientToken，不写会由服务服随机生成一个无符号UUID</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static SessionToken Authenticate(string email, string password, string clientToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            JObject json = new JObject(
                new JProperty("agent",
                new JObject(
                    new JProperty("name", "Minecraft"),
                    new JProperty("version", 1))),
                new JProperty("username", email),
                new JProperty("password", password));

            if (!string.IsNullOrEmpty(clientToken))
                json.Add(new JProperty("clientToken", clientToken));

            var PostResponse = PostJson(API_AUTHENTICATE, json.ToString(Formatting.Indented));
            return GetTokenFormPostResponse(PostResponse.Content, PostResponse.Code);
        }
        /// <summary>吊销原令牌，并颁发一个新的令牌。</summary>
        /// <param name="token">处于暂时失效状态的令牌</param>
        /// <exception cref="ArgumentNullException"/> 
        /// <exception cref="YggdrasilService"/>
        /// <returns>新颁发的令牌</returns>
        public static SessionToken Refresh(SessionToken token, bool selectedProfile=false)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            else if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new ArgumentNullException(nameof(token.AccessToken));

            JObject json = new JObject(new JProperty("accessToken", token.AccessToken));
            if (!string.IsNullOrEmpty(token.ClientToken))
                json.Add(new JProperty("clientToken", token.ClientToken));

            if (selectedProfile)
                json.Add(new JProperty("selectedProfile", new JObject(
                            new JProperty("id", token.PlayerUUID),
                            new JProperty("name", token.PlayerName))));
            var PostResponse = PostJson(API_REFRESH,json.ToString(Formatting.Indented));
            return GetTokenFormPostResponse(PostResponse.Content, PostResponse.Code);
        }
        /// <summary>查询令牌是否处于可用状态</summary>
        /// <exception cref="ArgumentNullException"/>
        public static bool Validate(SessionToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            else if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new ArgumentNullException(nameof(token.AccessToken));

            JObject json = new JObject(new JProperty("accessToken", token.AccessToken));
            if (!string.IsNullOrEmpty(token.ClientToken))
                json.Add(new JProperty("clientToken", token.ClientToken));
            var PostResponse = PostJson(API_VALIDATE, json.ToString(Formatting.Indented));
            return PostResponse.Code == HttpStatusCode.NoContent;
        }
        /// <summary>吊销令牌</summary>
        /// <exception cref="ArgumentNullException"/> 
        public static bool Invalidate(SessionToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            else if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new ArgumentNullException(nameof(token.AccessToken));

            JObject json = new JObject(new JProperty("accessToken", token.AccessToken));
            if (!string.IsNullOrEmpty(token.ClientToken))
                json.Add(new JProperty("clientToken", token.ClientToken));

            var PostResponse = PostJson(API_INVALIDATE, json.ToString(Formatting.Indented));
            return PostResponse.Code == HttpStatusCode.NoContent;
        }
        /// <summary>吊销用户的所有令牌</summary>
        /// <exception cref="ArgumentNullException"/>
        public static bool Signout(string email, string password) => Signout(email, password, out _);
        /// <summary>吊销用户的所有令牌</summary>
        /// <exception cref="ArgumentNullException"/>
        public static bool Signout(string email, string password, out string message)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            JObject json = new JObject(
                new JProperty("username", email),
                new JProperty("password", password));

            var PostResponse = PostJson(API_SIGNOUT, json.ToString(Formatting.Indented));
            message = PostResponse.Content;
            return PostResponse.Code == HttpStatusCode.NoContent;
        }
        /// <summary>申请会话</summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static bool Join(SessionToken token, string serverHash) => Join(token.AccessToken, token.PlayerUUID, serverHash);
        /// <summary>申请会话</summary>
        /// <param name="playerUUID">玩家UUID(无符号)</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static bool Join(string accessToken, string playerUUID, string serverHash)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken));
            if (string.IsNullOrWhiteSpace(playerUUID))
                throw new ArgumentNullException(nameof(playerUUID));
            if (string.IsNullOrWhiteSpace(serverHash))
                throw new ArgumentNullException(nameof(serverHash));

            JObject json = new JObject(
                        new JProperty("accessToken", accessToken),
                        new JProperty("selectedProfile", playerUUID),
                        new JProperty("serverId", serverHash));     
            var PostResponse = PostJson(API_JOIN, json.ToString(Formatting.Indented));
            if(PostResponse.Code==HttpStatusCode.Forbidden&&PostResponse.Content.Contains("Invalid token"))
                throw new YggdrasilException("Invalid token.", YggdrasilError.InvalidToken, PostResponse.Code, PostResponse.Content);
            else if(PostResponse.Code == HttpStatusCode.ServiceUnavailable)
                throw new YggdrasilException("Service unavailable.", YggdrasilError.ServiceUnavailable, PostResponse.Code, PostResponse.Content);
            else
                return PostResponse.Code == HttpStatusCode.NoContent;
        }
        /// <summary>检查会话的有效性</summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static bool HasJoined(string playerName, string serverID) => HasJoined(playerName, serverID, string.Empty, out _);
        /// <summary>检查会话的有效性</summary>
        /// <param name="clientIP">如果不为空就会检查是不是这个IP发送的加入会话请求</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static bool HasJoined(string playerName, string serverID, string clientIP) => HasJoined(playerName, serverID, clientIP, out _);
        /// <summary>检查会话的有效性</summary>
        /// <param name="clientIP">如果不为空就会检查是不是这个IP发送的加入会话请求</param>
        /// <param name="playerInfo">令牌所绑定角色的完整信息</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static bool HasJoined(string playerName, string serverID, string clientIP, out string playerInfo)
        {
            //大概可以验证2分钟内的(随便测的,不准确)
            if (string.IsNullOrWhiteSpace(serverID))
                throw new ArgumentNullException(nameof(serverID));
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentNullException(nameof(playerName));

            HttpClient hc = new HttpClient();
            HttpResponseMessage HttpResponse;
            if (string.IsNullOrWhiteSpace(clientIP))
                HttpResponse = hc.GetAsync($"{API_HAS_JOINED}?username={playerName}&serverId={serverID}").Result;
            else
                HttpResponse = hc.GetAsync($"{API_HAS_JOINED}?username={playerName}&serverId={serverID}&ip={clientIP}").Result;

            if (HttpResponse.StatusCode == HttpStatusCode.OK)
            {
                playerInfo = HttpResponse.Content.ReadAsStringAsync().Result;
                return true;
            }
            else if (HttpResponse.StatusCode == HttpStatusCode.NoContent)
            {
                playerInfo = string.Empty;
                return false;
            }
            else if (HttpResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                throw new YggdrasilException("Service unavailable.", YggdrasilError.ServiceUnavailable,
                    HttpResponse.StatusCode,
                    HttpResponse.Content.ReadAsStringAsync().Result);
            }
            else
            {
                throw new YggdrasilException("Unknown error", YggdrasilError.Unknown,
                    HttpResponse.StatusCode,
                    HttpResponse.Content.ReadAsStringAsync().Result);
            }
        }
        private static SessionToken GetTokenFormPostResponse(string json, HttpStatusCode code)
        {
            if (code == HttpStatusCode.OK)
            {
                JObject ResponseJson = JObject.Parse(json);

                if (ResponseJson["accessToken"] != null &&
                    ResponseJson["clientToken"] != null &&
                    ResponseJson["selectedProfile"]["id"] != null &&
                    ResponseJson["selectedProfile"]["name"] != null)
                {
                    SessionToken token = new SessionToken(
                        accessToken: ResponseJson["accessToken"].ToString(),
                        clientToken: ResponseJson["clientToken"].ToString(),
                        playerUUID: ResponseJson["selectedProfile"]["id"].ToString(),
                        playerName: ResponseJson["selectedProfile"]["name"].ToString());
                    return token;
                }
                else
                    throw new YggdrasilException("Invalid response.", YggdrasilError.InvalidResponse, code, json);
            }
            else if (code == HttpStatusCode.Forbidden)
            {
                if (json.Contains("UserMigratedException"))
                    throw new YggdrasilException("User migrated.", YggdrasilError.UserMigrated, code, json);
                else if (json.Contains("Invalid token"))
                    throw new YggdrasilException("Invalid token.", YggdrasilError.InvalidToken, code, json);
                else
                    throw new YggdrasilException("Invalid email or password.", YggdrasilError.InvalidUsernameOrPassword, code, json);
            }
            else if (code == HttpStatusCode.ServiceUnavailable)
                throw new YggdrasilException("Service unavailable.", YggdrasilError.ServiceUnavailable, code, json);
            else if (code == HttpStatusCode.TooManyRequests)
                throw new YggdrasilException("Too many requests.", YggdrasilError.TooManyRequest, code, json);
            else
                throw new YggdrasilException("Unknown Error.", YggdrasilError.Unknown, code, json);
        }
        private static (string Content, HttpStatusCode Code) PostJson(string url, string json)
        {
            using (HttpClient hc = new HttpClient())
            {
                if (!string.IsNullOrEmpty(UserAgent))
                    hc.DefaultRequestHeaders.Add("UserAgent",UserAgent);
                hc.DefaultRequestHeaders.ConnectionClose = true;
                HttpResponseMessage response = hc.PostAsync(url, new StringContent(json, HttpPostEncoding, "application/json")).Result;
                return (response.Content.ReadAsStringAsync().Result, response.StatusCode);
            }
        }
    }
}
