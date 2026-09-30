namespace RogueChess.Engine
{
    public enum Side
    {
        White,
        Black
    }

    public static class SideExtensions
    {
        public static Side Opponent(this Side side) => side == Side.White ? Side.Black : Side.White;

        /// <summary>Rank direction that counts as "forward" for this side.</summary>
        public static int Forward(this Side side) => side == Side.White ? 1 : -1;
    }
}
