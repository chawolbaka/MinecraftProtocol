using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType
{
    
    public struct VarInt:IEquatable<VarInt>
    {
        private byte[] Bytes;
        private int Data;

        public int Length => Bytes != null ? Bytes.Length : -1;

        public VarInt(int value)
        {
            this.Data = value;
            this.Bytes = null;
        }
        public VarInt(byte[] data,int startIndex)
        {
            this.Bytes = ReadByBytes(data, startIndex, out _);
            this.Data = 0;
        }
        public VarInt(byte[] data, int startIndex, out int endIndex)
        {
            this.Bytes = ReadByBytes(data, startIndex, out endIndex);
            this.Data = 0;
        }

        //Q:为什么写这坨operator的?
        //A:我想把它当成int用
        public static VarInt operator +(VarInt right, VarInt left) => new VarInt(right.ToInt() + left.ToInt());
        public static VarInt operator +(int right, VarInt left) => new VarInt(right + left.ToInt());
        public static VarInt operator +(VarInt right, int left) => new VarInt(right.ToInt() + left);

        public static VarInt operator -(VarInt right, VarInt left) => new VarInt(right.ToInt() - left.ToInt());
        public static VarInt operator -(int right, VarInt left) => new VarInt(right - left.ToInt());
        public static VarInt operator -(VarInt right, int left) => new VarInt(right.ToInt() - left);
        
        public static VarInt operator *(VarInt right, VarInt left) => new VarInt(right.ToInt() * left.ToInt());
        public static VarInt operator *(int right, VarInt left) => new VarInt(right * left.ToInt());
        public static VarInt operator *(VarInt right, int left) => new VarInt(right.ToInt() * left);

        public static VarInt operator /(VarInt right, VarInt left) => new VarInt(right.ToInt() / left.ToInt());
        public static VarInt operator /(int right, VarInt left) => new VarInt(right / left.ToInt());
        public static VarInt operator /(VarInt right, int left) => new VarInt(right.ToInt() / left);

        public static bool operator >(VarInt right, VarInt left) => right.ToInt() > left.ToInt();
        public static bool operator >(int right, VarInt left) => right > left.ToInt();
        public static bool operator >(VarInt right, int left) => right.ToInt() > left;

        public static bool operator <(VarInt right, VarInt left) => right.ToInt() < left.ToInt();
        public static bool operator <(int right, VarInt left) => right < left.ToInt();
        public static bool operator <(VarInt right, int left) => right.ToInt() < left;

        public static bool operator >=(VarInt right, VarInt left) => right.ToInt() >= left.ToInt();
        public static bool operator >=(int right, VarInt left) => right >= left.ToInt();
        public static bool operator >=(VarInt right, int left) => right.ToInt() >= left;

        public static bool operator <=(VarInt right, VarInt left) => right.ToInt() <= left.ToInt();
        public static bool operator <=(int right, VarInt left) => right <= left.ToInt();
        public static bool operator <=(VarInt right, int left) => right.ToInt() <= left;

        public static bool operator ==(VarInt right, VarInt left) => right.Equals(left);
        public static bool operator ==(int right, VarInt left) => left.Equals(right);
        public static bool operator ==(VarInt right, int left) => right.Equals(left);

        public static bool operator !=(VarInt right, VarInt left) => !right.Equals(left);
        public static bool operator !=(int right, VarInt left) => !left.Equals(right);
        public static bool operator !=(VarInt right, int left) => !right.Equals(left);

        public static VarInt operator ++(VarInt value) => new VarInt(value.ToInt() + 1);
        public static VarInt operator --(VarInt value) => new VarInt(value.ToInt() - 1);

        public static explicit operator VarInt(int value) => new VarInt(value);
        public static explicit operator int(VarInt value) => value.ToInt();
        public static explicit operator byte[] (VarInt value) => value.ToBytes();

        public int ToInt()
        {
            if (Data==0&&Bytes!=null&&Bytes[Bytes.Length-1]!=0)
            {
                for (int i = 0; i < Bytes.Length; i++)
                {
                    Data |= (Bytes[i] & 127) << i * 7;
                }
            }
            return Data;
        }
        public byte[] ToBytes()
        {
            if (this.Bytes == null) Bytes = ReadByInt(Data);
            return this.Bytes;
        }
        public override string ToString() => ToInt().ToString();
        public override bool Equals(object obj)
        {
            if (obj is VarInt)
                return Equals((VarInt)obj);
            else
                return false;
        }
        public bool Equals(int value)
        {
            if (Bytes != null && Data == 0)
                return ToInt() == value;
            else
                return Data == value;
        }
        public bool Equals(VarInt varint)
        {
            //这边可以理解成Data它是不是null,如果data是null就通过Bytes对比
            if (Data==0&&Bytes!=null&&Bytes[0]!=0)
            {
                if (varint.Bytes == null) varint.ToBytes();
                if (varint.Length != Bytes.Length) return false;
                byte[] temp = varint.ToBytes();
                for (int i = 0; i < varint.Length; i++)
                {
                    if (temp[i] != Bytes[i]) return false;
                }
                return true;
            }
            else 
            {
                if (Bytes == null)
                    return Data == varint.ToInt();
                else
                    return ToInt() == varint.ToInt();
            }
        }
        public static VarInt Parse(string s) => new VarInt(int.Parse(s));
        public static bool TryParse(string s, out VarInt result)
        {
            bool CanParse = int.TryParse(s, out int number);
            result = new VarInt(number);
            return CanParse;
        }
        public override int GetHashCode()
        {
            if (Bytes == null)
                return Data;
            else
                return ToInt();
        }

        private static byte[] ReadByBytes(byte[] data, int startIndex, out int endIndex)
        {
            //这个方法的使用场景是从一堆byte里面取出一段varint,不会做任何转换.
            List<byte> result = new List<byte>();
            for (int i = 0; i < 5; i++)
            {
                result.Add(data[i]);
                startIndex++;
                if ((data[i] & 0b1000_0000) == 0b0000_0000)
                {
                    endIndex = startIndex;
                    return result.ToArray();
                }
            }
            throw new OverflowException("VarInt too big");
        }
        private static byte[] ReadByInt(int value)
        {
            List<byte> bytes = new List<byte>();
            while ((value & -128) != 0)
            {
                bytes.Add((byte)(value & 127 | 128));
                value = (int)(((uint)value) >> 7);
            }
            bytes.Add((byte)value);
            return bytes.ToArray();
        }
    }
}
