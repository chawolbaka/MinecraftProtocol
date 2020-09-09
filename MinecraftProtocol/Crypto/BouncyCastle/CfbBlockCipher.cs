using System;
using BouncyCastle.Crypto.Parameters;

namespace BouncyCastle.Crypto.Modes
{
    /// <summary>
    /// implements a Cipher-FeedBack (CFB) mode on top of a simple cipher.
    /// </summary>
    public class CfbBlockCipher : IBlockCipher
    {
        private byte[]	IV;
        private byte[]	cfbV;
        private byte[]	cfbOutV;
		private bool	encrypting;
  
        private readonly int			blockSize;
        private readonly IBlockCipher	cipher;

        /// <summary>
        /// Basic constructor.
        /// </summary>
        /// <param name="cipher">the block cipher to be used as the basis of the feedback mode.</param>
        /// <param name="bitBlockSize">the block size in bits (note: a multiple of 8)</param>
        public CfbBlockCipher(IBlockCipher cipher, int bitBlockSize)
        {
            this.cipher = cipher;
            this.blockSize = bitBlockSize / 8;
            this.IV = new byte[cipher.GetBlockSize()];
            this.cfbV = new byte[cipher.GetBlockSize()];
            this.cfbOutV = new byte[cipher.GetBlockSize()];
        }

        /// <summary>
        /// return the underlying block cipher that we are wrapping.
        /// </summary>
        public IBlockCipher GetUnderlyingCipher()
        {
            return cipher;
        }

        /// <summary>
        /// Initialise the cipher and, possibly, the initialisation vector(IV).
        /// If an IV isn't passed as part of the parameter, the IV will be all zeros.
        /// An IV which is too short is handled in FIPS compliant fashion.
        /// </summary>
        /// <param name="forEncryption">if true the cipher is initialised for encryption, if false for decryption.</param>
        /// <param name="parameters">the key and other data required by the cipher.</param>
        /// <exception cref="ArgumentException">if the parameters argument is inappropriate.</exception>
        public void Init(bool forEncryption, ICipherParameters parameters)
        {
            this.encrypting = forEncryption;
            if (parameters is ParametersWithIV)
            {
                ParametersWithIV ivParam = (ParametersWithIV)parameters;
                byte[] iv = ivParam.GetIV();
                int diff = IV.Length - iv.Length;
                IV = diff == 0 ? iv : iv.AsSpan().Slice(diff).ToArray();
                //Array.Copy(iv, 0, IV, diff, iv.Length);
                Array.Clear(IV, 0, diff);

                parameters = ivParam.Parameters;
            }
            Reset();

            // if it's null, key is to be reused.
            if (parameters != null)
            {
                cipher.Init(true, parameters);
            }
        }


        /// <summary>
        /// return the algorithm name and mode.
        /// </summary>
        public string AlgorithmName => cipher.AlgorithmName + "/CFB" + (blockSize * 8);

        public bool IsPartialBlockOkay => true;

        /// <summary>
        /// return the block size we are operating at.
        /// </summary>
        /// <returns>the block size we are operating at (in bytes).</returns>
        public int GetBlockSize()
        {
            return blockSize;
        }

        /// <summary>
        /// Process one block of input from the array in and write it to the out array.
        /// </summary>
        /// <param name="input">the array containing the input data.</param>
        /// <param name="inOff">thr offset into the in array the data starts at.</param>
        /// <param name="output">the array the output data will be copied into.</param>
        /// <param name="outOff">the offset into the out array the output will start at.</param>
        /// <exception cref="DataLengthException">if there isn't enough data in in, or space in out.</exception>
        /// <exception cref="InvalidOperationException">if the cipher isn't initialised.</exception>
        /// <returns>the number of bytes processed and produced.</returns>
        public int ProcessBlock(byte[] input, int inOff, byte[] output, int outOff)
        {
            return (encrypting)
				?	EncryptBlock(input, inOff, output, outOff)
				:	DecryptBlock(input, inOff, output, outOff);
        }

        public int ProcessBlock(ReadOnlySpan<byte> input, Span<byte> output)
        {
            return (encrypting)
                ? EncryptBlock(input, output)
                :  DecryptBlock(input, output);
        }

