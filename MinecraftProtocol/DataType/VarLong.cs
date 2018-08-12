using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public static class VarLong
    {

        public static long Convert(byte[] varLong)=>Read(varLong);
        public static byte[] Convert(long value) => Write(value);
        public static byte[] ToVarLong(this long self) => Write(self);

        public static long Read(List<byte> value) => Read(value.ToArray(), out int refuse);
        public static long Read(byte[] value) => Read(value, out int refuse);
        public static long Read(byte[] value,out int endIndex)
        {
            long result = 0;
            byte index = 0;
            do
            {
                result |= (value[index]&127L) << 7 * index++;
                if (index >10 ) throw new OverflowException("VarLong too big");
            } while (index <= value.Length && (value[index-1] & 128)!=0);
            endIndex = index;
            return result;
        }
        /// <summary>
        /// 把一个long转换成Varlong
        /// </summary>
        /// <param name="value">警告,该参数如果为null会直接返回null！</param>
        /// <returns></returns>
        public static byte[] Write(long? value)
        {
            if (value == null)
                return null;
            List<byte> bytes = new List<byte>();
            do
            {
                byte temp = (byte)(value & 127);
                value = (long)(((ulong)value) >> 7);
                if (value != 0)
                    temp |= 128;
                bytes.Add(temp);
            } while (value != 0);
            return bytes.ToArray();
        }

    }
}
