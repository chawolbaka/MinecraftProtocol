using System;
using System.Collections.Generic;
using System.IO;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Protocol;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// The server sends several of this packet, one for each registry.
    /// It'll keep sending them until the hasMore value is no longer true.
    /// </summary>
    public class RegistryData : IForgeStructure, IEquatable<RegistryData>
    {
        /// <summary>Always 3 for RegistryData.</summary>
        public const byte Discriminator = 3;
        /// <summary>Marks whether another RegistryData packet will be sent after this.</summary>
        public bool HasMore;
        /// <summary>Name of the registry for this packet</summary>
        public string Name;
        /// <summary>Each id</summary>
        public Dictionary<string, int> Ids { get; set; } = new Dictionary<string, int>();
        /// <summary>Each substitution</summary>
        public List<string> Substitutions { get; set; } = new List<string>();
        /// <summary>Each dummy? May not be present in older versions of forge</summary>
        public List<string> Dummies { get; set; }

        public RegistryData() { }
        public RegistryData(bool hasMore, string name, Dictionary<string, int> ids, List<string> substitutions)
        {
            HasMore = hasMore;
            Name = name;
            Ids = new Dictionary<string, int>(ids ?? throw new ArgumentNullException(nameof(ids)));
            Substitutions = new List<string>(substitutions ?? throw new ArgumentNullException(nameof(substitutions)));
        }
        public RegistryData(bool hasMore, string name, Dictionary<string, int> ids, List<string> substitutions, List<string> dummies)
        {
            HasMore = hasMore;
            Name = name;
            Dummies = dummies;
            Ids = new Dictionary<string, int>(ids ?? throw new ArgumentNullException(nameof(ids)));
            Substitutions = new List<string>(substitutions ?? throw new ArgumentNullException(nameof(substitutions)));
        }

        public byte[] ToBytes()
        {
            byte[] data;
            using (MinecraftMemoryStream ms = new MinecraftMemoryStream())
            {
                ms.WriteByte(Discriminator);
                ms.WriteBoolean(HasMore);
                ms.WriteString(Name);

                ms.WriteVarInt(Ids.Count);
                foreach (var id in Ids)
                {
                    ms.WriteString(id.Key);
                    ms.WriteVarInt(id.Value);
                }

                ms.WriteStringArray(Substitutions);
                
                if (Dummies != null && Dummies.Count > 0)
                    ms.WriteStringArray(Dummies);
                
                data = ms.ToArray();
            }
            return data;
        }
        /// <summary>只读取HasMore字段</summary>
        public static bool ReadHasMore(List<byte> data) => ProtocolHandler.ReadBoolean(data ?? throw new ArgumentNullException(nameof(data)), 1);
        public static RegistryData Read(ReadOnlySpan<byte> data)
        {
            if (data.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "data length too short");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            RegistryData RD = new RegistryData();            
            data = data.Slice(1)
                .ReadBoolean(out RD.HasMore)
                .ReadString(out RD.Name)
                .ReadVarInt(out int IdsCount);

            for (int i = 0; i < IdsCount; i++)
            {
                data = data.ReadString(out string name).ReadVarInt(out int id);
                RD.Ids.Add(name, id);
            }
            data = data.ReadStringArray(out string[] substitutions);
            RD.Substitutions.AddRange(substitutions);
            if (data.Length > 0)
            {
                data.ReadStringArray(out string[] dummies);
                RD.Dummies = new List<string>(dummies);
            }  
            return RD;
        }

        public override bool Equals(object obj) => Equals(obj as RegistryData);
        public bool Equals(RegistryData other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null || other.HasMore != HasMore || other.Name != Name)
                return false;


            return
                ProtocolHandler.Compare(Ids, other?.Ids) &&
                ProtocolHandler.Compare(Dummies, other?.Dummies) &&
                ProtocolHandler.Compare(Substitutions, other?.Substitutions);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(HasMore, Name, Ids, Substitutions, Dummies);
        }
    }
}
