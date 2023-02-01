using System;

namespace MinecraftProtocol.DataType
{
    /// <summary>
    /// https://www.minecraft.net/en-us/article/minecraft-snapshot-17w43a
    /// </summary>
    public class Identifier : IEquatable<Identifier>
    {
        public string Namespace { get; }
        public string Name { get; }

        public Identifier(string name)
        {
            Namespace = "minecraft";
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public Identifier(string @namespace, string name)
        {
            //允许字符的正则我懒的写了
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static Identifier Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException(nameof(input));
            string[] identifier = input.Split(':');
            if (identifier.Length > 2)
                throw new FormatException("too much :");
            else if (identifier.Length == 1)
                return new Identifier(identifier[0]);
            else
                return new Identifier(identifier[0], identifier[1]);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? Namespace : Namespace +":"+ Name;
        }

        public bool Equals(Identifier other)
        {
            return other != null && other.Namespace.Equals(Namespace) && other.Name.Equals(Name);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Identifier);
        }
    }
}
