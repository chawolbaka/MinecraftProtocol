using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Protocol.Generator
{
    [Generator]
    public class ByteReaderGenerator: ISourceGenerator
    {
        private const string AttributeText = @"
using System;
namespace MinecraftProtocol.IO
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal class ByteReaderAttribute : Attribute
    {
    }
}
";


        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((c) => c.AddSource($"ByteReaderAttribute.cs", AttributeText));
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver && receiver.MatchList.Count>0))
                return;
            foreach (var item in receiver.MatchList)
                context.AddSource($"{item.ClassName}.Methods.cs", $@"
using System;
using System.IO;
using System.Text;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.IO.NBT;
using MinecraftProtocol.IO.NBT.Tags;

namespace {item.Namespace}
{{

    public ref partial struct {item.ClassName}
    {{
        public int Count => _data.Length;

        public byte this[int index] => _data[index];

        public bool IsReadToEnd => _offset >= _data.Length;
        public int Position
        {{
            get => _offset;
            set
            {{
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Position), ""负数"");
                if (value > _data.Length)
                    throw new ArgumentOutOfRangeException(nameof(Position), ""超出数组边界"");
                _offset = value;
            }}
        }}

        public bool ReadBoolean() => _data[_offset++] == 0x01;

        public sbyte ReadByte() => (sbyte)_data[_offset++];

        public byte ReadUnsignedByte() => _data[_offset++];

        public short ReadShort() => (short)(_data[_offset++] << 8 | _data[_offset++]);

        public ushort ReadUnsignedShort() => (ushort)(_data[_offset++] << 8 | _data[_offset++]);

        public int ReadInt()
        {{
            return _data[_offset++] << 24 |
                   _data[_offset++] << 16 |
                   _data[_offset++] << 08 |
                   _data[_offset++];

        }}

        public uint ReadUnsignedInt()
        {{
            return ((uint)_data[_offset++]) << 24 |
                   ((uint)_data[_offset++]) << 16 |
                   ((uint)_data[_offset++]) << 08 |
                   _data[_offset++];

        }}

        public long ReadLong()
        {{
            return ((long)_data[_offset++]) << 56 |
                  ((long)_data[_offset++]) << 48 |
                  ((long)_data[_offset++]) << 40 |
                  ((long)_data[_offset++]) << 32 |
                  ((long)_data[_offset++]) << 24 |
                  ((long)_data[_offset++]) << 16 |
                  ((long)_data[_offset++]) << 08 |
                  _data[_offset++];
        }}

        public ulong ReadUnsignedLong()
        {{
            return ((ulong)_data[_offset++]) << 56 |
                   ((ulong)_data[_offset++]) << 48 |
                   ((ulong)_data[_offset++]) << 40 |
                   ((ulong)_data[_offset++]) << 32 |
                   ((ulong)_data[_offset++]) << 24 |
                   ((ulong)_data[_offset++]) << 16 |
                   ((ulong)_data[_offset++]) << 08 |
                   _data[_offset++];
        }}

        public float ReadFloat()
        {{
            const int size = sizeof(float);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _data[_offset + 3 - i];
            _offset += size;
            return BitConverter.ToSingle(buffer);
        }}

        public double ReadDouble()
        {{
            const int size = sizeof(double);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _data[_offset + 3 - i];
            _offset += size;
            return BitConverter.ToDouble(buffer);
        }}

        public string ReadString()
        {{
            int length = ReadVarInt();
            var x = _data.Slice(_offset, length);
            string result = Encoding.UTF8.GetString(x);
            _offset += length;
            return result;
        }}

        public int ReadVarShort()
        {{
            int result = VarShort.Read(_data.Slice(_offset), out int length);
            _offset += length;
            return result;
        }}

        public int ReadVarInt()
        {{
            int result = VarInt.Read(_data.Slice(_offset), out int length);
            _offset += length;
            return result;
        }}

        public long ReadVarLong()
        {{
            long result = VarLong.Read(_data.Slice(_offset), out int length);
            _offset += length;
            return result;
        }}


        public UUID ReadUUID()
        {{
            return new UUID(ReadLong(), ReadLong());
        }}

        public Position ReadPosition(int protocolVersion)
        {{
            return new Position(ReadUnsignedLong(), protocolVersion);
        }}

        public Identifier ReadIdentifier()
        {{
            return Identifier.Parse(ReadString());
        }}

        public byte[] ReadBytes(int length)
        {{
            byte[] result = _data.Slice(_offset, length).ToArray();
            _offset += length;
            return result;
        }}
        public byte[] ReadByteArray(int protocolVersion)
        {{
            int ArrayLength = protocolVersion >= ProtocolVersions.V14w21a ? ReadVarInt() : ReadShort();
            byte[] result = _data.Slice(_offset, ArrayLength).ToArray();
            _offset += ArrayLength;
            return result;
        }}
        public string[] ReadStringArray(int length)
        {{
            string[] list = new string[length];
            for (int i = 0; i < list.Length; i++)
            {{
                list[i] = ReadString();
            }}
            return list;
        }}

        public string[] ReadStringArray()
        {{
            string[] list = new string[ReadVarInt()];
            for (int i = 0; i < list.Length; i++)
            {{
                list[i] = ReadString();
            }}
            return list;
        }}

        public Identifier[] ReadIdentifierArray()
        {{
            Identifier[] list = new Identifier[ReadVarInt()];
            for (int i = 0; i < list.Length; i++)
            {{
                list[i] = Identifier.Parse(ReadString());
            }}
            return list;
        }}

        public CompoundTag ReadNBT()
        {{
            NBTReader reader = new NBTReader(ref this);
            NBTTagType type = reader.ReadType();
            if (type == NBTTagType.Compound)
                return new CompoundTag().Read(ref reader) as CompoundTag;
            else if (type == NBTTagType.End)
                return new CompoundTag();
            else
                throw new InvalidDataException(""Failed to read nbt"");
        }}

        public string ReadOptionalString()
        {{
            return ReadBoolean() ? ReadString() : null;
        }}

        public byte[] ReadOptionalBytes(int length)
        {{
            return ReadBoolean() ? ReadBytes(length) : null;
        }}
        public byte[] ReadOptionalByteArray(int protocolVersion)
        {{
            return ReadBoolean() ? ReadByteArray(protocolVersion) : null;
        }}


        public ReadOnlySpan<byte> AsSpan()
        {{
            return _data.Slice(_offset);
        }}

        public void SetToEnd()
        {{
            _offset = _data.Length;
        }}

        public void Reset()
        {{
            _offset = 0;
        }}
    }}
}}");
        }

        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<(string Namespace, string ClassName)> MatchList = new List<(string Namespace, string ClassName)>();
            public string Namespace { get; set; }
            public string ClassName { get; set; }

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is StructDeclarationSyntax sds
                && sds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)
                && sds.AttributeLists.Any(a => a.Attributes.Any(x => x.Name.ToString() == "ByteReader"))))
                {
                    MatchList.Add((GetNamespace(sds.Parent), sds.Identifier.ValueText));
                }
            }

            private string GetNamespace(SyntaxNode node)
            {
                if (node is NamespaceDeclarationSyntax nds)
                    return nds.Name.ToString();
                else if (node.Parent != null)
                    return GetNamespace(node.Parent);
                else
                    return "MinecraftProtocol";
            }
        }
    }
}
