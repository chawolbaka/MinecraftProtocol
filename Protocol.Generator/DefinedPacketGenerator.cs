using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol.Generator
{
    [Generator]
    public partial class DefinedPacketGenerator : ISourceGenerator
    {
        private static readonly string PacketPropertyAttributeNamespace = "MinecraftProtocol.Packets";
        private static readonly string PacketPropertyAttribute = "PacketPropertyAttribute";
        private static readonly string ReadOnlyCompatiblePacket = "ReadOnlyCompatiblePacket";
        private static readonly string ICompatible = "ICompatible";
        private static readonly string CompatiblePacket = "CompatiblePacket";
        private static readonly string Packet = "Packet";

        //SourceGenerator是通过解析源码来获取信息的，所以这边不赋值也一样能正常使用
        private const string AttributeTextShort = @"
using System;
namespace MinecraftProtocol.Packets
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal class PacketPropertyAttribute : Attribute
    {
        public string PropertyName { get; set; }
        public int ConstructorPriority { get; set; }
        public bool IsReadProperty { get; set; }
        public bool IsWriteProperty { get; set; }
        public bool IsOverrideProperty { get; set; }
        public bool IsOptional { get; set; }

        public PacketPropertyAttribute() { }
        public PacketPropertyAttribute(string name) { }
        public PacketPropertyAttribute(string name, int priority) { }
        public PacketPropertyAttribute(string propertyName, int constructorPriority, bool isReadProperty) { }
        public PacketPropertyAttribute(string propertyName, int constructorPriority, bool isReadProperty, bool isWriteProperty) { }
        public PacketPropertyAttribute(string propertyName, int constructorPriority, bool isReadProperty, bool isWriteProperty, bool isOverrideProperty) { }
        public PacketPropertyAttribute(string propertyName, int constructorPriority, bool isReadProperty, bool isWriteProperty, bool isOverrideProperty, bool isOptional) { }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
            context.RegisterForPostInitialization((c) => c.AddSource("PacketPropertyAttribute.cs", AttributeTextShort));
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // 获取先前的语法接收器 
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
                return;
            List<KeyValuePair<INamedTypeSymbol, GenerateInfo>> InfoList = new List<KeyValuePair<INamedTypeSymbol, GenerateInfo>>();

            // 按 class 对字段进行分组，并生成代码
            foreach (IGrouping<INamedTypeSymbol, KeyValuePair<IFieldSymbol, AttributeProperty>> group in receiver.Fields.GroupBy(f => f.Key.ContainingType))
            {
                InfoList.Add(new KeyValuePair<INamedTypeSymbol, GenerateInfo>(group.Key, new GenerateInfo(group)));
                context.AddSource($"{group.Key.Name}.AutoGenerate.cs", SourceText.From(GenerateClass(group.Key, group, InfoList[InfoList.Count - 1].Value), Encoding.UTF8));
            }
            context.AddSource($"PacketConvert.AutoGenerate.cs", SourceText.From(GeneratePacketConvert(InfoList), Encoding.UTF8));
        }


        private string GenerateClass(INamedTypeSymbol classSymbol, IEnumerable<KeyValuePair<IFieldSymbol, AttributeProperty>> fields,GenerateInfo info)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                return null;
           
            StringBuilder cotr = new StringBuilder();
            StringBuilder tryRead = new StringBuilder();
            StringBuilder source = new StringBuilder($@"using System;
using MinecraftProtocol.Compatible;

namespace {classSymbol.ContainingNamespace.ToDisplayString()}
{{
    public partial class {classSymbol.Name}
    {{
");
            foreach (var pair in fields)
            {
                var fieldSymbol = pair.Key;
                var AttributeProperty = pair.Value;
                if (fieldSymbol.Name.Length == 0)
                    continue;
                source.AppendLine($@"        public {(AttributeProperty.IsOverrideProperty? "override" : "virtual")} {fieldSymbol.Type} {AttributeProperty.PropertyName} 
        {{
            get => ThrowIfDisposed({fieldSymbol.Name}); 
            set 
            {{
                ThrowIfDisposed();
                {fieldSymbol.Name} = value;                
                SetProperty(""{fieldSymbol.Name}"", value);
            }}
        }}");
            }

            string ReadFormalParameters = info.ReadPropertyList.Count > 0 ? ", " + string.Join(", ", info.ReadPropertyList) : "";
            string ReadArguments = info.ReadPropertyNameList.Count > 0 ? ", " + string.Join(", ", info.ReadPropertyNameList) : "";

            string ReadCotrBody = $@"        {{
            Id = GetPacketId(ProtocolVersion);
{info.ReadInit}            Read(ref reader);
        }}";

            string RefReadCotrBody = $@"        {{
            Id = GetPacketId(ProtocolVersion);
{info.ReadInit}            CompatibleByteReader reader = new CompatibleByteReader(packet.AsSpan(), ProtocolVersion);
            Read(ref reader);
        }}";
            string CompatibleRefReadCotrBody = $@"        {{
            Id = GetPacketId(ProtocolVersion);
{info.ReadInit}            CompatibleByteReader reader = packet.AsCompatibleByteReader();
            Read(ref reader);
        }}";

            cotr.AppendLine($"        public {classSymbol.Name}(ref {CompatiblePacket} packet{ReadFormalParameters}) : base(packet.Id, ref packet._start, ref packet._size, ref packet._data, packet.ProtocolVersion)").AppendLine(CompatibleRefReadCotrBody);
            cotr.AppendLine($"        public {classSymbol.Name}(ref {Packet} packet{ReadFormalParameters}, {ICompatible} compatible) : base(packet.Id, ref packet._start, ref packet._size, ref packet._data, compatible.ProtocolVersion)").AppendLine(RefReadCotrBody);
            cotr.AppendLine($"        public {classSymbol.Name}(ref {Packet} packet{ReadFormalParameters}, int protocolVersion) : base(packet.Id, ref packet._start, ref packet._size, ref packet._data, protocolVersion)").AppendLine(RefReadCotrBody);

            cotr.AppendLine($@"        public {classSymbol.Name}(CompatibleByteReader reader{ReadFormalParameters}) : base(ref reader)").AppendLine(ReadCotrBody);
            cotr.AppendLine($@"        public {classSymbol.Name}(ref CompatibleByteReader reader{ReadFormalParameters}) : base(ref reader)").AppendLine(ReadCotrBody);



            if (info.WritePropertyList.Count > 0)
            {
                cotr.AppendLine($"        public {classSymbol.Name}({string.Join(", ", info.WritePropertyList)}, {ICompatible} compatible): this({string.Join(", ",info.WritePropertyNameList)}, compatible.ProtocolVersion) {{}}");
       
                cotr.AppendLine($"        public {classSymbol.Name}({string.Join(", ", info.WritePropertyList)}, int protocolVersion): base(GetPacketId(protocolVersion), protocolVersion)");

            }
            else
            {
                cotr.AppendLine($"        public {classSymbol.Name}({ICompatible} compatible): this(compatible.ProtocolVersion) {{}}");
                cotr.AppendLine($"        public {classSymbol.Name}(int protocolVersion): base(GetPacketId(protocolVersion), protocolVersion)");
            }
                
            cotr.AppendLine("        {");
            cotr.Append(info.WriteInit);
            cotr.AppendLine("            CheckProperty();");
            cotr.AppendLine("            Write();");
            cotr.Append("        }");

            if(info.HasOptionalProperty&&info.OptionalPropertyList.Count>0)
            {

                cotr.AppendLine();
                cotr.AppendLine($"        public {classSymbol.Name}({string.Join(", ", info.OptionalPropertyList)}, {ICompatible} compatible) : this({string.Join(", ",info.OptionalPropertyNameList)}, compatible.ProtocolVersion) {{}}");
                cotr.AppendLine($"        public {classSymbol.Name}({string.Join(", ", info.OptionalPropertyList)}, int protocolVersion): base(GetPacketId(protocolVersion), protocolVersion)");
                cotr.AppendLine("        {");
                cotr.Append(info.OptionalInit);
                cotr.AppendLine("            CheckProperty();");
                cotr.AppendLine("            Write();");
                cotr.Append("        }");
            }

            tryRead.Append($@"

        public static bool TryRead<TPacket>(TPacket readPacket{ReadFormalParameters}) where TPacket: ICompatiblePacket => TryRead(ref readPacket{ReadArguments}, out _);
        public static bool TryRead<TPacket>(TPacket readPacket{ReadFormalParameters}, out {classSymbol.Name} packet) where TPacket: ICompatiblePacket => TryRead(ref readPacket{ReadArguments}, out packet);
        public static bool TryRead<TPacket>(TPacket readPacket{ReadFormalParameters}, {ICompatible} compatible) where TPacket: IPacket => TryRead(ref readPacket{ReadArguments}, compatible.ProtocolVersion, out _);
        public static bool TryRead<TPacket>(TPacket readPacket{ReadFormalParameters}, {ICompatible} compatible, out {classSymbol.Name} packet) where TPacket: IPacket => TryRead(ref readPacket{ReadArguments}, compatible.ProtocolVersion, out packet);
        public static bool TryRead<TPacket>(TPacket readPacket{ReadFormalParameters}, int protocolVersion) where TPacket: IPacket => TryRead(ref readPacket{ReadArguments}, protocolVersion, out _);
        public static bool TryRead<TPacket>(TPacket readPacket{ReadFormalParameters}, int protocolVersion, out {classSymbol.Name} packet) where TPacket: IPacket => TryRead(ref readPacket{ReadArguments}, protocolVersion, out packet);
 
        public static bool TryRead<TPacket>(ref TPacket readPacket{ReadFormalParameters}) where TPacket: ICompatiblePacket => TryRead(ref readPacket{ReadArguments}, out _);
        public static bool TryRead<TPacket>(ref TPacket readPacket{ReadFormalParameters}, out {classSymbol.Name} packet) where TPacket: ICompatiblePacket
        {{
            packet = null;
            if (readPacket == null || readPacket.Id != GetPacketId(readPacket.ProtocolVersion))
                return false;
            try
            {{
                  CompatibleByteReader reader = readPacket.AsCompatibleByteReader();
                  packet = new {classSymbol.Name}(ref reader{ReadArguments});
                  return reader.IsReadToEnd;
            }}
            catch (PacketNotFoundException) {{ return false; }}
            catch (InvalidPacketException) {{ return false; }}
            catch (ArgumentOutOfRangeException) {{ return false; }}
            catch (IndexOutOfRangeException) {{ return false; }}
            catch (InvalidCastException) {{ return false; }}
            catch (OverflowException) {{ return false; }}
        }}
        public static bool TryRead<TPacket>(ref TPacket readPacket{ReadFormalParameters}, {ICompatible} compatible) where TPacket: IPacket => TryRead(ref readPacket{ReadArguments}, compatible.ProtocolVersion, out _);
        public static bool TryRead<TPacket>(ref TPacket readPacket{ReadFormalParameters}, {ICompatible} compatible, out {classSymbol.Name} packet) where TPacket: IPacket => TryRead(ref readPacket{ReadArguments}, compatible.ProtocolVersion, out packet);
        public static bool TryRead<TPacket>(ref TPacket readPacket{ReadFormalParameters}, int protocolVersion) where TPacket: IPacket => TryRead(ref readPacket{ReadArguments}, protocolVersion, out _);
        public static bool TryRead<TPacket>(ref TPacket readPacket{ReadFormalParameters}, int protocolVersion, out {classSymbol.Name} packet) where TPacket: IPacket
        {{
            packet = null;
            if (readPacket == null || readPacket.Id != GetPacketId(protocolVersion))
                return false;
            try
            {{
                  ReadOnlySpan<byte> span = readPacket.AsByteReader().AsSpan();
                  CompatibleByteReader reader = new CompatibleByteReader(ref span, protocolVersion);
                  packet = new {classSymbol.Name}(ref reader{ReadArguments});
                  return reader.IsReadToEnd;
            }}
            catch (PacketNotFoundException) {{ return false; }}
            catch (InvalidPacketException) {{ return false; }}
            catch (ArgumentOutOfRangeException) {{ return false; }}
            catch (IndexOutOfRangeException) {{ return false; }}
            catch (InvalidCastException) {{ return false; }}
            catch (OverflowException) {{ return false; }}
        }}
");


            source.Append(cotr);
            source.Append(tryRead);
            source.AppendLine($"        public static int GetPacketId({ICompatible} compatible) => GetPacketId(compatible.ProtocolVersion);");
            source.AppendLine(@"    }
}");

            return source.ToString();
        }

        public class GenerateInfo
        {
            public List<string> WritePropertyList = new List<string>();
            public List<string> WritePropertyNameList = new List<string>();

            public List<string> OptionalPropertyList = new List<string>();
            public List<string> OptionalPropertyNameList = new List<string>();

            public List<string> ReadPropertyList = new List<string>();
            public List<string> ReadPropertyNameList = new List<string>();
            public StringBuilder WriteInit = new StringBuilder();
            public StringBuilder OptionalInit = new StringBuilder();
            public StringBuilder ReadInit = new StringBuilder();

            public bool HasOptionalProperty;

            public GenerateInfo(IEnumerable<KeyValuePair<IFieldSymbol, AttributeProperty>> fields)
            {
                foreach (var pair in fields)
                {
                    var fieldSymbol = pair.Key;
                    var AttributeProperty = pair.Value;
                    if (fieldSymbol.Name.Length == 0)
                        continue;

                    string CotrPropertyName = AttributeProperty.PropertyName.Substring(0, 1).ToLower() + AttributeProperty.PropertyName.Substring(1);
                    if (AttributeProperty.IsWriteProperty)
                    {
                        if (!AttributeProperty.IsOptional)
                        {
                            OptionalPropertyNameList.Add(CotrPropertyName);
                            OptionalPropertyList.Add($"{fieldSymbol.Type} {CotrPropertyName}");
                            OptionalInit.AppendLine($"            this.{fieldSymbol.Name} = {CotrPropertyName};");
                        }
                        else
                        {
                            HasOptionalProperty = true;
                            OptionalInit.AppendLine($"            this.{fieldSymbol.Name} = default;");
                        }
                        WritePropertyNameList.Add(CotrPropertyName);
                        WritePropertyList.Add($"{fieldSymbol.Type} {CotrPropertyName}");
                        WriteInit.AppendLine($"            this.{fieldSymbol.Name} = {CotrPropertyName};");
                        
                    }
                    if (AttributeProperty.IsReadProperty)
                    {
                        ReadPropertyNameList.Add(CotrPropertyName);
                        ReadPropertyList.Add($"{fieldSymbol.Type} {CotrPropertyName}");
                        ReadInit.AppendLine($"            this.{fieldSymbol.Name} = {CotrPropertyName};");
                    }

                }
            }
        }
        public class AttributeProperty
        {
            public string PropertyName { get; set; }
            public int ConstructorPriority { get; set; }
            public bool IsReadProperty { get; set; }
            public bool IsWriteProperty { get; set; }
            public bool IsOverrideProperty { get; set; }
            public bool IsOptional { get; set; }
            
            public AttributeProperty()
            {
                ConstructorPriority = -1;
                IsWriteProperty = true;
                IsReadProperty = false;
                IsOverrideProperty = false;
                IsOptional = false;
            }
        }
    }
}
