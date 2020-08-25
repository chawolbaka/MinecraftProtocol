using System;
using System.Text;
using System.Security.Cryptography;
using BouncyCastle.Crypto;
using BouncyCastle.Crypto.Modes;
using BouncyCastle.Crypto.Parameters;
using BouncyCastle.Crypto.Engines;

namespace MinecraftProtocol.Crypto
{
    public class CryptoHandler
    {
        
        public bool Enable => _enable;
        private bool _enable;
        public byte[] Key => _key != null ? (byte[])_key.Clone() : throw new InvalidOperationException("未初始化。");
        private byte[] _key;

        private BufferedBlockCipher _encrypt;
        private BufferedBlockCipher _decrypt;

        public void Init(byte[] secretKey)
        {
            _enable = true;
            _key = (byte[])secretKey.Clone();

            _encrypt = new BufferedBlockCipher(new CfbBlockCipher(new AesFastEngine(), 8));
            _encrypt.Init(true, new ParametersWithIV(new KeyParameter(secretKey), secretKey, 0, 16));

            _decrypt = new BufferedBlockCipher(new CfbBlockCipher(new AesFastEngine(), 8));
            _decrypt.Init(false, new ParametersWithIV(new KeyParameter(secretKey), secretKey, 0, 16));

        }
        public byte[] Encrypt(byte[] input) => Encrypt(input, 0, input.Length);
        public byte[] Encrypt(byte[] input, int offset, int length)
        {
            if (!_enable)
                throw new InvalidOperationException("加密未开启。");
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} 必须大于0");
            return _encrypt.ProcessBytes(input, offset, length);
        }

        public byte[] Decrypt(byte[] input) => Decrypt(input, 0, input.Length);
        public byte[] Decrypt(byte[] input, int offset, int length)
        {
            if (!_enable)
                throw new InvalidOperationException("加密未开启。");
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} 必须大于0");
            return _decrypt.ProcessBytes(input, offset, length);
        }

    }
}
