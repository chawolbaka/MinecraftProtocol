using MinecraftProtocol.Auth.Yggdrasil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MinecraftProtocol.Auth.Mojang
{
    public static class MojangAccount
    {
        private const string API_AUTHENTICATE = "https://authserver.mojang.com/authenticate";
        private const string API_REFRESH = "https://authserver.mojang.com/refresh";
        private const string API_VALIDATE = "https://authserver.mojang.com/validate";
        private const string API_INVALIDATE = "https://authserver.mojang.com/invalidate";
        private const string API_SIGNOUT = "https://authserver.mojang.com/signout";

        public static Encoding HttpPostEncoding { get; set; } = Encoding.UTF8;
        public static string UserAgent { get; set; }

        /// <summary>
        /// 使用邮箱和密码进行身份验证，如果成功会分配一个新的令牌。
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static Task<SessionToken> AuthenticateAsync(string email, string password) => AuthenticateAsync(email, password, string.Empty);

        /// <summary>
        /// 使用邮箱和密码进行身份验证，如果成功会分配一个新的令牌。
        /// </summary>
        /// <param name="clientToken">由客户端指定的clientToken，不写会由服务服随机生成一个无符号UUID</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="YggdrasilService"/>
        public static async Task<SessionToken> AuthenticateAsync(string email, string password, string clientToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            JsonObject json = new JsonObject
            {
                ["agent"] = new JsonObject
                {
                    ["name"] = "Minecraft",
                    ["version"] = 1
                },
                ["username"] = email,
                ["password"] = password
            };

            if (!string.IsNullOrEmpty(clientToken))
                json.Add("clientToken", clientToken);

            using var postResponse = await PostJsonAsync(API_AUTHENTICATE, json.ToJsonString());
            return await GetTokenFormPostResponseAsync(postResponse);
        }


        /// <summary>
        /// 吊销原令牌，并颁发一个新的令牌。
        /// </summary>
        /// <param name="token">处于暂时失效状态的令牌</param>
        /// <exception cref="ArgumentNullException"/> 
        /// <exception cref="YggdrasilService"/>
        /// <returns>新颁发的令牌</returns>
        public static async Task<SessionToken> RefreshAsync(SessionToken token, bool selectedProfile = false)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            else if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new ArgumentNullException(nameof(token.AccessToken));

            JsonObject json = new JsonObject { ["accessToken"] = token.AccessToken };

            if (!string.IsNullOrEmpty(token.ClientToken))
                json.Add("clientToken", token.ClientToken);

            if (selectedProfile)
                json.Add("selectedProfile", new JsonObject { ["id"] = token.PlayerUUID, ["name"] = token.PlayerName });


            using var postResponse = await PostJsonAsync(API_REFRESH, json.ToJsonString());
            return await GetTokenFormPostResponseAsync(postResponse);
        }

        /// <summary>
        /// 查询令牌是否处于可用状态
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static async Task<bool> ValidateAsync(SessionToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            else if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new ArgumentNullException(nameof(token.AccessToken));


            JsonObject json = new JsonObject { ["accessToken"] = token.AccessToken };

            if (!string.IsNullOrEmpty(token.ClientToken))
                json.Add("clientToken", token.ClientToken);

            using var postResponse = await PostJsonAsync(API_VALIDATE, json.ToJsonString());
            return postResponse.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// 吊销令牌
        /// </summary>
        /// <exception cref="ArgumentNullException"/> 
        public static async Task<bool> InvalidateAsync(SessionToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            else if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new ArgumentNullException(nameof(token.AccessToken));


            JsonObject json = new JsonObject { ["accessToken"] = token.AccessToken };

            if (!string.IsNullOrEmpty(token.ClientToken))
                json.Add("clientToken", token.ClientToken);

            var postResponse = await PostJsonAsync(API_INVALIDATE, json.ToJsonString());
            return postResponse.StatusCode == HttpStatusCode.NoContent;
        }


        /// <summary>
        /// 吊销用户的所有令牌
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static async Task<(bool IsSuccess, string message)> SignoutAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            JsonObject json = new JsonObject
            {
                ["username"] = email,
                ["password"] = password
            };

            using var postResponse = await PostJsonAsync(API_SIGNOUT, json.ToJsonString());
            return (postResponse.StatusCode == HttpStatusCode.NoContent, await postResponse.Content.ReadAsStringAsync());
        }

        private static async Task<SessionToken> GetTokenFormPostResponseAsync(HttpResponseMessage httpResponse)
        {
            if (httpResponse is null)
                throw new ArgumentNullException(nameof(httpResponse));


            string rawJson = await httpResponse.Content.ReadAsStringAsync();
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                JsonNode ResponseJson = JsonNode.Parse(rawJson);

                if (ResponseJson["accessToken"] != null &&
                    ResponseJson["clientToken"] != null &&
                    ResponseJson["selectedProfile"] != null &&
                    ResponseJson["selectedProfile"]["id"] != null &&
                    ResponseJson["selectedProfile"]["name"] != null)
                {
                    SessionToken token = new SessionToken(
                        accessToken: ResponseJson["accessToken"].GetValue<string>(),
                        clientToken: ResponseJson["clientToken"].GetValue<string>(),
                        playerUUID: ResponseJson["selectedProfile"]["id"].GetValue<string>(),
                        playerName: ResponseJson["selectedProfile"]["name"].GetValue<string>());
                    return token;
                }
                else
                    throw new YggdrasilException("Invalid response.", YggdrasilError.InvalidResponse, httpResponse);
            }
            else if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                if (rawJson.Contains("UserMigratedException"))
                    throw new YggdrasilException("User migrated.", YggdrasilError.UserMigrated, httpResponse);
                else if (rawJson.Contains("Invalid token"))
                    throw new YggdrasilException("Invalid token.", YggdrasilError.InvalidToken, httpResponse);
                else
                    throw new YggdrasilException("Invalid email or password.", YggdrasilError.InvalidUsernameOrPassword, httpResponse);
            }
            else if (httpResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new YggdrasilException("Service unavailable.", YggdrasilError.ServiceUnavailable, httpResponse);
            else if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                throw new YggdrasilException("Too many requests.", YggdrasilError.TooManyRequest, httpResponse);
            else
                throw new YggdrasilException("Unknown Error.", YggdrasilError.Unknown, httpResponse);
        }

        private static async Task<HttpResponseMessage> PostJsonAsync(string url, string json)
        {
            using (HttpClient hc = new HttpClient())
            {
                if (!string.IsNullOrEmpty(UserAgent))
                    hc.DefaultRequestHeaders.Add("UserAgent", UserAgent);
                hc.DefaultRequestHeaders.ConnectionClose = true;
                return await hc.PostAsync(url, new StringContent(json, HttpPostEncoding, "application/json"));
            }
        }
    }
}
