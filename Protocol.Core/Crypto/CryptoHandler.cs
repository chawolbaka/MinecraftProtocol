using System;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace MinecraftProtocol.Crypto
{
    public class CryptoHandler
    {
        public bool Enable => _enable;
        public byte[] Key => _key != null ? (byte[])_key.Clone() : throw new InvalidOperationException("未初始化。");

        private const int BLOCK_SIZE = 16;
        private bool _enable;
        private byte[] _key;

        private byte[] _encryptIV;
        private byte[] _decryptIV;

        private AesCfbOnly _fastAes;
        private Aes _defaultAes;

        public void Init(byte[] secretKey)
        {
            _enable = true;
            _key = (byte[])secretKey.Clone();
            _encryptIV = (byte[])_key.Clone();
            _decryptIV = (byte[])_key.Clone();
            if (!AesCfbOnly.IsSupported)
            {
                _defaultAes = Aes.Create();
                _defaultAes.BlockSize = 128;
                _defaultAes.KeySize = 128;
                _defaultAes.Key = secretKey;
                _defaultAes.Mode = CipherMode.ECB;
                _defaultAes.Padding = PaddingMode.None;
            }
            else
            {
                _fastAes = new AesCfbOnly(_key);
            }
        }

        //这边按Try开头感觉容易误解，以后想到更适合的可能更改

        /// <summary>
        /// 如果已开启就对input进行加密，否则直接返回input
        /// </summary>
        public byte[] TryEncrypt(byte[] input)
        {
            
            if (_enable)
                Encrypt(input);
            return input;
        }

        /// <summary>
        /// 如果已开启就对input进行解密，否则直接返回input
        /// </summary>
        public byte[] TryDecrypt(byte[] input)
        {
            if (_enable)
                Decrypt(input);
            return input;
        }


        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Encrypt(Span<byte> buffer) 
        {
            if (!_enable)
                throw new InvalidOperationException("加密未开启。");

            //密钥流：
            //对第n块进行aes加密
            //加密完成后取密钥流第n块的第0位对明文进行xor得到密文
            //块的位置向后移动1位，并将密文加到当前块的末位
            //（1块的大小是16字节）



            //这边只声明这种长度的keyStream是一种轻微的优化，因为如果超出这个长度就可以直接对着buffer进行加密流，所以这样可以避免每次加密都声明buffer.Length + BLOCK_SIZE的keyStream
            Span<byte> keyStream = new byte[(BLOCK_SIZE * 2)];
            _encryptIV.CopyTo(keyStream); //复制IV到密钥流的开头
            buffer.Slice(0, buffer.Length <= 16 ? buffer.Length: BLOCK_SIZE).CopyTo(keyStream.Slice(BLOCK_SIZE)); //复制明文到剩下的位置

            //如果是直接使用AES-NI那么就不需要这个blockOutput，因为可以直接修改代码，但C#自带的AES类我无法修改所以还是需要声明一个blockOutput
            Span<byte> blockOutput = _fastAes == null ? stackalloc byte[BLOCK_SIZE] : null;
            for (int i = 0; i < buffer.Length; i++)
            {
                Span<byte> currentBlock = i > 16 ? buffer.Slice(i - BLOCK_SIZE, BLOCK_SIZE) : keyStream.Slice(i, BLOCK_SIZE);

                if (_fastAes == null)
                {
                    _defaultAes.EncryptEcb(currentBlock, blockOutput, PaddingMode.None);
                    buffer[i] = (byte)(buffer[i] ^ blockOutput[0]);
                }
                else
                {
                    _fastAes.EncryptCfbBlock(currentBlock, ref buffer[i]);
                }
                if (i < 16)
                    keyStream[BLOCK_SIZE + i] = buffer[i];
            }

            if (buffer.Length > 16)
                buffer.Slice(buffer.Length - BLOCK_SIZE).CopyTo(_encryptIV);
            else
                keyStream.Slice(buffer.Length, BLOCK_SIZE).CopyTo(_encryptIV);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Decrypt(Span<byte> buffer)
        {
            if (!_enable)
                throw new InvalidOperationException("加密未开启。");

            Span<byte> keyStream = new byte[buffer.Length + BLOCK_SIZE];
            _decryptIV.CopyTo(keyStream);
            buffer.CopyTo(keyStream.Slice(BLOCK_SIZE));

            Span<byte> blockOutput = _fastAes == null ? stackalloc byte[BLOCK_SIZE] : null;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (_fastAes == null)
                {
                    _defaultAes.EncryptEcb(keyStream.Slice(i, BLOCK_SIZE), blockOutput, PaddingMode.None);
                    buffer[i] = (byte)(buffer[i] ^ blockOutput[0]);
                }
                else
                {
                    _fastAes.EncryptCfbBlock(keyStream.Slice(i, BLOCK_SIZE), ref buffer[i]);
                }
            }
            keyStream.Slice(buffer.Length).CopyTo(_decryptIV);
        }
    }
}