using System;

namespace BouncyCastle.Crypto.Parameters
{
    public class ParametersWithIV : ICipherParameters
    {
        public ICipherParameters Parameters { get; }
     
        private readonly byte[] InitializationVector;

        public ParametersWithIV(ICipherParameters parameters, byte[] iv) : this(parameters, iv, 0, iv.Length) { }
        public ParametersWithIV(ICipherParameters parameters, byte[] iv, int ivOff, int ivLen)
        {
            // NOTE: 'parameters' may be null to imply key re-use
            if (iv == null)
                throw new ArgumentNullException(nameof(iv));

            this.Parameters = parameters;
            this.InitializationVector = ivOff == 0 && ivLen == iv.Length ? (byte[])iv.Clone() : iv.AsSpan().Slice(ivOff, ivLen).ToArray();
        }

        public byte[] GetIV() => (byte[])InitializationVector.Clone();
        
    }
}
