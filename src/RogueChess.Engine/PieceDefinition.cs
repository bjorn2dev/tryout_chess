using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine
{
    /// <summary>
    /// One direction a piece can travel in. Dy is relative to the owner's forward direction.
    /// </summary>
    public sealed class PatternStep
    {
        public const int Unlimited = int.MaxValue;

        public int Dx { get; }
        public int Dy { get; }
        public int MaxRange { get; }
        /// <summary>A jumping step ignores pieces in between and never advances after a non-lethal attack.</summary>
        public bool Jumps { get; }

        public PatternStep(int dx, int dy, int maxRange, bool jumps = false)
        {
            Dx = dx;
            Dy = dy;
            MaxRange = maxRange;
            Jumps = jumps;
        }
    }

    public sealed class PieceDefinition
    {
        public string Name { get; }
        public char Symbol { get; }
        public int Hp { get; }
        public int Atk { get; }
        public bool IsKing { get; }
        public IReadOnlyList<PatternStep> MovePattern { get; }
        public IReadOnlyList<PatternStep> CapturePattern { get; }

        public PieceDefinition(string name, char symbol, int hp, int atk, bool isKing,
            IEnumerable<PatternStep> movePattern, IEnumerable<PatternStep> capturePattern)
        {
            Name = name;
            Symbol = symbol;
            Hp = hp;
            Atk = atk;
            IsKing = isKing;
            MovePattern = movePattern.ToList();
            CapturePattern = capturePattern.ToList();
        }

        public PieceDefinition WithStats(int hp, int atk) =>
            new PieceDefinition(Name, Symbol, hp, atk, IsKing, MovePattern, CapturePattern);
    }
}
