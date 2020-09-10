using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MinecraftProtocol.Compression;

namespace MinecraftProtocol.IO
{
    public class MinecraftMemoryStream : MemoryStream
    {
        public MinecraftMemoryStream() : base() { }
        public MinecraftMemoryStream(byte[] buffer) : base(buffer) { }
        public MinecraftMemoryStream(int capacity) : base(capacity) { }
        public MinecraftMemoryStream(byte[] buffer, bool writable) : base(buffer, writable) { }
        public MinecraftMemoryStream(byte[] buffer, int index, int count) : base(buffer, index, count) { }
        public MinecraftMemoryStream(byte[] buffer, int index, int count, bool writable) : base(buffer, index, count, writable) { }
        public MinecraftMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) : base(buffer, index, count, writable, publiclyVisible) { }

        #region Read Minecraft Data Types
        public virtual bool ReadBoolean() => ReadByte() == 0x01;
        public virtual sbyte ReadSignedByte() => (sbyte)ReadByte();
        public virtual byte ReadUnsignedByte() => (byte)ReadByte();
        public virtual short ReadShort()
        {
            Span<byte> buffer = new byte[2];
            Read(buffer);
            return (short)(buffer[0] << 8 | buffer[1]);
        }
        public virtual ushort ReadUnsignedShort()
        {
            Span<byte> buffer = new byte[2];
            Read(buffer);
            return (ushort)(buffer[0] << 8 | buffer[1]);
        }
        public virtual int ReadInt()
        {
            Span<byte> buffer = new byte[4];
            Read(buffer);
            return buffer[0] << 24 |
                    buffer[1] << 16 |
                    buffer[2] << 08 |
                    buffer[3];
        }
        public virtual long ReadLong()
        {
            Span<byte> buffer = new byte[8];
            Read(buffer);
            return
                ((long)buffer[0]) << 56 |
                ((long)buffer[1]) << 48 |
                ((long)buffer[2]) << 40 |
                ((long)buffer[3]) << 32 |
                ((long)buffer[4]) << 24 |
                ((long)buffer[5]) << 16 |
                ((long)buffer[6]) << 08 |
                buffer[7];
        }
        public virtual float ReadFloat()
        {
            Span<byte> buffer = new byte[sizeof(float)];
            Read(buffer); buffer.Reverse();
            return BitConverter.ToSingle(buffer);
        }
        public virtual double ReadDouble()
        {
            Span<byte> buffer = new byte[sizeof(double)];
            Read(buffer); buffer.Reverse();
            return BitConverter.ToDouble(buffer);
        }
        public virtual string ReadString()
        {
            Span<byte> buffer = new byte[VarInt.Read(this)];
            Read(buffer);
            return Encoding.UTF8.GetString(buffer);
        }
        public virtual int ReadVarShort()
        {
            return VarShort.Read(this);
        }
        public virtual int ReadVarInt()
        {
            return VarInt.Read(this);
        }
        public virtual long ReadVarLong()
        {
            return VarLong.Read(this);
        }
        public virtual string[] ReadStringArray()
        {
            string[] buffer = new string[VarInt.Read(this)];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = ReadString();
            return buffer;
        }
        public virtual byte[] ReadByteArray()
        {
            byte[] buffer = new byte[VarInt.Read(this)];
            Read(buffer);
            return buffer;
        }
        [Obsolete("Obsoleted in 14w21a")]
        public virtual byte[] ReadLegacyByteArray()
        {
            byte[] buffer = new byte[ReadShort()];
            Read(buffer);
            return buffer;
        }
        #endregion

        #region Write Minecraft Data Types
        public virtual void WriteBoolean(bool value) => base.WriteByte((byte)(value ? 1 : 0));
        public virtual void WriteSignedByte(sbyte value) => WriteByte((byte)value);
        public virtual void WriteUnsignedByte(byte value) => WriteByte(value);
        public virtual void WriteShort(short value)
        {
            byte[] data = new byte[sizeof(short)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            Write(data);
        }
        public virtual void WriteUnsignedShort(ushort value)
        {
            byte[] data = new byte[sizeof(ushort)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            Write(data);
        }
        public virtual void WriteInt(int value)
        {
            byte[] data = new byte[sizeof(int)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            Write(data);
        }
        public virtual void WriteLong(long value)
        {
            byte[] data = new byte[sizeof(long)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            Write(data);
        }
        public virtual void WriteFloat(float value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }
        public virtual void WriteDouble(double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            Write(data);
        }
        public virtual void WriteString(string value)
        {
            byte[] str = Encoding.UTF8.GetBytes(value);
            Write(VarInt.GetBytes(str.Length));
            Write(str);
        }
        public virtual void WriteVarShort(int value)
        {
            Write(VarShort.GetBytes(value));
        }
        public virtual void WriteVarInt(int value)
        {
            Write(VarInt.GetBytes(value));
        }
        public virtual void WriteVarLong(long value)
        {
            Write(VarLong.GetBytes(value));
        }
        public virtual void WriteStringArray(List<string> stringArray)
        {
            WriteVarInt(stringArray.Count);
            foreach (var str in stringArray)
                WriteString(str);
        }
        public virtual void WriteStringArray(ReadOnlySpan<string> stringArray)
        {
            WriteVarInt(stringArray.Length);
            foreach (var str in stringArray)
                WriteString(str);
        }
        public virtual void WriteByteArray(ReadOnlySpan<byte> byteArray)
        {
            WriteVarInt(byteArray.Length);
            Write(byteArray);
        }
        [Obsolete("Obsoleted in 14w21a")]
        public virtual void WriteLegacyByteArray(ReadOnlySpan<byte> byteArray)
        {
            WriteShort((short)byteArray.Length);
            Write(byteArray);
        }
        #endregion
    }
}
