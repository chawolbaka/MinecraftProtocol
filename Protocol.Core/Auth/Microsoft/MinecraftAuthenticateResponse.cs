using MinecraftProtocol.Auth.Yggdrasil;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MinecraftProtocol.Auth.Microsoft
{
    public class MinecraftAuthenticateResponse
    {
        /// <summary> this is not the uuid of the account </summary>
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        public async Task<SessionToken> AsSessionTokenAsync()
        {
            using HttpClient hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");
            
            using HttpResponseMessage httpResponse = await hc.GetAsync("https://api.minecraftservices.com/minecraft/profile");
            if (!httpResponse.IsSuccessStatusCode)
                throw new MicrosoftAuthenticationException($"Authentication failed ({httpResponse.StatusCode})");

            JsonNode json = JsonNode.Parse(await httpResponse.Content.ReadAsStringAsync());
            if (json.AsObject().TryGetPropertyValue("errorMessage", out var error))
                throw new YggdrasilException(error.GetValue<string>(), YggdrasilError.Unknown, httpResponse);


            return new SessionToken(AccessToken, json["name"].GetValue<string>(), json["id"].GetValue<string>(), string.Empty);
        }
    }
}
