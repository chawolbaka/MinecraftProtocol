using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.API
{
    public static class MojangAPI
    {

        /// <summary>
        /// 通过玩家名查询UUID
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="MojangAPIException"/>
        public static UUID GetUUID(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentNullException(nameof(playerName));
                
            HttpClient hc = new HttpClient();
            var HttpResponse = hc.GetAsync("https://api.mojang.com/users/profiles/minecraft/" + playerName).Result;
            string json = HttpResponse.Content.ReadAsStringAsync().Result;
            if (!string.IsNullOrWhiteSpace(json))
            {
                string id = JObject.Parse(json)["id"].ToString();
                if(string.IsNullOrWhiteSpace(id))
                    throw new MojangAPIException("无法从响应中读取UUID.", json, HttpResponse.StatusCode);
                return new UUID(Guid.Parse(id));
            }
            else
            {
                throw new MojangAPIException("MojanAPI未返回有效数据.", json, HttpResponse.StatusCode); 
            }
        }
    }
}
