using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MinecraftProtocol.Crypto
{

    // qwq感谢该项目教我如何通过Unsafe直接把数组转换为Vector128
    // https://gist.github.com/Thealexbarney/9f75883786a9f3100408ff795fb95d85
    public class AesCfbOnly
    {
        public static bool IsSupported => Sse2.IsSupported && Aes.IsSupported;

        private static readonly byte[] _rcon = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36 };

        private Vector128<byte>[] _roundKeys;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public AesCfbOnly(Span<byte> key)
        {
            _roundKeys = new Vector128<byte>[11];
            _roundKeys[0] = Unsafe.ReadUnaligned<Vector128<byte>>(ref key[0]);

            for (int i = 1; i <= 10; i++)
            {
                //(； ･`д･´)看什么看，没见过强迫症吗
                _roundKeys[i] = Sse2.Xor(_roundKeys[i - 1], Sse2.ShiftLeftLogical128BitLane(_roundKeys[i - 1], 4));
                _roundKeys[i] = Sse2.Xor(_roundKeys[i - 0], Sse2.ShiftLeftLogical128BitLane(_roundKeys[i - 0], 4));
                _roundKeys[i] = Sse2.Xor(_roundKeys[i - 0], Sse2.ShiftLeftLogical128BitLane(_roundKeys[i - 0], 4));

                _roundKeys[i] = Sse2.Xor(_roundKeys[i], Sse2.Shuffle(Aes.KeygenAssist(_roundKeys[i - 1], _rcon[i - 1]).AsInt32(), 0xff).AsByte());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void EncryptCfbBlock(Span<byte> currentBlock, ref byte dest)
        {
            Vector128<byte>[] keys = _roundKeys;

            // Makes the JIT remove all the other range checks on keys
            _ = keys[10];

            Vector128<byte> block = Unsafe.ReadUnaligned<Vector128<byte>>(ref currentBlock[0]);
            block = Sse2.Xor(block, keys[0]);
            block = Aes.Encrypt(block, keys[1]);
            block = Aes.Encrypt(block, keys[2]);
            block = Aes.Encrypt(block, keys[3]);
            block = Aes.Encrypt(block, keys[4]);
            block = Aes.Encrypt(block, keys[5]);
            block = Aes.Encrypt(block, keys[6]);
            block = Aes.Encrypt(block, keys[7]);
            block = Aes.Encrypt(block, keys[8]);
            block = Aes.Encrypt(block, keys[9]);
            block = Aes.EncryptLast(block, keys[10]);

            //反正cfb只需要第一个byte，那就直接返回加密后的第一个byte就够啦，省的每次加密还要专门生成一个数组来存加密后的内容
            dest ^= Unsafe.As<Vector128<byte>, byte>(ref Unsafe.AsRef(in block));
        }
        
    }
}