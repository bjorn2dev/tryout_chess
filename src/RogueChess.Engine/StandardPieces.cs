using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine
{
    /// <summary>The six classic pieces with the default stat table.</summary>
    public static class StandardPieces
    {
        private static readonly (int dx, int dy)[] Orthogonal = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        private static readonly (int dx, int dy)[] Diagonal = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        private static readonly (int dx, int dy)[] KnightJumps =
        {
            (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)
        };

        private static List<PatternStep> Steps(IEnumerable<(int dx, int dy)> directions, int range, bool jumps = false) =>
            directions.Select(d => new PatternStep(d.dx, d.dy, range, jumps)).ToList();

        private static PieceDefinition Symmetric(string name, char symbol, int hp, int atk, bool isKing, List<PatternStep> pattern) =>
            new PieceDefinition(name, symbol, hp, atk, isKing, pattern, pattern);

        public static readonly PieceDefinition Pawn = new PieceDefinition("Pawn", 'P', 1, 1, false,
            Steps(new[] { (0, 1) }, 1),
            Steps(new[] { (1, 1), (-1, 1) }, 1));

        public static readonly PieceDefinition Knight =
            Symmetric("Knight", 'N', 2, 1, false, Steps(KnightJumps, 1, jumps: true));

        public static readonly PieceDefinition Bishop =
            Symmetric("Bishop", 'B', 2, 1, false, Steps(Diagonal, PatternStep.Unlimited));

        public static readonly PieceDefinition Rook =
            Symmetric("Rook", 'R', 3, 2, false, Steps(Orthogonal, PatternStep.Unlimited));

        public static readonly PieceDefinition Queen =
            Symmetric("Queen", 'Q', 3, 2, false, Steps(Orthogonal.Concat(Diagonal), PatternStep.Unlimited));

        public static readonly PieceDefinition King =
            Symmetric("King", 'K', 6, 1, true, Steps(Orthogonal.Concat(Diagonal), 1));
    }
}
