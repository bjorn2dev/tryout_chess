using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine
{
    public enum BattleResult
    {
        Ongoing,
        WhiteWins,
        BlackWins,
        Draw
    }

    public enum EndReason
    {
        None,
        KingDefeated,
        Fatigue
    }

    public sealed class Piece
    {
        public int Id { get; }
        public PieceDefinition Definition { get; }
        public Side Side { get; }
        public Coord Position { get; set; }
        public int Hp { get; set; }

        public Piece(int id, PieceDefinition definition, Side side, Coord position, int hp)
        {
            Id = id;
            Definition = definition;
            Side = side;
            Position = position;
            Hp = hp;
        }

        public Piece Clone() => new Piece(Id, Definition, Side, Position, Hp);
    }

    /// <summary>Plain, copyable data describing a battle position. Only pieces still on the board are listed.</summary>
    public sealed class BattleState
    {
        public int Width { get; }
        public int Height { get; }
        public List<Piece> Pieces { get; } = new List<Piece>();
        public Side SideToAct { get; set; }
        public int Round { get; set; } = 1;
        public BattleResult Result { get; set; } = BattleResult.Ongoing;
        public EndReason EndReason { get; set; } = EndReason.None;

        public BattleState(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Contains(Coord square) =>
            square.File >= 0 && square.File < Width && square.Rank >= 0 && square.Rank < Height;

        public Piece PieceAt(Coord square)
        {
            foreach (var piece in Pieces)
                if (piece.Position == square) return piece;
            return null;
        }

        public Piece King(Side side) => Pieces.FirstOrDefault(p => p.Side == side && p.Definition.IsKing);

        public BattleState Clone()
        {
            var copy = new BattleState(Width, Height)
            {
                SideToAct = SideToAct,
                Round = Round,
                Result = Result,
                EndReason = EndReason
            };
            copy.Pieces.AddRange(Pieces.Select(p => p.Clone()));
            return copy;
        }
    }
}
