using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MinecraftProtocol.Crypto
{

    // qwq感谢该项目教我如何通过Unsafe直接把数组转换为Vector128
    // https://gist.github.com/Thealexbarney/9f75883786a9f3100408ff795fb95d85
    public class EncryptOnlyAes
    {
        public static bool IsSupported => Sse2.IsSupported && Aes.IsSupported;

        private static readonly byte[] _rcon = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36 };

        private Vector128<byte>[] _roundKeys;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public EncryptOnlyAes(Span<byte> key)
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
        public void Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> destination)
        {
            Vector128<byte>[] keys = _roundKeys;

            ReadOnlySpan<Vector128<byte>> blocks = MemoryMarshal.Cast<byte, Vector128<byte>>(plaintext);
            Span<Vector128<byte>> dest = MemoryMarshal.Cast<byte, Vector128<byte>>(destination);

            // Makes the JIT remove all the other range checks on keys
            _ = keys[10];

            for (int i = 0; i < blocks.Length; i++)
            {
                Vector128<byte> b = blocks[i];

                b = Sse2.Xor(b, keys[0]);
                b = Aes.Encrypt(b, keys[1]);
                b = Aes.Encrypt(b, keys[2]);
                b = Aes.Encrypt(b, keys[3]);
                b = Aes.Encrypt(b, keys[4]);
                b = Aes.Encrypt(b, keys[5]);
                b = Aes.Encrypt(b, keys[6]);
                b = Aes.Encrypt(b, keys[7]);
                b = Aes.Encrypt(b, keys[8]);
                b = Aes.Encrypt(b, keys[9]);
                b = Aes.EncryptLast(b, keys[10]);

                dest[i] = b;
            }
        }
        
    }
}