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
        private static readonly string CompatiblePacket = "CompatiblePacket";
        private static readonly string ReadOnlyPacket = "ReadOnlyPacket";

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

        public PacketPropertyAttribute() { }
        public PacketPropertyAttribute(string name) { }
        public PacketPropertyAttribute(string name, int priority) { }
        public PacketPropertyAttribute(string propertyName, int constructorPriority, bool isReadProperty) { }
        public PacketPropertyAttribute(string propertyName, int constructorPriority, bool isReadProperty, bool isWriteProperty) { }
        public PacketPropertyAttribute(string propertyName, int constructorPriority, bool isReadProperty, bool isWriteProperty, bool isOverrideProperty) { }
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
            StringBuilder source = new StringBuilder($@"
using System;

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
                SetProperty(""{fieldSymbol.Name}"",value);
            }}
        }}");
            }

            string ReadFormalParameters = info.ReadPropertyList.Count > 0 ? string.Join(", ", info.ReadPropertyList) : "";
            string ReadArguments = info.ReadPropertyNameList.Count > 0 ? string.Join(", ", info.ReadPropertyNameList) : "";
            if (info.ReadPropertyList.Count > 0)
            {
                cotr.AppendLine($@"        public {classSymbol.Name}({ReadOnlyCompatiblePacket} packet, {ReadFormalParameters}) : this(packet, {ReadArguments}, packet.ProtocolVersion) {{ }}");
                cotr.AppendLine($@"        public {classSymbol.Name}({ReadOnlyPacket} packet, {ReadFormalParameters}, int protocolVersion) : base(packet, protocolVersion)")
                    .AppendLine("        {")
                    .AppendLine($@"            ID = GetPacketId(protocolVersion);")
                    .Append(info.ReadInit)
                    .AppendLine("            Read();")
                    .AppendLine("        }");

                cotr.AppendLine($@"        public {classSymbol.Name}(ref {CompatiblePacket} packet, {ReadFormalParameters}) : base(packet.ID, ref packet._data, packet.ProtocolVersion)")
                    .AppendLine("        {")
                    .AppendLine($@"            ID = GetPacketId(packet.ProtocolVersion);")
                    .Append(info.ReadInit)
                    .AppendLine("            Read();")
                    .AppendLine("        }"); ;

            }
            else
            {
                cotr.AppendLine($"        public {classSymbol.Name}({ReadOnlyCompatiblePacket} packet) : this(packet, packet.ProtocolVersion) {{ }}");
                cotr.AppendLine($"        public {classSymbol.Name}({ReadOnlyPacket} packet, int protocolVersion) : base(packet, protocolVersion) {{ ID = GetPacketId(protocolVersion); Read(); }}");
                cotr.AppendLine($"        public {classSymbol.Name}(ref {CompatiblePacket} packet) : base(packet.ID, ref packet._data, packet.ProtocolVersion) {{ ID = GetPacketId(packet.ProtocolVersion); Read(); }}");

            }



            if (info.WritePropertyList.Count > 0)
                cotr.AppendLine($"        public {classSymbol.Name}({string.Join(", ", info.WritePropertyList)}, int protocolVersion): base(GetPacketId(protocolVersion), protocolVersion)");
            else
                cotr.AppendLine($"        public {classSymbol.Name}(int protocolVersion): base(GetPacketId(protocolVersion), protocolVersion)");
            cotr.AppendLine("        {");
            cotr.Append(info.WriteInit);
            cotr.AppendLine("            CheckProperty();")
                .AppendLine("            Write();")
                .Append("        }");

            if(info.ReadPropertyList.Count>0&& info.ReadPropertyNameList.Count == info.ReadPropertyList.Count)
            {
                tryRead.Append($@"
        public static bool TryRead({ReadOnlyCompatiblePacket} readPacket, {ReadFormalParameters}) => TryRead(readPacket, {ReadArguments}, readPacket.ProtocolVersion, out _);
        public static bool TryRead({ReadOnlyCompatiblePacket} readPacket, {ReadFormalParameters}, out {classSymbol.Name} packet) => TryRead(readPacket, {ReadArguments}, readPacket.ProtocolVersion, out packet);
        public static bool TryRead({ReadOnlyPacket} readPacket, {ReadFormalParameters}, int protocolVersion) => TryRead(readPacket, {ReadArguments}, protocolVersion, out _);
        public static bool TryRead({ReadOnlyPacket} readPacket, {ReadFormalParameters}, int protocolVersion, out {classSymbol.Name} packet)");
            }
            else
            {
                tryRead.Append($@"
        public static bool TryRead({ReadOnlyCompatiblePacket} readPacket) => TryRead(readPacket, readPacket.ProtocolVersion, out _);
        public static bool TryRead({ReadOnlyCompatiblePacket} readPacket, out {classSymbol.Name} packet) => TryRead(readPacket, readPacket.ProtocolVersion, out packet);
        public static bool TryRead({ReadOnlyPacket} readPacket, int protocolVersion) => TryRead(readPacket, protocolVersion, out _);
        public static bool TryRead({ReadOnlyPacket} readPacket, int protocolVersion, out {classSymbol.Name} packet)");
            }
            tryRead.Append($@"
        {{
            packet = null;
            if (readPacket is null || readPacket.ID != GetPacketId(protocolVersion))
                return false;
            try
            {{
                  packet = new {classSymbol.Name}(readPacket");
            if (info.ReadPropertyList.Count > 0 && info.ReadPropertyNameList.Count == info.ReadPropertyList.Count)
                tryRead.Append(", ").Append(ReadArguments);
            tryRead.AppendLine(", protocolVersion);");
            tryRead.Append(
$@"                  return packet.Reader.IsReadToEnd;
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
            source.Append("} }");
            return source.ToString();
        }

        public class GenerateInfo
        {
            public List<string> WritePropertyList = new List<string>();
            public List<string> WritePropertyNameList = new List<string>();
            public List<string> ReadPropertyList = new List<string>();
            public List<string> ReadPropertyNameList = new List<string>();
            public StringBuilder WriteInit = new StringBuilder();
            public StringBuilder ReadInit = new StringBuilder();

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
                        WritePropertyList.Add($"{fieldSymbol.Type} {CotrPropertyName}");
                        WriteInit.AppendLine($"            {fieldSymbol.Name} = {CotrPropertyName};");
                        WritePropertyNameList.Add(CotrPropertyName);
                    }
                    if (AttributeProperty.IsReadProperty)
                    {
                        ReadPropertyList.Add($"{fieldSymbol.Type} {CotrPropertyName}");
                        ReadInit.AppendLine($"            {fieldSymbol.Name} = {CotrPropertyName};");
                        ReadPropertyNameList.Add(CotrPropertyName);
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

            public AttributeProperty()
            {
                ConstructorPriority = -1;
                IsWriteProperty = true;
                IsReadProperty = false;
                IsOverrideProperty = false;
            }
        }
    }
}
