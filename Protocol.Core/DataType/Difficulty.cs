using System;

namespace MinecraftProtocol.DataType
{
    public struct Difficulty : IEquatable<Difficulty>
    {
        public static Difficulty Unknown = new Difficulty(null);
        
        /// <summary>和平</summary>
        public static Difficulty Peaceful = new Difficulty(0);

        /// <summary>简单</summary>
        public static Difficulty Easy = new Difficulty(1);
        
        /// <summary>普通</summary>
        public static Difficulty Normal = new Difficulty(2);
        
        /// <summary>困难</summary>
        public static Difficulty Hard = new Difficulty(3);

        private readonly byte? _difficulty;

        private Difficulty(byte? difficulty)
        {
            _difficulty = difficulty;
        }
        public Difficulty(byte difficulty)
        {
            _difficulty = difficulty;
        }

        public override string ToString()
        {
            return !_difficulty.HasValue ? "Unknown" : _difficulty switch
            {
                0 => "Peaceful",
                1 => "Easy",
                2 => "Normal",
                3 => "Hard",
                _ => "Unknown" //有可能改成_difficulty.ToString
            };
        }

        public override bool Equals(object obj)
        {
            return obj is Difficulty difficulty && Equals(difficulty);
        }

        public bool Equals(Difficulty other)
        {
            return _difficulty.HasValue == other._difficulty.HasValue && _difficulty.Value == other._difficulty.Value;
        }

        public override int GetHashCode()
        {
            return _difficulty ?? -233;
        }

        public static implicit operator Difficulty(byte value) => new Difficulty(value);
        public static explicit operator byte(Difficulty value) => value._difficulty.HasValue ? value._difficulty.Value : throw new InvalidCastException("Unknown difficulty");
        
        public static bool operator ==(Difficulty left, Difficulty right)=>left.Equals(right);
        
        public static bool operator !=(Difficulty left, Difficulty right)=> !(left == right);
        
    }
}
