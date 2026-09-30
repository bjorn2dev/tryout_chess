using System;

namespace RogueChess.Engine
{
    /// <summary>A board square. File and rank are zero-based; "a1" is (0, 0).</summary>
    public readonly struct Coord : IEquatable<Coord>
    {
        public int File { get; }
        public int Rank { get; }

        public Coord(int file, int rank)
        {
            File = file;
            Rank = rank;
        }

        public static Coord Parse(string text)
        {
            if (!TryParse(text, out var coord))
                throw new FormatException($"'{text}' is not a square like 'a1'.");
            return coord;
        }

        public static bool TryParse(string text, out Coord coord)
        {
            coord = default;
            if (string.IsNullOrEmpty(text) || text.Length < 2) return false;
            char file = char.ToLowerInvariant(text[0]);
            if (file < 'a' || file > 'z') return false;
            if (!int.TryParse(text.Substring(1), out int rank) || rank < 1) return false;
            coord = new Coord(file - 'a', rank - 1);
            return true;
        }

        public bool Equals(Coord other) => File == other.File && Rank == other.Rank;
        public override bool Equals(object obj) => obj is Coord other && Equals(other);
        public override int GetHashCode() => File * 397 ^ Rank;
        public static bool operator ==(Coord a, Coord b) => a.Equals(b);
        public static bool operator !=(Coord a, Coord b) => !a.Equals(b);
        public override string ToString() => $"{(char)('a' + File)}{Rank + 1}";
    }
}
