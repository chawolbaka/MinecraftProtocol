using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Utils
{
    public static class CollectionUtils
    {
        /// <summary>
        /// 拼接Byte数组
        /// </summary>
        public static byte[] ConcatBytes(params ICollection<byte>[] bytes)
        {
            
            int length = 0, offset = 0;
            foreach (var array in bytes)
                length += array.Count;
            byte[] buffer = new byte[length];
            foreach (var array in bytes)
            {
                if (array != null)
                    array.CopyTo(buffer, offset);
                offset += array.Count;
            }
            return buffer;
        }

        /// <summary>
        /// 对比两个Dictionary的值是否相等
        /// </summary>
        public static bool Compare<K, V>(IDictionary<K, V> a, IDictionary<K, V> b)
        {
            if (a is null && b is null || ReferenceEquals(a, b)) return true;
            if ((a is null && b != null) || (a != null && b is null)) return false;
            if (a.Count != b.Count) return false;
            if (a.Count == 0 && b.Count == 0) return true;
            if (a.GetEnumerator().Current is IEquatable<V> && b.GetEnumerator().Current is IEquatable<V>)
            {
                foreach (var value in a)
                    if (!b.ContainsKey(value.Key) || !((IEquatable<V>)b[value.Key]).Equals(value.Value)) return false;
            }
            else
            {
                foreach (var value in a)
                    if (!b.ContainsKey(value.Key) || !b[value.Key].Equals(value.Value)) return false;
            }
            return true;
        }

        /// <summary>
        /// 对比两个集合的值是否相等
        /// </summary>
        public static bool Compare<T>(IList<T> a, IList<T> b)
        {
            if (a is null && b is null || ReferenceEquals(a, b)) return true;
            if ((a is null && b != null) || (a != null && b is null)) return false;
            if (a.Count != b.Count) return false;
            if (a.Count == 0 && b.Count == 0) return true;
            if (a[0] is IEquatable<T> && b[0] is IEquatable<T>)
            {
                for (int i = 0; i < a.Count; i++)
                    if (!((IEquatable<T>)a[i]).Equals(b[i])) return false;
            }
            else
            {
                for (int i = 0; i < a.Count; i++)
                    if (!a[i].Equals(b[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// 对比两个Byte数组的值是否相等
        /// </summary>
        public static bool Compare(byte[] b1, byte[] b2)
        {
            if (b1 != null && b2 != null && b1.Length != b2.Length)
                return false;
            if (ReferenceEquals(b1, b2) || (b1.Length == 0 && b2.Length == 0))
                return true;
            for (int i = 0; i < b1.Length; i++)
                if (b1[i] != b2[i])
                    return false;
            return true;
        }

    }
}
