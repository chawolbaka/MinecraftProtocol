using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MinecraftProtocol.Crypto
{
    public static class CryptoUtils
    {
        private const string SERVER_ID_STRING_ENCODE = "iso-8859-1";

        public static byte[] GenerateSecretKey(int keySize = 128)
        {
            AesManaged AES = new AesManaged();
            AES.KeySize = keySize;
            AES.GenerateKey();
            return AES.Key;
        }
        public static string GetServerHash(string serverID, byte[] secretKey, byte[] publicKey)
        {

            /*
             * sha1.update(ASCII encoding of the server id string from Encryption Request) 
             * sha1.update(shared secret) 
             * sha1.update(server's encoded public key from Encryption Request) 
             * 按照wiki.vg上写的我猜是把这3个属性按下面顺序叠在一个,然后算出MinecraftShaDigest
             * 1.通过ASCII编码后的server id string(1.7后是空的)
             * 2.公钥
             * 3.服务器加密请求内带的一个Token(应该是服务端随机生成的)
             */
            return GetMinecraftShaDigest(new byte[][] {
                Encoding.GetEncoding(SERVER_ID_STRING_ENCODE).GetBytes(serverID),
                secretKey,
                publicKey
            });
        }
        public static string GetMinecraftShaDigest(string value) => GetMinecraftShaDigest(Encoding.UTF8.GetBytes(value));
        public static string GetMinecraftShaDigest(params byte[][] bytes)
        {
            /*
             * that the Sha1.hexdigest() method used by minecraft is non standard. 
             * It doesn't match the digest method found in most programming languages and libraries. 
             * It works by treating the sha1 output bytes as one large integer in two's complement and then printing the integer in base 16
             * placing a minus sign if the interpreted number is negative. 
             * 
             * 谷歌+人工理解后的翻译:
             * mc使用的不是标准的Sha1.hexdigest(),大部分编程语言取出来的hash都无法和MC的匹配(我怎么觉得是全部)
             * mc的是这样子的:
             * 1.首先算出SHA1
             * 2.如果不是负数就直接转成16进制
             * 2.如果是负数就把它转成正数然后转成16进制,转完往开头加"-"号
             */
            byte[] hash = GetSha1Digest(bytes);
            bool isNegative = (hash[0] & 0b1000_0000) == 0b1000_0000;
            if (isNegative)
            {
                //如果是负数就需要把它转换成正数(转换方法:https://gist.github.com/toqueteos/5372776)
                bool carry = true;
                for (int i = hash.Length - 1; i >= 0; i--)
                {
                    hash[i] = (byte)~hash[i];
                    if (carry)
                    {
                        carry = hash[i] == 0xFF;
                        hash[i]++;
                    }
                }
            }
            //C#有时候会在开头加一个0,这对MC来说是多余的。
            return isNegative ? "-" + GetHexString(hash).TrimStart('0') : GetHexString(hash).TrimStart('0');
        }
        public static string GetHexString(byte[] hex)
        {
            StringBuilder sb = new StringBuilder() { Capacity = hex.Length * 2};
            for (int i = 0; i < hex.Length; i++)
                sb.Append(hex[i].ToString("x2"));
            return sb.ToString();
        }
        public static byte[] GetHexBytes(string hex)
        {
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return result;
        }
        private static byte[] GetSha1Digest(byte[][] hash)
        {
            //叠叠乐?把所有byte叠一起然后算出hash
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            for (int i = 0; i < hash.Length; i++)
                sha1.TransformBlock(hash[i], 0, hash[i].Length, hash[i], 0);
            //看不懂这行是什么意思
            sha1.TransformFinalBlock(new byte[] { }, 0, 0);
            return sha1.Hash;
        }
    }
}
