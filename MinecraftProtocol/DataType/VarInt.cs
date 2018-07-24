using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public static class VarInt
    {
        
    //    private byte[] _data;
    //    public int Length { get { return _data.Length; } }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="integer">这个参数里面的数字会被转换成Varint</param>
    //    public VarInt(int integer)
    //    {
    //        this._data = Write(integer);
    //    }
    //    /// <summary>
    //    /// 把VarInt转换成int32
    //    /// </summary>
    //    public int ToInt() => Read(this._data);
    //    /// <summary>
    //    /// 获取Varint的字节数组
    //    /// </summary>
    //    /// <returns></returns>
    //    public byte[] GetBytes() => this._data;

    //    /// <summary>
    //    /// 把一个VarInt转换成int32
    //    /// </summary>
    //    public static int Convert(VarInt varInt) => Read(varInt.GetBytes());
        /// <summary>
        /// 把一个VarInt转换成int32
        /// </summary>
        public static int Convert(byte[] varInt) => Read(varInt);
        /// <summary>
        /// 把一个int转换成Varint
        /// </summary>
        public static byte[] Convert(int integer) => Write(integer);

        /// <summary>
        /// 把int转换为VarInt
        /// </summary>
        /// <param name="value">警告,该参数如果为null会直接返回null！</param>
        /// <returns></returns>
        public static byte[] Write(int? value)
        {
            if (value == null)
                return null;
            List<byte> bytes = new List<byte>();
            while ((value & -128) != 0)
            {
                bytes.Add((byte)(value & 127 | 128));
                value = (int)(((uint)value) >> 7);
            }
            bytes.Add((byte)value);
            return bytes.ToArray();
        }

        public static int Read(List<byte> varInt) => Read(varInt.ToArray(), 0, out int refuse);
        public static int Read(byte[] varInt) => Read(varInt, 0, out int refuse);
        public static int Read(byte[] varInt,int startIndex) => Read(varInt,startIndex, out int refuse);
        /// <summary>
        /// 从一个Byte数组中把Varint转换成int
        /// </summary>
        /// <param name="startIndex">起始位置</param>
        /// <returns>
        /// integer:转换出来的数字
        /// indexEND:这个Varint的结束位置
        /// </returns>
        public static int Read(byte[] data,int startIndex,out int endIndex)
        {
            int result = 0;
            int i = 0;
            while (true)
            {
                result |= (data[startIndex] & 127) << i++ * 7;
                if (i > 5) throw new OverflowException("VarInt too big");
                if ((data[startIndex] & 128) != 128)
                {
                    startIndex++;
                    break;
                }
                startIndex++;
            }
            endIndex = startIndex;
            return result;
        }

            
    }
}
