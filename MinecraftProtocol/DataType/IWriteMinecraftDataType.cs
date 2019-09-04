using System;

namespace MinecraftProtocol.DataType
{
    interface IWriteMinecraftDataType
    {
        /// <summary>True is encoded as 0x01, false as 0x00.</summary>
        void WriteBoolean(bool value);

        /// <summary>An integer between -128 and 127</summary>
        /// <remarks>Signed 8-bit integer, two's complement</remarks>
        void WriteByte(sbyte value);

        /// <summary>An integer between -128 and 127</summary>
        /// <remarks>Unsigned 8-bit integer</remarks>
        void WriteUnsignedByte(byte value);

        /// <summary>An integer between -32768 and 32767</summary>
        /// <remarks>Signed 16-bit integer, two's complement</remarks>
        void WriteShort(short value);

        /// <summary>An integer between 0 and 65535</summary>
        /// <remarks>Unsigned 16-bit integer</remarks>
        void WriteUnsignedShort(ushort value);

        /// <summary>An integer between -2147483648 and 2147483647</summary>
        /// <remarks>Signed 32-bit integer, two's complement</remarks>
        void WriteInt(int value);

        /// <summary>An integer between -9223372036854775808 and 9223372036854775807</summary>
        /// <remarks>Signed 64-bit integer, two's complement</remarks>
        void WriteLong(long value);

        /// <summary>A single-precision 32-bit IEEE 754 floating point number</summary>
        void WriteFloat(float value);

        /// <summary>A double-precision 64-bit IEEE 754 floating point number</summary>
        void WriteDouble(double value);

        /// <summary>UTF-8 string prefixed with its size in bytes as a VarInt. Maximum length of n characters, which varies by context; up to n × 4 bytes can be used to encode n characters and both of those limits are checked. Maximum n value is 32767. The + 3 is due to the max size of a valid length VarInt.</summary>
        void WriteString(string value);

        ///// <summary>Encoded as a String with max length of 32767.</summary>
        //void WriteChat(Chat value);

        ///// <summary>Encoded as a String with max length of 32767.</summary>
        //void WriteIdentifier(Identifier value);

        /// <summary>An integer between -2147483648 and 2147483647</summary>
        /// <remarks>Variable-length data encoding a two's complement signed 32-bit integer</remarks>
        void WriteVarInt(VarInt value);

        /// <summary>An integer between -9223372036854775808 and 9223372036854775807</summary>
        /// <remarks>Variable-length data encoding a two's complement signed 32-bit integer</remarks>
        void WriteVarLong(long value);

        /// <summary>Encoded as an unsigned 128-bit integer (or two unsigned 64-bit integers: the most significant 64 bits and then the least significant 64 bits)</summary>
        void WriteUUID(UUID value);

        void WriteByteArray(byte[] value, int protocolVersion);

    }
}
