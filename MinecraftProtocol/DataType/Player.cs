using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType
{
    public class Player
    { 
        //private Coords Coords { get; }
        public Guid Uuid { get; set; }//我知道命名规则冲突了呀QAQ,可是Uuid看着好丑的感觉
        public string Name { get; set; }

        /// <summary>
        /// First use the network need
        /// </summary>
        public bool HasBuyGame {
            get
            {
                if (_HasBuyGame == null)
                {
                    try
                    {
                        VerifyUUID();
                        _HasBuyGame=true;
                    }
                    catch (Exception)
                    {
                        _HasBuyGame = false;
                    }
                }
                return (bool)_HasBuyGame;
            }
        }
        public byte[] Skin {
            get {
                if (_Skin == null) _Skin = GetSkin();
                return _Skin;
            }
            set {
                _Skin = value;
            }
        }
        private byte[] _Skin;
        private bool? _HasBuyGame = null;

        public Player(string name,Guid uuid)
        {
            Name = string.IsNullOrWhiteSpace(name)==false?name:throw new ArgumentNullException(nameof(name));
            Uuid = uuid != null?uuid:throw new ArgumentNullException(nameof(uuid));
        }

        public bool VerifyUUID()
        {
            WebClient wc = new WebClient();
            string html = Encoding.UTF8.GetString(wc.DownloadData(
                @"https://api.mojang.com/users/profiles/minecraft/" + Name));
            if (string.IsNullOrWhiteSpace(html))
                return Uuid.ToString().Replace("-","")== JObject.Parse(html)["id"].ToString();
            else
                throw new Exception("The Player does not exist");
        }
        private byte[] GetSkin()
        {
            //API:https://sessionserver.mojang.com/session/minecraft/profile/<uuid>
            WebClient wc = new WebClient();
            string html = Encoding.UTF8.GetString(wc.DownloadData(
                @"https://sessionserver.mojang.com/session/minecraft/profile/" + 
                Uuid.ToString().Replace("-","")));
            if (string.IsNullOrWhiteSpace(html))
            {
                _HasBuyGame = false;
                throw new Exception("the uuid does not exist(does it have buy this game?)");
            }
            else
            {
                byte[] skin = null ;
                var json = JObject.Parse(html);
                if(json.ContainsKey("properties"))
                {
                    string base64 = json["properties"][0]["value"].ToString();
                    return Convert.FromBase64String(base64);
                }
                else
                {
                    throw new JsonException("can not read api(is it updated?)");
                }
            }
        }
    }
}
