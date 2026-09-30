using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine
{
    public sealed class BattleRules
    {
        public int SupportBonusPerPiece { get; set; } = 1;
        public int SupportBonusCap { get; set; } = 2;
        public int FatigueStartRound { get; set; } = 25;
        public int FatigueDamage { get; set; } = 1;
    }

    public sealed class PiecePlacement
    {
        public PieceDefinition Definition { get; }
        public Side Side { get; }
        public Coord Position { get; }

        public PiecePlacement(PieceDefinition definition, Side side, Coord position)
        {
            Definition = definition;
            Side = side;
            Position = position;
        }
    }

    public sealed class BattleConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Side FirstToAct { get; set; } = Side.White;
        public BattleRules Rules { get; set; } = new BattleRules();
        public List<PiecePlacement> Placements { get; } = new List<PiecePlacement>();

        public BattleConfig Add(PieceDefinition definition, Side side, string square)
        {
            Placements.Add(new PiecePlacement(definition, side, Coord.Parse(square)));
            return this;
        }

        /// <summary>Throws <see cref="ArgumentException"/> when the configuration cannot start a battle.</summary>
        public void Validate()
        {
            if (Width < 1 || Height < 1)
                throw new ArgumentException("Board width and height must be at least 1.");
            if (Rules == null)
                throw new ArgumentException("Rules are required.");
            if (Rules.SupportBonusPerPiece < 0 || Rules.SupportBonusCap < 0)
                throw new ArgumentException("Support bonus and cap cannot be negative.");
            if (Rules.FatigueStartRound < 2 || Rules.FatigueDamage < 1)
                throw new ArgumentException("Fatigue must start in round 2 or later and deal at least 1 damage.");

            var occupied = new HashSet<Coord>();
            foreach (var placement in Placements)
            {
                var p = placement.Position;
                if (p.File < 0 || p.File >= Width || p.Rank < 0 || p.Rank >= Height)
                    throw new ArgumentException($"{placement.Definition.Name} on {p} is outside the board.");
                if (!occupied.Add(p))
                    throw new ArgumentException($"More than one piece on {p}.");
                if (placement.Definition.Hp < 1)
                    throw new ArgumentException($"{placement.Definition.Name} must start with at least 1 HP.");
            }

            foreach (Side side in new[] { Side.White, Side.Black })
            {
                int kings = Placements.Count(x => x.Side == side && x.Definition.IsKing);
                if (kings != 1)
                    throw new ArgumentException($"{side} must have exactly one king but has {kings}.");
            }
        }
    }
}
