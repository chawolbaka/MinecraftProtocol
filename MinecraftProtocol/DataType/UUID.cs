using System;
using System.Text;
using System.Security.Cryptography;
using MinecraftProtocol.API;

namespace MinecraftProtocol.DataType
{

    public struct UUID:IEquatable<UUID>
    {
        public static UUID Empty => new UUID(Guid.Empty);

        private Guid _uuid;

        public UUID(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                throw new ArgumentNullException(nameof(uuid));
            _uuid = Guid.Parse(uuid);
        }
        public UUID(Guid uuid)
        {
            if (uuid==null)
                throw new ArgumentNullException(nameof(uuid));
            _uuid = uuid;
        }

        /// <summary>
        /// 从MojangAPI获取正版玩家的UUID
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="MojangAPIException"/>
        public static UUID GetUUIDByMojangAPI(string playerName)=>MojangAPI.GetUUID(playerName);
        public static UUID GetUUIDByPlayerName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                throw new ArgumentNullException(nameof(playerName));
            }
            else
            {
                //https://gist.github.com/games647/2b6a00a8fc21fd3b88375f03c9e2e603
                //https://en.wikipedia.org/wiki/Universally_unique_identifier#Versions_3_and_5_(namespace_name-based)
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] MD5 = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + playerName));
                MD5[6] = (byte)((MD5[6] & 0x0f) | 0x30); //set the version to 3 -> Name based md5 hash
                MD5[8] = (byte)((MD5[8] & 0x3f) | 0x80); //IETF variant

                StringBuilder uuid_hex = new StringBuilder();
                for (int i = 0; i < MD5.Length; i++)
                    uuid_hex.Append(MD5[i].ToString("x2"));

                //TrimStart('0')对于CryptoHandler.GetMinecraftShaDigest是必要的,但是对于UUID我不清楚是不是必要的
                //总之先不加,毕竟这边是要塞到Guid里面的,可能已经在里面处理过了?
                //哪天出bug了再加上把qwq
                //return new UUID(uuid_hex.ToString().TrimStart('0'));
                return new UUID(uuid_hex.ToString());
            }
        }
        
        //这边为什么不新建一个guid?
        //我是是这样考虑的,Guid的构造函数看了眼源码好像要做好多事情的样子
        //所以直接把_uuid丢出去了,我觉得直接让它直接在内存里面复制一份比较省事
        public Guid ToGuid() => _uuid;
        public override string ToString() => _uuid.ToString();
        public override int GetHashCode() => _uuid.GetHashCode();
        public static explicit operator string(UUID uuid) => uuid.ToString();
        public static explicit operator Guid(UUID uuid) => uuid.ToGuid();

        public bool Equals(UUID other) => this._uuid.Equals(other.ToGuid());
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is UUID id))
                return false;
            else
                return this.Equals(id);
        }

    }
}
