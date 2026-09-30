using NUnit.Framework;
using RogueChess.Engine;
using static RogueChess.Engine.Tests.TestBoard;

namespace RogueChess.Engine.Tests;

public class SetupAndPatternTests
{
    private static List<string> MovesOnEmptyBoard(PieceDefinition definition, Side side = Side.White)
    {
        var state = new BattleState(6, 6);
        var piece = new Piece(0, definition, side, Coord.Parse("c3"), definition.Hp);
        state.Pieces.Add(piece);
        return Patterns.MoveSquares(state, piece).Select(c => c.ToString()).OrderBy(s => s).ToList();
    }

    [Test]
    public void Rook_reaches_its_file_and_rank()
    {
        Assert.That(MovesOnEmptyBoard(R), Is.EquivalentTo(new[]
            { "a3", "b3", "d3", "e3", "f3", "c1", "c2", "c4", "c5", "c6" }));
    }

    [Test]
    public void Bishop_reaches_its_diagonals()
    {
        Assert.That(MovesOnEmptyBoard(B), Is.EquivalentTo(new[]
            { "b2", "a1", "d4", "e5", "f6", "b4", "a5", "d2", "e1" }));
    }

    [Test]
    public void Queen_combines_rook_and_bishop()
    {
        Assert.That(MovesOnEmptyBoard(Q), Is.EquivalentTo(MovesOnEmptyBoard(R).Concat(MovesOnEmptyBoard(B))));
    }

    [Test]
    public void Knight_jumps_in_l_shapes()
    {
        Assert.That(MovesOnEmptyBoard(N), Is.EquivalentTo(new[]
            { "b1", "d1", "a2", "e2", "a4", "e4", "b5", "d5" }));
    }

    [Test]
    public void King_steps_one_square()
    {
        Assert.That(MovesOnEmptyBoard(K), Is.EquivalentTo(new[]
            { "b2", "c2", "d2", "b3", "d3", "b4", "c4", "d4" }));
    }

    [Test]
    public void Pawn_moves_forward_and_attacks_diagonally_forward()
    {
        Assert.That(MovesOnEmptyBoard(P, Side.White), Is.EqualTo(new[] { "c4" }));
        Assert.That(MovesOnEmptyBoard(P, Side.Black), Is.EqualTo(new[] { "c2" }));

        var state = new BattleState(6, 6);
        var pawn = new Piece(0, P, Side.White, Coord.Parse("c3"), 1);
        state.Pieces.Add(pawn);
        Assert.That(Patterns.Attacks(state, pawn, Coord.Parse("b4")), Is.True);
        Assert.That(Patterns.Attacks(state, pawn, Coord.Parse("d4")), Is.True);
        Assert.That(Patterns.Attacks(state, pawn, Coord.Parse("c4")), Is.False);
        Assert.That(Patterns.Attacks(state, pawn, Coord.Parse("b2")), Is.False);
    }

    [Test]
    public void Valid_configuration_starts_in_round_one()
    {
        var battle = new Battle(Scenarios.Starter());

        Assert.That(battle.State.Round, Is.EqualTo(1));
        Assert.That(battle.State.SideToAct, Is.EqualTo(Side.White));
        Assert.That(battle.State.Result, Is.EqualTo(BattleResult.Ongoing));
        Assert.That(battle.State.Pieces, Has.Count.EqualTo(16));
        Assert.That(battle.State.Pieces.All(p => p.Hp == p.Definition.Hp), Is.True);
    }

    [Test]
    public void First_side_comes_from_the_configuration()
    {
        var config = Config(null, (K, Side.White, "a1"), (K, Side.Black, "f6"));
        config.FirstToAct = Side.Black;

        Assert.That(new Battle(config).State.SideToAct, Is.EqualTo(Side.Black));
    }

    [Test]
    public void Missing_king_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Make((K, Side.White, "a1"), (R, Side.Black, "f6")));
    }

    [Test]
    public void Second_king_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Make((K, Side.White, "a1"), (K, Side.White, "b1"), (K, Side.Black, "f6")));
    }

    [Test]
    public void Piece_outside_the_board_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Make((K, Side.White, "a1"), (K, Side.Black, "f6"), (R, Side.White, "g1")));
    }

    [Test]
    public void Two_pieces_on_one_square_are_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Make((K, Side.White, "a1"), (K, Side.Black, "f6"), (R, Side.White, "a1")));
    }
}
