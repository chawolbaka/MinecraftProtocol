using System;

namespace BouncyCastle.Crypto
{

    /// <summary>
    /// A wrapper class that allows block ciphers to be used to process data in
    /// a piecemeal fashion. The BufferedBlockCipher outputs a block only when the
    /// buffer is full and more data is being added, or on a doFinal.
    /// <para>
    /// Note: in the case where the underlying cipher is either a CFB cipher or an
    /// OFB one the last block may not be a multiple of the block size.
    /// </para>
    /// </summary>
    public class BufferedBlockCipher : BufferedCipherBase
	{
        internal byte[]			buf;
		internal int			bufOff;
		internal bool			forEncryption;
		internal IBlockCipher	cipher;

		protected BufferedBlockCipher() { }

        /// <summary>
        /// Create a buffered block cipher without padding.
        /// </summary>
        /// <param name="cipher">the underlying block cipher this buffering object wraps. false otherwise.</param>
        public BufferedBlockCipher(IBlockCipher cipher)
		{
			if (cipher == null)
				throw new ArgumentNullException(nameof(cipher));

			this.cipher = cipher;
			buf = new byte[cipher.GetBlockSize()];
			bufOff = 0;
		}

        public override string AlgorithmName => cipher.AlgorithmName;

        /// <summary>
        /// initialise the cipher.
        /// </summary>
        /// <param name="forEncryption">if true the cipher is initialised for encryption, if false for decryption.</param>
        /// <param name="parameters">the key and other data required by the cipher.</param>
        /// <exception cref="ArgumentException">if the parameters argument is inappropriate.</exception>
        public override void Init(bool forEncryption, ICipherParameters parameters)
        {
            this.forEncryption = forEncryption;
            Reset();
            cipher.Init(forEncryption, parameters);
        }

        /// <summary>
        /// return the blocksize for the underlying cipher.
        /// </summary>
        public override int GetBlockSize()
		{
			return cipher.GetBlockSize();
		}

        /// <summary>
        /// return the size of the output buffer required for an update an input of len bytes.
        /// </summary>
        /// <param name="length">the length of the input.</param>
        public override int GetUpdateOutputSize(int length)
        {
			int total = length + bufOff;
			int leftOver = total % buf.Length;
			return total - leftOver;
		}

        /// <summary>
        /// return the size of the output buffer required for an update plus a doFinal with an input of len bytes.
        /// </summary>
        /// <param name="length">the length of the input.</param>
        /// <returns>the space required to accommodate a call to update and doFinal with len bytes of input.</returns>
		public override int GetOutputSize(int length)
		{
			// Note: Can assume IsPartialBlockOkay is true for purposes of this calculation
			return length + bufOff;
		}

        /// <summary>
        /// process a single byte, producing an output block if necessary.
        /// </summary>
        /// <param name="input">the input byte.</param>
        /// <param name="output">the space for any output that might be produced.</param>
        /// <param name="outOff">the offset from which the output will be copied.</param>
        /// <returns>the number of output bytes copied to out.</returns>
        /// <exception cref="DataLengthException">if there isn't enough space in out.</exception>
        /// <exception cref="InvalidOperationException">if the cipher isn't initialised.</exception>
        public override int ProcessByte(byte input, byte[] output, int outOff)
        {
			buf[bufOff++] = input;

			if (bufOff == buf.Length)
			{
				if ((outOff + buf.Length) > output.Length)
					throw new DataLengthException("output buffer too short");

				bufOff = 0;
				return cipher.ProcessBlock(buf, 0, output, outOff);
			}

			return 0;
		}

        public override byte[] ProcessByte(byte input)
		{
			int outLength = GetUpdateOutputSize(1);

			byte[] outBytes = outLength > 0 ? new byte[outLength] : null;

			int pos = ProcessByte(input, outBytes, 0);

			if (outLength > 0 && pos < outLength)
			{
				//byte[] tmp = new byte[pos];
				//Array.Copy(outBytes, 0, tmp, 0, pos);
				//outBytes = tmp;
                outBytes = outBytes.AsSpan(0, pos).ToArray();
            }

			return outBytes;
		}

        public override byte[] ProcessBytes(byte[] input, int inOff, int length)
        {
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (length < 1)
				return null;

			int outLength = GetUpdateOutputSize(length);

			byte[] outBytes = outLength > 0 ? new byte[outLength] : null;

			int pos = ProcessBytes(input, inOff, length, outBytes, 0);

			if (outLength > 0 && pos < outLength)
                outBytes = outBytes.AsSpan(0, pos).ToArray();
			
			return outBytes;
		}

        public override Span<byte> ProcessBytes(ReadOnlySpan<byte> input, int inOff, int length)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (length < 1)
                return null;

            int outLength = GetUpdateOutputSize(length);

            Span<byte> outBytes = outLength > 0 ? new byte[outLength] : null;

            int pos = ProcessBytes(input, inOff, length, outBytes, 0);

            if (outLength > 0 && pos < outLength)
                outBytes = outBytes.Slice(0, pos);

            return outBytes;
        }

