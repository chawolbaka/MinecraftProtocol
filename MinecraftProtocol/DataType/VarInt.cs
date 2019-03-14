using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType
{
    public struct VarInt
    {
        private byte[] Data;
        private int? Buffer;

        public int Length => Data.Length;

        public VarInt(int value)
        {
            this.Buffer = null;
            this.Data = ReadByInt(value);
        }
        public VarInt(byte[] data,int startIndex)
        {
            this.Buffer = null;
            this.Data = ReadByBytes(data, startIndex, out _);
        }
        public VarInt(byte[] data, int startIndex, out int endIndex)
        {
            this.Buffer = null;
            this.Data = ReadByBytes(data, startIndex, out endIndex);
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
        public static bool operator ==(int right, VarInt left) => new VarInt(right).Equals(left);
        public static bool operator ==(VarInt right, int left) => right.Equals(new VarInt(left));

        public static bool operator !=(VarInt right, VarInt left) => !right.Equals(left);
        public static bool operator !=(int right, VarInt left) => !(new VarInt(right).Equals(left));
        public static bool operator !=(VarInt right, int left) => !right.Equals(new VarInt(left));

        public static VarInt operator ++(VarInt v) => new VarInt(v.ToInt() + 1);
        public static VarInt operator --(VarInt v) => new VarInt(v.ToInt() - 1);

        public static explicit operator VarInt(int value) => new VarInt(value);
        public static explicit operator int(VarInt value) => value.ToInt();
        public static explicit operator byte[] (VarInt value) => value.ToBytes();

        public int ToInt()
        {
            if (Buffer == null)
            {
                Buffer = 0;
                for (int i = 0; i < Data.Length; i++)
                {
                    Buffer |= (Data[i] & 127) << i * 7;
                }
            }
            return Buffer.Value;
        }
        public byte[] ToBytes() => Data;
        public override string ToString() => ToInt().ToString();
        public override bool Equals(object obj)
        {
            if (obj is VarInt)
                return Equals((VarInt)obj);
            else
                return false;
        }
        public bool Equals(VarInt obj)
        {
            if (obj.Length != Data.Length) return false;
            for (int i = 0; i < obj.Length; i++)
            {
                if (obj.ToBytes()[i] != Data[i]) return false;
            }
            return true;
        }

        public static VarInt Parse(string s) => new VarInt(int.Parse(s));
        public static bool TryParse(string s, out VarInt result)
        {
            bool CanParse = int.TryParse(s, out int number);
            result = new VarInt(number);
            return CanParse;
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

        public override int GetHashCode()
        {
            //看见个绿色的波浪线要我重写GetHasCode,这我怎么重写哇QAQ,不会写...
            return base.GetHashCode();
        }
    }
}
