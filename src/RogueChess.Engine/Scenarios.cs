namespace RogueChess.Engine
{
    public static class Scenarios
    {
        /// <summary>
        /// 6x6 starter battle: back rank ". R N K B .", pawns on files b-e, mirrored for Black.
        /// </summary>
        public static BattleConfig Starter(BattleRules rules = null, PieceDefinition king = null)
        {
            king = king ?? StandardPieces.King;
            var config = new BattleConfig { Width = 6, Height = 6, Rules = rules ?? new BattleRules() };

            foreach (var (side, back, pawns) in new[] { (Side.White, "1", "2"), (Side.Black, "6", "5") })
            {
                config.Add(StandardPieces.Rook, side, "b" + back);
                config.Add(StandardPieces.Knight, side, "c" + back);
                config.Add(king, side, "d" + back);
                config.Add(StandardPieces.Bishop, side, "e" + back);
                foreach (var file in new[] { "b", "c", "d", "e" })
                    config.Add(StandardPieces.Pawn, side, file + pawns);
            }
            return config;
        }
    }
}