        /// <summary>
        /// Do the appropriate processing for CFB mode encryption.
        /// </summary>
        /// <param name="input">the array containing the data to be encrypted.</param>
        /// <param name="inOff">offset into the in array the data starts at.</param>
        /// <param name="outBytes">the array the encrypted data will be copied into.</param>
        /// <param name="outOff">the offset into the out array the output will start at.</param>
        /// <exception cref="DataLengthException">if there isn't enough data in in, or space in out.</exception>
        /// <exception cref="InvalidOperationException">if the cipher isn't initialised.</exception>
        /// <returns>the number of bytes processed and produced.</returns>
        public int EncryptBlock(byte[] input, int inOff, byte[] outBytes, int outOff)
        {
            if ((inOff + blockSize) > input.Length)
                throw new DataLengthException("input buffer too short");
            if ((outOff + blockSize) > outBytes.Length)
                throw new DataLengthException("output buffer too short");
            
            cipher.ProcessBlock(cfbV, 0, cfbOutV, 0);
            //
            // XOR the cfbV with the plaintext producing the ciphertext
            //
            for (int i = 0; i < blockSize; i++)
            {
                outBytes[outOff + i] = (byte)(cfbOutV[i] ^ input[inOff + i]);
            }
            //
            // change over the input block.
            //
            Array.Copy(cfbV, blockSize, cfbV, 0, cfbV.Length - blockSize);
            Array.Copy(outBytes, outOff, cfbV, cfbV.Length - blockSize, blockSize);
            return blockSize;
        }

        /// <summary>
        /// Do the appropriate processing for CFB mode decryption.
        /// </summary>
        /// <param name="input">the array containing the data to be decrypted.</param>
        /// <param name="inOff">offset into the in array the data starts at.</param>
        /// <param name="outBytes">the array the decrypted data will be copied into.</param>
        /// <param name="outOff">the offset into the out array the output will start at.</param>
        /// <exception cref="DataLengthException">if there isn't enough data in in, or space in out.</exception>
        /// <exception cref="InvalidOperationException">if the cipher isn't initialised.</exception>
        /// <returns>the number of bytes processed and produced.</returns>
        public int DecryptBlock(byte[] input, int inOff, byte[] outBytes, int outOff)
        {
            if ((inOff + blockSize) > input.Length)
            {
                throw new DataLengthException("input buffer too short");
            }
            if ((outOff + blockSize) > outBytes.Length)
            {
                throw new DataLengthException("output buffer too short");
            }
            cipher.ProcessBlock(cfbV, 0, cfbOutV, 0);

            // change over the input block.            
            Array.Copy(cfbV, blockSize, cfbV, 0, cfbV.Length - blockSize);
            Array.Copy(input, inOff, cfbV, cfbV.Length - blockSize, blockSize);

            // XOR the cfbV with the ciphertext producing the plaintext            
            for (int i = 0; i < blockSize; i++)
            {
                outBytes[outOff + i] = (byte)(cfbOutV[i] ^ input[inOff + i]);
            }
            return blockSize;
        }

        public int EncryptBlock(ReadOnlySpan<byte> input, Span<byte> outBytes)
        {
            if ((blockSize) > input.Length)
                throw new DataLengthException("input buffer too short");
            if ((blockSize) > outBytes.Length)
                throw new DataLengthException("output buffer too short");

            cipher.ProcessBlock(cfbV, 0, cfbOutV, 0);
            //
            // XOR the cfbV with the plaintext producing the ciphertext
            //
            for (int i = 0; i < blockSize; i++)
            {
                outBytes[i] = (byte)(cfbOutV[i] ^ input[i]);
            }
            //
            // change over the input block.
            //
            cfbV.AsSpan().Slice(blockSize, cfbV.Length - blockSize).CopyTo(cfbV);
            outBytes.Slice(0, blockSize).CopyTo(cfbV.AsSpan().Slice(cfbV.Length - blockSize));
            return blockSize;
        }

        public int DecryptBlock(ReadOnlySpan<byte> input, Span<byte> outBytes)
        {
            if (blockSize > input.Length)
                throw new DataLengthException("input buffer too short");
            if (blockSize > outBytes.Length)
                throw new DataLengthException("output buffer too short");
            
            cipher.ProcessBlock(cfbV, 0, cfbOutV, 0);

            // change over the input block.            
            cfbV.AsSpan().Slice(blockSize).CopyTo(cfbV);
            input.Slice(0, blockSize).CopyTo(cfbV.AsSpan().Slice(cfbV.Length - blockSize));

            // XOR the cfbV with the ciphertext producing the plaintext            
            for (int i = 0; i < blockSize; i++)
            {
                outBytes[i] = (byte)(cfbOutV[i] ^ input[i]);
            }
            return blockSize;
        }
        /**
        * reset the chaining vector back to the IV and reset the underlying
        * cipher.
        */
        public void Reset()
        {
            Array.Copy(IV, 0, cfbV, 0, IV.Length);
            cipher.Reset();
        }
    }
}
