using System.Collections.Generic;

namespace RogueChess.Engine
{
    /// <summary>Identifies a piece in an event, also after it has left the board.</summary>
    public readonly struct PieceRef
    {
        public int Id { get; }
        public Side Side { get; }
        public string Name { get; }

        public PieceRef(Piece piece)
        {
            Id = piece.Id;
            Side = piece.Side;
            Name = piece.Definition.Name;
        }

        public override string ToString() => $"{Side} {Name}";
    }

    public abstract class BattleEvent
    {
    }

    public sealed class PieceMoved : BattleEvent
    {
        public PieceRef Piece { get; }
        public Coord From { get; }
        public Coord To { get; }

        public PieceMoved(PieceRef piece, Coord from, Coord to)
        {
            Piece = piece;
            From = from;
            To = to;
        }

        public override string ToString() => $"{Piece} moves {From} -> {To}";
    }

    public sealed class PieceAttacked : BattleEvent
    {
        public PieceRef Attacker { get; }
        public PieceRef Target { get; }
        public Coord From { get; }
        public Coord TargetSquare { get; }
        public int Damage { get; }
        public IReadOnlyList<Coord> Supporters { get; }

        public PieceAttacked(PieceRef attacker, PieceRef target, Coord from, Coord targetSquare, int damage, IReadOnlyList<Coord> supporters)
        {
            Attacker = attacker;
            Target = target;
            From = from;
            TargetSquare = targetSquare;
            Damage = damage;
            Supporters = supporters;
        }

        public override string ToString() =>
            $"{Attacker} on {From} attacks {Target} on {TargetSquare} for {Damage} ({Supporters.Count} supporting)";
    }

    public sealed class PieceDamaged : BattleEvent
    {
        public PieceRef Piece { get; }
        public int Amount { get; }
        public int RemainingHp { get; }

        public PieceDamaged(PieceRef piece, int amount, int remainingHp)
        {
            Piece = piece;
            Amount = amount;
            RemainingHp = remainingHp;
        }

        public override string ToString() => $"{Piece} takes {Amount} damage, {RemainingHp} HP left";
    }

    public sealed class PieceDefeated : BattleEvent
    {
        public PieceRef Piece { get; }
        public Coord Square { get; }

        public PieceDefeated(PieceRef piece, Coord square)
        {
            Piece = piece;
            Square = square;
        }

        public override string ToString() => $"{Piece} on {Square} is defeated";
    }

    public sealed class FatigueApplied : BattleEvent
    {
        public int Round { get; }
        public int Damage { get; }

        public FatigueApplied(int round, int damage)
        {
            Round = round;
            Damage = damage;
        }

        public override string ToString() => $"Round {Round}: fatigue deals {Damage} to both kings";
    }

    public sealed class TurnPassed : BattleEvent
    {
        public Side Side { get; }

        public TurnPassed(Side side)
        {
            Side = side;
        }

        public override string ToString() => $"{Side} has no legal action and passes";
    }

    public sealed class BattleEnded : BattleEvent
    {
        public BattleResult Result { get; }
        public EndReason Reason { get; }

        public BattleEnded(BattleResult result, EndReason reason)
        {
            Result = result;
            Reason = reason;
        }

        public override string ToString() => $"Battle ended: {Result} ({Reason})";
    }
}
