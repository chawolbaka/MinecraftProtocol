using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MinecraftProtocol.Auth.Microsoft
{
    public static class XboxLive
    {
        private const string API_XBL_AUTHENTICATE = "https://user.auth.xboxlive.com/user/authenticate";
        private const string API_XSTS_AUTHORIZE = "https://xsts.auth.xboxlive.com/xsts/authorize";
        private const string API_XBOX_MINECRAFT_AUTHENTICATE = "https://api.minecraftservices.com/authentication/login_with_xbox";

        public static string UserAgent { get; set; } = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        public static async Task<XboxLiveResponse> XBLAuthenticateAsync(MicrosoftOAuth2Token microsoftOAuth2Response) => await XBLAuthenticateAsync(microsoftOAuth2Response.AccessToken);
        public static async Task<XboxLiveResponse> XBLAuthenticateAsync(string accessToken)
        {
            JsonObject json = new JsonObject
            {
                ["Properties"] = new JsonObject
                {
                    ["AuthMethod"] = "RPS",
                    ["SiteName"] = "user.auth.xboxlive.com",
                    ["RpsTicket"] = accessToken
                },
                ["RelyingParty"] = "http://auth.xboxlive.com",
                ["TokenType"] = "JWT"
            };
            using HttpResponseMessage httpResponse = await PostJsonAsync(API_XBL_AUTHENTICATE, json);
            if (!httpResponse.IsSuccessStatusCode)
                throw new MicrosoftAuthenticationException($"Authentication failed ({httpResponse.StatusCode})");

            return JsonSerializer.Deserialize<XboxLiveResponse>(await httpResponse.Content.ReadAsStringAsync());
        }

        public static async Task<XboxLiveResponse> GetXSTSAuthorizeAsync(XboxLiveResponse xboxLiveResponse)
        {
            JsonObject json = new JsonObject
            {
                ["Properties"] = new JsonObject
                {
                    ["SandboxId"] = "RETAIL",
                    ["UserTokens"] = new JsonArray(xboxLiveResponse.Token)
                },
                ["RelyingParty"] = "rp://api.minecraftservices.com/",
                ["TokenType"] = "JWT"
            };
            using HttpResponseMessage httpResponse = await PostJsonAsync(API_XSTS_AUTHORIZE, json);
            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                long error = long.Parse(JsonNode.Parse(await httpResponse.Content.ReadAsStringAsync())["XErr"].GetValue<string>());
                switch (error)
                {
                    case 2148916233: throw new MicrosoftAuthenticationException("The account doesn't have an Xbox account. Once they sign up for one (or login through minecraft.net to create one) then they can proceed with the login. This shouldn't happen with accounts that have purchased Minecraft with a Microsoft account, as they would've already gone through that Xbox signup process.");
                    case 2148916235: throw new MicrosoftAuthenticationException("The account is from a country where Xbox Live is not available/banned");
                    case 2148916236: throw new MicrosoftAuthenticationException("The account needs adult verification on Xbox page.");
                    case 2148916237: throw new MicrosoftAuthenticationException("The account needs adult verification on Xbox page.");
                    case 2148916238: throw new MicrosoftAuthenticationException("The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult. This only seems to occur when using a custom Microsoft Azure application. When using the Minecraft launchers client id, this doesn't trigger.");
                    default: throw new MicrosoftAuthenticationException($"XSTS authorize failed, error code = {error}");
                }
            }
            else if (!httpResponse.IsSuccessStatusCode)
            {
                throw new MicrosoftAuthenticationException($"Authentication failed ({httpResponse.StatusCode})");
            }

            return JsonSerializer.Deserialize<XboxLiveResponse>(await httpResponse.Content.ReadAsStringAsync());
        }


        
        public static async Task<MinecraftAuthenticateResponse> MinecraftAuthenticate(XboxLiveResponse xstsResponse)
        {
            using HttpResponseMessage httpResponse = await PostJsonAsync(API_XBOX_MINECRAFT_AUTHENTICATE,
                new JsonObject { ["identityToken"] = $"XBL3.0 x={xstsResponse.DisplayClaims.XUIS[0].UserHashs};{xstsResponse.Token}" });

            if (!httpResponse.IsSuccessStatusCode)
                throw new MicrosoftAuthenticationException($"Authentication failed ({httpResponse.StatusCode})");

            return JsonSerializer.Deserialize<MinecraftAuthenticateResponse>(await httpResponse.Content.ReadAsStringAsync());
        }

        private static async Task<HttpResponseMessage> PostJsonAsync(string url, JsonObject json)
        {
            using (HttpClient hc = new HttpClient())
            {
                if (!string.IsNullOrEmpty(UserAgent))
                    hc.DefaultRequestHeaders.Add("UserAgent", UserAgent);
                hc.DefaultRequestHeaders.ConnectionClose = true;
                return await hc.PostAsync(url, new StringContent(json.ToJsonString(), Encoding.UTF8, "application/json"));
            }
        }
    }
}
