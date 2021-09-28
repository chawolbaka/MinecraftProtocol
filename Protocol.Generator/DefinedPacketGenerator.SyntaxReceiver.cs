using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Protocol.Generator
{
    public partial class DefinedPacketGenerator
    {
        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<KeyValuePair<IFieldSymbol, AttributeProperty>> Fields = new List<KeyValuePair<IFieldSymbol, AttributeProperty>>();
            
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                        AttributeData attributeData = fieldSymbol.GetAttributes().SingleOrDefault(ad => ad.AttributeClass.ToDisplayString() == $"{PacketPropertyAttributeNamespace}.{PacketPropertyAttribute}");
                        if (attributeData != null)
                        {
                            AttributeProperty ap = new AttributeProperty();
                            if (attributeData.ConstructorArguments.Length >= 1)
                                ap.PropertyName = attributeData.ConstructorArguments[0].Value.ToString();
                            if (attributeData.ConstructorArguments.Length >= 2)
                                ap.ConstructorPriority = Convert.ToInt32(attributeData.ConstructorArguments[1].Value);
                            if (attributeData.ConstructorArguments.Length >= 3)
                                ap.IsReadProperty = Convert.ToBoolean(attributeData.ConstructorArguments[2].Value);
                            if (attributeData.ConstructorArguments.Length >= 4)
                                ap.IsWriteProperty = Convert.ToBoolean(attributeData.ConstructorArguments[3].Value);
                            if (attributeData.ConstructorArguments.Length >= 5)
                                ap.IsOverrideProperty = Convert.ToBoolean(attributeData.ConstructorArguments[4].Value);

                            //PacketProperty(xxx = true) 这种方式拥有更高的优先级，如果有就覆盖上面的(但好像也不会出现覆盖的情况)
                            var pn = attributeData.NamedArguments.SingleOrDefault(x => x.Key == nameof(AttributeProperty.PropertyName)).Value;
                            var cp = attributeData.NamedArguments.SingleOrDefault(x => x.Key == nameof(AttributeProperty.ConstructorPriority)).Value;
                            var rp = attributeData.NamedArguments.SingleOrDefault(x => x.Key == nameof(AttributeProperty.IsReadProperty)).Value;
                            var wp = attributeData.NamedArguments.SingleOrDefault(x => x.Key == nameof(AttributeProperty.IsWriteProperty)).Value;
                            var op = attributeData.NamedArguments.SingleOrDefault(x => x.Key == nameof(AttributeProperty.IsOverrideProperty)).Value;

                            if (!pn.IsNull)
                                ap.PropertyName = pn.Value.ToString();
                            if (!cp.IsNull)
                                ap.ConstructorPriority = Convert.ToInt32(pn.Value);
                            if (!rp.IsNull)
                                ap.IsReadProperty = Convert.ToBoolean(rp.Value);
                            if (!wp.IsNull)
                                ap.IsWriteProperty = Convert.ToBoolean(wp.Value);
                            if (!op.IsNull)
                                ap.IsOverrideProperty = Convert.ToBoolean(op.Value);

                            if (string.IsNullOrWhiteSpace(ap.PropertyName))
                            {
                                ap.PropertyName = fieldSymbol.Name.TrimStart('_');
                                ap.PropertyName = ap.PropertyName.Length == 1 ? ap.PropertyName.ToUpper() : ap.PropertyName.Substring(0, 1).ToUpper() + ap.PropertyName.Substring(1);
                            }

                            Fields.Add(new KeyValuePair<IFieldSymbol, AttributeProperty>(fieldSymbol, ap));
                        }
                    }
                }
            }
        }
    }
}