        /// <summary>
        /// process an array of bytes, producing output if necessary.
        /// </summary>
        /// <param name="input">the input byte array.</param>
        /// <param name="inOff">the offset at which the input data starts.</param>
        /// <param name="length">the number of bytes to be copied out of the input array.</param>
        /// <param name="output">the space for any output that might be produced.</param>
        /// <param name="outOff">the offset from which the output will be copied.</param>
        /// <returns>the number of output bytes copied to out.</returns>
        /// <exception cref="DataLengthException">if there isn't enough space in out.</exception>
        /// <exception cref="InvalidOperationException">if the cipher isn't initialised.</exception>
        public override int ProcessBytes(byte[] input, int inOff, int length, byte[] output, int outOff)
        {
			if (length < 1)
			{
				if (length < 0)
					throw new ArgumentException("Can't have a negative input length!");

				return 0;
			}

			int blockSize = GetBlockSize();
			int outLength = GetUpdateOutputSize(length);

			if (outLength > 0)
			{
                Check.OutputLength(output, outOff, outLength, "output buffer too short");
			}

            int resultLen = 0;
			int gapLen = buf.Length - bufOff;
			if (length > gapLen)
			{
				Array.Copy(input, inOff, buf, bufOff, gapLen);
				resultLen += cipher.ProcessBlock(buf, 0, output, outOff);
				bufOff = 0;
				length -= gapLen;
				inOff += gapLen;
				while (length > buf.Length)
				{
					resultLen += cipher.ProcessBlock(input, inOff, output, outOff + resultLen);
					length -= blockSize;
					inOff += blockSize;
				}
			}
			Array.Copy(input, inOff, buf, bufOff, length);
			bufOff += length;
			if (bufOff == buf.Length)
			{
				resultLen += cipher.ProcessBlock(buf, 0, output, outOff + resultLen);
				bufOff = 0;
			}
			return resultLen;
		}
        public int ProcessBytes(ReadOnlySpan<byte> input, int inOff, int length, Span<byte> output, int outOff)
        {
            if (length < 1)
            {
                if (length < 0)
                    throw new ArgumentException("Can't have a negative input length!");

                return 0;
            }

            int blockSize = GetBlockSize();
            int outLength = GetUpdateOutputSize(length);

            if (outLength > 0)
            {
                Check.OutputLength(output, outOff, outLength, "output buffer too short");
            }

            int resultLen = 0;
            int gapLen = buf.Length - bufOff;
            if (length > gapLen)
            {
                //Array.Copy(input, inOff, buf, bufOff, gapLen);
                input.Slice(inOff, gapLen).CopyTo(buf.AsSpan(bufOff));
                resultLen += cipher.ProcessBlock(buf, output.Slice(outOff));
                bufOff = 0;
                length -= gapLen;
                inOff += gapLen;
                while (length > buf.Length)
                {
                    resultLen += cipher.ProcessBlock(input.Slice(inOff), output.Slice(outOff + resultLen));
                    length -= blockSize;
                    inOff += blockSize;
                }
            }
            //Array.Copy(input, inOff, buf, bufOff, length);
            input.Slice(inOff, length).CopyTo(buf.AsSpan().Slice(bufOff));
            bufOff += length;
            if (bufOff == buf.Length)
            {
                resultLen += cipher.ProcessBlock(buf, output.Slice( outOff + resultLen));
                bufOff = 0;
            }
            return resultLen;
        }

        public override byte[] DoFinal()
		{
			byte[] outBytes = EmptyBuffer;

			int length = GetOutputSize(0);
			if (length > 0)
			{
				outBytes = new byte[length];

				int pos = DoFinal(outBytes, 0);
				if (pos < outBytes.Length)
				{
					byte[] tmp = new byte[pos];
					Array.Copy(outBytes, 0, tmp, 0, pos);
					outBytes = tmp;
				}
			}
			else
			{
				Reset();
			}

			return outBytes;
		}

        public override byte[] DoFinal(byte[] input, int inOff, int inLen)
        {
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			int length = GetOutputSize(inLen);

			byte[] outBytes = EmptyBuffer;

			if (length > 0)
			{
				outBytes = new byte[length];

				int pos = (inLen > 0)
					?	ProcessBytes(input, inOff, inLen, outBytes, 0)
					:	0;

				pos += DoFinal(outBytes, pos);

				if (pos < outBytes.Length)
				{
					byte[] tmp = new byte[pos];
					Array.Copy(outBytes, 0, tmp, 0, pos);
					outBytes = tmp;
				}
			}
			else
			{
				Reset();
			}

			return outBytes;
		}

        /// <summary>
        /// Process the last block in the buffer.
        /// </summary>
        /// <param name="output">the array the block currently being held is copied into.</param>
        /// <param name="outOff">the offset at which the copying starts.</param>
        /// <returns>the number of output bytes copied to out.</returns>
        /// <exception cref="DataLengthException">if there is insufficient space in out for the output, or the input is not block size aligned and should be.</exception>
        /// <exception cref="DataLengthException">if the input is not block size</exception>
        /// <exception cref="InvalidOperationException">if the underlying cipher is not initialised.</exception>
        /// <exception cref="InvalidCipherTextException">if padding is expected and not found.</exception>
        public override int DoFinal(byte[] output, int outOff)
        {
			try
			{
				if (bufOff != 0)
				{
                    Check.DataLength(!cipher.IsPartialBlockOkay, "data not block size aligned");
                    Check.OutputLength(output, outOff, bufOff, "output buffer too short for DoFinal()");

                    // NB: Can't copy directly, or we may write too much output
					cipher.ProcessBlock(buf, 0, buf, 0);
					Array.Copy(buf, 0, output, outOff, bufOff);
				}

				return bufOff;
			}
			finally
			{
				Reset();
			}
		}

        /// <summary>
        /// Reset the buffer and cipher. After resetting the object is in the same
        /// state as it was after the last init (if there was one).
        /// </summary>
		public override void Reset()
		{
			Array.Clear(buf, 0, buf.Length);
			bufOff = 0;

			cipher.Reset();
		}
	}
}
