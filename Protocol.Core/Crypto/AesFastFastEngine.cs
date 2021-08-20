using System;
using System.Linq;
using BouncyCastle.Crypto;
using BouncyCastle.Crypto.Parameters;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;


namespace MinecraftProtocol.Crypto
{
    public class AesFastFastEngine : IBlockCipher
    {
        public static bool IsSupported => Sse2.IsSupported && Aes.IsSupported;

        public string AlgorithmName => "AES";

        public bool IsPartialBlockOkay => false;

        public int GetBlockSize() => BLOCK_SIZE;

        private const int BLOCK_SIZE = 16;

        private Vector128<byte>[] _roundKeys;
        private bool _forEncryption;

        // vector used in calculating key schedule (powers of x in GF(256))
        private static readonly byte[] rcon = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36 };

        public virtual void Init(bool forEncryption, ICipherParameters parameters)
        {
            KeyParameter keyParameter = parameters as KeyParameter;

            if (keyParameter == null)
                throw new ArgumentNullException(nameof(parameters));

            byte[] key = keyParameter.GetKey();
            if (key.Length != 16)
                throw new NotImplementedException("AES-128 only");		
			
            _roundKeys = new Vector128<byte>[11];
            _roundKeys[0] = Vector128.Create(key[0],key[1],key[2],key[3],key[4],key[5],key[6],key[7],key[8],key[9],key[10],key[11],key[12],key[13],key[14],key[15]);
            for (int i = 1; i <= 10; ++i)
            {
                //(； ･`д･´)看什么看，没见过间歇性的强迫症吗
                _roundKeys[i] = Sse2.Xor(_roundKeys[i-1], Sse2.ShiftLeftLogical128BitLane(_roundKeys[i-1], 4));
                _roundKeys[i] = Sse2.Xor(_roundKeys[i-0], Sse2.ShiftLeftLogical128BitLane(_roundKeys[i-0], 4));
                _roundKeys[i] = Sse2.Xor(_roundKeys[i-0], Sse2.ShiftLeftLogical128BitLane(_roundKeys[i-0], 4));

                if (forEncryption || i == 10)
                    _roundKeys[i] = Sse2.Xor(_roundKeys[i], Sse2.Shuffle(Aes.KeygenAssist(_roundKeys[i-1], rcon[i-1]).AsInt32(), 0xff).AsByte());
                else
                    _roundKeys[i] = Aes.InverseMixColumns(Sse2.Xor(_roundKeys[i], Sse2.Shuffle(Aes.KeygenAssist(_roundKeys[i-1], rcon[i-1]).AsInt32(), 0xff).AsByte()));
            }

            _forEncryption = forEncryption;
        }

        public virtual int ProcessBlock(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            Vector128<byte> Block;
            if (_forEncryption)
                Block=EncryptBlock(Vector128.Create(input[0+inputOffset],input[1+inputOffset],input[2+inputOffset],input[3+inputOffset],input[4+inputOffset],input[5+inputOffset],input[6+inputOffset],input[7+inputOffset],input[8+inputOffset],input[9+inputOffset],input[10+inputOffset],input[11+inputOffset],input[12+inputOffset],input[13+inputOffset],input[14+inputOffset],input[15+inputOffset]));
            else
                Block=DecryptBlock(Vector128.Create(input[0+inputOffset],input[1+inputOffset],input[2+inputOffset],input[3+inputOffset],input[4+inputOffset],input[5+inputOffset],input[6+inputOffset],input[7+inputOffset],input[8+inputOffset],input[9+inputOffset],input[10+inputOffset],input[11+inputOffset],input[12+inputOffset],input[13+inputOffset],input[14+inputOffset],input[15+inputOffset]));

            for (int i = 0; i < 16; i++)
                output[i+outputOffset] = Block.GetElement(i);

            return BLOCK_SIZE;
        }

        public virtual int ProcessBlock(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Vector128<byte> Block;
            if (_forEncryption)
                Block=EncryptBlock(Vector128.Create(input[0],input[1],input[2],input[3],input[4],input[5],input[6],input[7],input[8],input[9],input[10],input[11],input[12],input[13],input[14],input[15]));
            else
                Block=DecryptBlock(Vector128.Create(input[0],input[1],input[2],input[3],input[4],input[5],input[6],input[7],input[8],input[9],input[10],input[11],input[12],input[13],input[14],input[15]));

            for (int i = 0; i < 16; i++)
                output[i] = Block.GetElement(i);
            
            return BLOCK_SIZE;
        }

        private Vector128<byte> EncryptBlock(Vector128<byte> data)
        {
            data = Sse2.Xor(data, _roundKeys[0]);
            for (int i = 1; i < _roundKeys.Length - 1; i++)
                data = Aes.Encrypt(data, _roundKeys[i]);

            return Aes.EncryptLast(data, _roundKeys[_roundKeys.Length-1]);
        }
        private Vector128<byte> DecryptBlock(Vector128<byte> data)
        {
            data = Sse2.Xor(data, _roundKeys[_roundKeys.Length - 1]);
            for (int i = _roundKeys.Length - 2; i > 0; i--)
                data = Aes.Decrypt(data, _roundKeys[i]);

            return Aes.DecryptLast(data, _roundKeys[0]);
        }

        public void Reset()
        {

        }
    }
}