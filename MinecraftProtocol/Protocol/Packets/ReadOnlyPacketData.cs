using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MinecraftProtocol.Protocol.Packets
{
    public class ReadOnlyPacketData: ReadOnlyCollection<byte>
    {

        private ReadOnlyMemory<byte> Buffer
        {
            get
            {
                if (!_buffer.HasValue || _buffer.Value.Length != Items.Count)
                {
                    byte[] data = new byte[Items.Count];
                    CopyTo(data, 0);
                    _buffer = new ReadOnlyMemory<byte>(data);
                }
                return _buffer.Value;
            }
        }
        private ReadOnlyMemory<byte>? _buffer;

        public ReadOnlyPacketData(IList<byte> list) : base(list) { }

        public ReadOnlySpan<byte> Slice(int start) => Buffer.Span.Slice(start);
        public ReadOnlySpan<byte> Slice(int start, int length) => Buffer.Span.Slice(start, length);

        public ReadOnlySpan<byte> ToSpan() => Buffer.Span;

        public byte[] ToArray()
        {
            byte[] bytes = new byte[Count];
            CopyTo(bytes, 0);
            return bytes;
        }

    }
}
