using System.Collections.Generic;

namespace RogueChess.Engine
{
    /// <summary>An enemy piece within reach, and where the attacker ends if that piece survives.</summary>
    public readonly struct AttackReach
    {
        public Piece Target { get; }
        public Coord EndIfTargetSurvives { get; }

        public AttackReach(Piece target, Coord endIfTargetSurvives)
        {
            Target = target;
            EndIfTargetSurvives = endIfTargetSurvives;
        }
    }

    /// <summary>Turns piece patterns into squares on a concrete position.</summary>
    public static class Patterns
    {
        private static Coord Offset(Piece piece, PatternStep step, int n) =>
            new Coord(piece.Position.File + step.Dx * n, piece.Position.Rank + step.Dy * piece.Side.Forward() * n);

        /// <summary>Empty squares the piece can move to.</summary>
        public static IEnumerable<Coord> MoveSquares(BattleState state, Piece piece)
        {
            foreach (var step in piece.Definition.MovePattern)
            {
                for (int n = 1; n <= step.MaxRange; n++)
                {
                    var square = Offset(piece, step, n);
                    if (!state.Contains(square)) break;
                    if (state.PieceAt(square) == null) yield return square;
                    else if (!step.Jumps) break;
                }
            }
        }

        /// <summary>Enemy pieces the piece can attack.</summary>
        public static IEnumerable<AttackReach> AttackTargets(BattleState state, Piece piece)
        {
            foreach (var step in piece.Definition.CapturePattern)
            {
                for (int n = 1; n <= step.MaxRange; n++)
                {
                    var square = Offset(piece, step, n);
                    if (!state.Contains(square)) break;
                    var occupant = state.PieceAt(square);
                    if (occupant == null) continue;
                    if (occupant.Side != piece.Side)
                    {
                        var end = step.Jumps ? piece.Position : Offset(piece, step, n - 1);
                        yield return new AttackReach(occupant, end);
                    }
                    if (!step.Jumps) break;
                }
            }
        }

        /// <summary>Whether the piece's capture pattern reaches the square with a clear path.</summary>
        public static bool Attacks(BattleState state, Piece piece, Coord target)
        {
            foreach (var step in piece.Definition.CapturePattern)
            {
                for (int n = 1; n <= step.MaxRange; n++)
                {
                    var square = Offset(piece, step, n);
                    if (!state.Contains(square)) break;
                    if (square == target) return true;
                    if (state.PieceAt(square) != null && !step.Jumps) break;
                }
            }
            return false;
        }
    }
}
