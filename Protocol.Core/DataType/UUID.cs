using System;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

namespace MinecraftProtocol.DataType
{

    public struct UUID : IComparable, IComparable<UUID>, IEquatable<UUID>
    {

        private static Dictionary<string, UUID> Cache = new Dictionary<string, UUID>();
        public static UUID Empty => new UUID(0, 0);
        
        public readonly long Most;
        public readonly long Least;

        public UUID(long most, long least)
        {
            Most = most;
            Least = least;
        }

        public UUID(byte[] data)
        {
            if (data == null) 
                throw new ArgumentNullException(nameof(data));
            if (data.Length != 16)
                throw new ArgumentOutOfRangeException(nameof(data), $"{nameof(data)}的长度必须是16");
            
            Least = 0; Most = 0;
            for (int i = 0; i < 8; i++)
                Most = (Most << 8) | data[i];
            for (int i = 8; i < 16; i++)
                Least = (Least << 8) | data[i];
        }

        /// <summary>
        /// 通过玩家名向MojangAPI获取正版玩家的UUID
        /// </summary>
        /// <param name="playerName">玩家名</param>
        /// <param name="useCache">是否缓存获取从MojangAPI获取的uuid</param>
        /// <param name="url">api地址</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        public static async ValueTask<UUID> GetFromMojangAsync(string playerName, bool useCache = true, string url = "https://api.mojang.com/users/profiles/minecraft/")
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentNullException(nameof(playerName));
            if (useCache&&Cache.ContainsKey(playerName))
                return Cache[playerName];

            HttpClient hc = new HttpClient();
            var HttpResponse = await hc.GetAsync(url + playerName);
            string json = await HttpResponse.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("MojangAPI未响应任何有效数据");

            string id = JsonNode.Parse(json)["id"].GetValue<string>();

            if (string.IsNullOrWhiteSpace(id))
                throw new FormatException("MojangAPI返回了空uuid");

            if (useCache) 
                Cache.Add(playerName, Parse(id));
            return Parse(id);

        }

        /// <summary>
        /// 通过玩家名获取离线模式下的UUID
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static UUID GetFromPlayerName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
                throw new ArgumentNullException(nameof(playerName));

            //https://gist.github.com/games647/2b6a00a8fc21fd3b88375f03c9e2e603
            //https://en.wikipedia.org/wiki/Universally_unique_identifier#Versions_3_and_5_(namespace_name-based)
            using (MD5 md5 = MD5.Create())
            {
                byte[] MD5 = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + playerName));
                MD5[6] = (byte)((MD5[6] & 0x0f) | 0x30); //set the version to 3 -> Name based md5 hash
                MD5[8] = (byte)((MD5[8] & 0x3f) | 0x80); //IETF variant
                return new UUID(MD5);
            }
        }

        public static UUID NewUUID()
        {
            byte[] bytes = new byte[16];
            using RandomNumberGenerator RNG = RandomNumberGenerator.Create();
            RNG.GetBytes(bytes);

            //https://en.wikipedia.org/wiki/Universally_unique_identifier#Version_4_(random)
            bytes[6] = (byte)((bytes[6] & 0x0f) | 0x40); //set the version to 4 -> Random
            bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80); //IETF variant
            return new UUID(bytes);
        }

        public static UUID Parse(string input)
        {
            string[] hexs;
            if (input.Length == 36)
            {
                hexs = input.Split('-');
                if (hexs.Length != 5 || hexs[0].Length != 8 || hexs[1].Length != 4 || hexs[2].Length != 4 || hexs[3].Length != 4 || hexs[4].Length != 12)
                    throw new FormatException("UUID的格式必须是xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
            }
            else if (input.Length == 32)
            {
                hexs = new string[5];
                hexs[0] = input.Substring(0, 8);
                hexs[1] = input.Substring(8, 4);
                hexs[2] = input.Substring(12, 4);
                hexs[3] = input.Substring(16, 4);
                hexs[4] = input.Substring(20, 12);
            }
            else
            {
                throw new FormatException("无法解析uuid的格式");
            }
            

            return new UUID(
                (Convert.ToInt64(hexs[0], 16) << 32) | (Convert.ToInt64(hexs[1], 16) << 16) | Convert.ToInt64(hexs[2], 16),
                (Convert.ToInt64(hexs[3], 16) << 48) | (Convert.ToInt64(hexs[4], 16)));
        }
        public static bool TryParse(string input, out UUID result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(input) || input.Length is not 36 or 32)
                return false;

            try
            {
                result = Parse(input);
                return true;
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (ArgumentException) { return false; }
            catch (FormatException) { return false; }
            catch (OverflowException) { return false; }

        }

        public string ToString(bool toUpper)
        {
            StringBuilder sb = new StringBuilder();
            sb.Capacity = 36;
            sb.Append(ToHex(((Most  >> 32) & 0xFFFFFFFF)     | 0x100000000, toUpper)).Append('-');
            sb.Append(ToHex(((Most  >> 16) & 0xFFFF)         | 0x10000, toUpper)).Append('-');
            sb.Append(ToHex(((Most  >> 00) & 0xFFFF)         | 0x10000, toUpper)).Append('-');
            sb.Append(ToHex(((Least >> 48) & 0xFFFF)         | 0x10000, toUpper)).Append('-');
            sb.Append(ToHex(((Least >> 00) & 0xFFFFFFFFFFFF) | 0x1000000000000, toUpper));
            return sb.ToString();
        }
        public override string ToString() => ToString(false);
        private ReadOnlySpan<char> ToHex(long value, bool toUpper) => value.ToString(toUpper ? "X2" : "x2").AsSpan().Slice(1);

        public override bool Equals(object obj) => obj is UUID uUID && Equals(uUID);

        public bool Equals(UUID other) => Most == other.Most && Least == other.Least;

        public static bool operator ==(UUID left, UUID right) => left.Equals(right);

        public static bool operator !=(UUID left, UUID right) => !(left == right);

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 0;
            if (!(obj is UUID))
                throw new ArgumentException("对象的类型必须是UUID", nameof(obj));

            return CompareTo((UUID)obj);
        }

        public int CompareTo(UUID other)
        {
            if (this.Most < other.Most) return -1;
            else if (this.Most > other.Most) return 1;
            else if (this.Least < other.Least) return -1;
            else if (this.Least > other.Least) return 1;
            else return 0;
        }

        public override int GetHashCode()
        {
            long temp = Most ^ Least;
            return ((int)(temp >> 32)) ^ (int)temp;
        }

    }
}
