using MinecraftProtocol.Auth.Yggdrasil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MinecraftProtocol.Auth.Microsoft
{
    public static class MicrosoftAccount
    {
        public static string DefaultClientId { get; set; }

        public static string UserAgent { get; set; } = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        //by mcc
        private static readonly Regex ppft = new("sFTTag:'.*value=\"(.*)\"\\/>'");
        private static readonly Regex urlPost = new("urlPost:'(.+?(?=\'))");

        public static async Task<MicrosoftOAuth2Token> RefreshToken(MicrosoftOAuth2Token token, string clientId, string redirectUri)
        {
            if (token is null)
                throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrWhiteSpace(token.RefreshToken))
                throw new ArgumentNullException(nameof(token.RefreshToken), "Refresh token cannot be empty");

            using HttpClient hc = new HttpClient();
            using HttpResponseMessage httpResponse = await hc.PostAsync("https://login.microsoftonline.com/consumers/oauth2/v2.0/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["grant_type"] = "refresh_token",
                    ["redirect_uri"] = redirectUri,
                    ["refresh_token"] = token.RefreshToken
                }));

            if (!httpResponse.IsSuccessStatusCode)
                throw new MicrosoftAuthenticationException($"Refresh token failed ({httpResponse.StatusCode})");

            JsonNode json = JsonNode.Parse(await httpResponse.Content.ReadAsStringAsync());
            if (json.AsObject().TryGetPropertyValue("errorMessage", out var error))
                throw new YggdrasilException(error.GetValue<string>(), YggdrasilError.Unknown, httpResponse);

            return new MicrosoftOAuth2Token(token.Email, json["access_token"].GetValue<string>(), json["refresh_token"].GetValue<string>(), int.Parse(json["expires_in"].GetValue<string>()));
        }


        public static Task<MicrosoftOAuth2Token> AuthenticateAsync(string email, string password) => AuthenticateAsync(email, password, DefaultClientId);
        public static async Task<MicrosoftOAuth2Token> AuthenticateAsync(string email, string password, string clientId)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId), "You will first need to obtain an OAuth 2.0 client ID by creating a Microsoft Azure application");


            CookieContainer cookies = new CookieContainer();
            using HttpClientHandler handler = new HttpClientHandler() { CookieContainer = cookies };
            using HttpClient hc = new HttpClient(handler);

            if (!string.IsNullOrEmpty(UserAgent))
                hc.DefaultRequestHeaders.Add("UserAgent", UserAgent);

            //预登录
            using HttpResponseMessage preloginResponse = await hc.GetAsync($"https://login.live.com/oauth20_authorize.srf?client_id={clientId}&redirect_uri=https://login.live.com/oauth20_desktop.srf&scope=service::user.auth.xboxlive.com::MBI_SSL&display=touch&response_type=token&locale=en");
            string html = await preloginResponse.Content.ReadAsStringAsync();

            //开始登录
            using HttpResponseMessage loginResponse = await hc.PostAsync(urlPost.Match(html).Groups[1].Value, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["login"] = email,
                ["loginfmt"] = email,
                ["passwd"] = password,
                ["PPFT"] = ppft.Match(html).Groups[1].Value
            }));

            if (!loginResponse.IsSuccessStatusCode)
            {
                string loginContent = (await loginResponse.Content.ReadAsStringAsync()).ToLower();
                if (loginContent.Contains("help us protect your account"))
                    throw new MicrosoftAuthenticationException("2FA enabled but not supported yet");
                else if (loginContent.Contains("sign in to"))
                    throw new MicrosoftAuthenticationException("Invalid credentials. Check your credentials");
                else
                    throw new MicrosoftAuthenticationException($"Authentication failed ({loginResponse.StatusCode})");
            }

            if (string.IsNullOrWhiteSpace(loginResponse.RequestMessage.RequestUri.Fragment) && loginResponse.RequestMessage.RequestUri.Fragment[0] == '#')
                throw new MicrosoftAuthenticationException("Cannot extract access token");

            MicrosoftOAuth2Token token = new MicrosoftOAuth2Token(email);
            string[] fragment = loginResponse.RequestMessage.RequestUri.Fragment.AsSpan().Slice(1).ToString().Split('=');
            for (int i = 0; i < fragment.Length; i++)
            {
                if (fragment[i] == "access_token" && i + 1 < fragment.Length)
                    token.AccessToken = fragment[++i];
                else if (fragment[i] == "refresh_token" && i + 1 < fragment.Length)
                    token.AccessToken = fragment[++i];
                else if (fragment[i] == "expires_in" && i + 1 < fragment.Length)
                    token.AccessToken = fragment[++i];
            }
            return token;
        }
    }

}
