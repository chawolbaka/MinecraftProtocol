using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftProtocol.Auth.Microsoft
{
    public class XboxLiveResponse
    {
        [JsonPropertyName("IssueInstant")]
        public DateTime IssueInstant { get; set; }

        [JsonPropertyName("NotAfter")]
        public DateTime NotAfter { get; set; }

        [JsonPropertyName("Token")]
        public string Token { get; set; }

        [JsonPropertyName("DisplayClaims")]
        public DisplayClaim DisplayClaims { get; set; }

        public class DisplayClaim
        {
            [JsonPropertyName("xui")]
            public List<XUI> XUIS { get; set; }

            public class XUI
            {
                [JsonPropertyName("uhs")]
                public string UserHashs { get; set; }
            }
        }
    }
}
