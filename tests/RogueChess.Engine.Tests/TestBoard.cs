using RogueChess.Engine;

namespace RogueChess.Engine.Tests;

internal static class TestBoard
{
    public static readonly PieceDefinition P = StandardPieces.Pawn;
    public static readonly PieceDefinition N = StandardPieces.Knight;
    public static readonly PieceDefinition B = StandardPieces.Bishop;
    public static readonly PieceDefinition R = StandardPieces.Rook;
    public static readonly PieceDefinition Q = StandardPieces.Queen;
    public static readonly PieceDefinition K = StandardPieces.King;

    public static BattleConfig Config(BattleRules rules, params (PieceDefinition def, Side side, string at)[] pieces)
    {
        var config = new BattleConfig { Width = 6, Height = 6, Rules = rules ?? new BattleRules() };
        foreach (var (def, side, at) in pieces)
            config.Add(def, side, at);
        return config;
    }

    public static Battle Make(params (PieceDefinition def, Side side, string at)[] pieces) =>
        new Battle(Config(null, pieces));

    public static Battle Make(BattleRules rules, params (PieceDefinition def, Side side, string at)[] pieces) =>
        new Battle(Config(rules, pieces));

    public static Piece At(this Battle battle, string square) => battle.State.PieceAt(Coord.Parse(square));

    public static ActionResult Move(this Battle battle, string from, string to) =>
        battle.Apply(BattleAction.Move(Coord.Parse(from), Coord.Parse(to)));

    public static ActionResult Attack(this Battle battle, string from, string target) =>
        battle.Apply(BattleAction.Attack(Coord.Parse(from), Coord.Parse(target)));

    public static LegalAction FindAttack(this Battle battle, string from, string target) =>
        battle.GetLegalActions().FirstOrDefault(a =>
            a.Action.Equals(BattleAction.Attack(Coord.Parse(from), Coord.Parse(target))));

    public static bool CanMove(this Battle battle, string from, string to) =>
        battle.GetLegalActions().Any(a => a.Action.Equals(BattleAction.Move(Coord.Parse(from), Coord.Parse(to))));

    /// <summary>Pieces, side to act, round and result as one comparable string.</summary>
    public static string Snapshot(this BattleState state) =>
        Position(state) + $"|{state.SideToAct}|{state.Round}|{state.Result}|{state.EndReason}";

    public static string Position(BattleState state) =>
        string.Join(";", state.Pieces.OrderBy(p => p.Id).Select(p => $"{p.Id}@{p.Position}:{p.Hp}"));
}
