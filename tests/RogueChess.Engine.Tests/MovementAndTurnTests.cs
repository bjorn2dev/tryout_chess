using NUnit.Framework;
using RogueChess.Engine;
using static RogueChess.Engine.Tests.TestBoard;

namespace RogueChess.Engine.Tests;

public class MovementAndTurnTests
{
    [Test]
    public void Slider_is_blocked_by_a_piece_on_its_file()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (R, Side.White, "a1"), (P, Side.White, "a3"));

        Assert.That(battle.CanMove("a1", "a2"), Is.True);
        Assert.That(battle.CanMove("a1", "a3"), Is.False);
        Assert.That(battle.CanMove("a1", "a4"), Is.False);
    }

    [Test]
    public void Knight_jumps_over_pieces()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (N, Side.White, "b1"),
            (P, Side.White, "a2"), (P, Side.White, "b2"), (P, Side.White, "c2"));

        Assert.That(battle.CanMove("b1", "c3"), Is.True);
    }

    [Test]
    public void King_may_step_onto_an_attacked_square()
    {
        var battle = Make((K, Side.White, "c1"), (K, Side.Black, "f6"), (R, Side.Black, "d6"));

        Assert.That(battle.Move("c1", "d1").Accepted, Is.True);
    }

    [Test]
    public void Pawn_on_the_far_edge_cannot_move_and_is_not_promoted()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (P, Side.White, "a6"));

        Assert.That(battle.GetLegalActions().Any(a => a.Action.From == Coord.Parse("a6")), Is.False);
        Assert.That(battle.At("a6").Definition, Is.SameAs(P));
    }

    [Test]
    public void Pawn_has_no_double_step()
    {
        var battle = new Battle(Scenarios.Starter());

        Assert.That(battle.CanMove("b2", "b3"), Is.True);
        Assert.That(battle.CanMove("b2", "b4"), Is.False);
    }

    [Test]
    public void Turn_passes_after_a_valid_action()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"));

        Assert.That(battle.Move("a1", "a2").Accepted, Is.True);
        Assert.That(battle.State.SideToAct, Is.EqualTo(Side.Black));
        Assert.That(battle.State.Round, Is.EqualTo(1));

        battle.Move("f6", "f5");
        Assert.That(battle.State.SideToAct, Is.EqualTo(Side.White));
        Assert.That(battle.State.Round, Is.EqualTo(2));
    }

    [Test]
    public void Illegal_action_is_rejected_without_changing_anything()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"), (R, Side.White, "b1"));
        var before = battle.State.Snapshot();

        var results = new[]
        {
            battle.Move("a1", "a3"),   // not in the king's pattern
            battle.Move("b1", "a1"),   // occupied by a friendly piece
            battle.Attack("b1", "a1"), // friendly target
            battle.Attack("b1", "b4"), // empty square
            battle.Move("c3", "c4"),   // no piece there
        };

        Assert.That(results.Any(r => r.Accepted), Is.False);
        Assert.That(results.All(r => r.Events.Count == 0), Is.True);
        Assert.That(battle.State.Snapshot(), Is.EqualTo(before));
    }

    [Test]
    public void Acting_out_of_turn_is_rejected()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"));
        var before = battle.State.Snapshot();

        Assert.That(battle.Move("f6", "f5").Accepted, Is.False);
        Assert.That(battle.State.Snapshot(), Is.EqualTo(before));
    }

    [Test]
    public void Side_without_a_legal_action_passes()
    {
        var statueKing = new PieceDefinition("King", 'K', 6, 1, true, new PatternStep[0], new PatternStep[0]);
        var battle = Make((K, Side.White, "a1"), (statueKing, Side.Black, "f6"));

        var result = battle.Move("a1", "a2");

        Assert.That(result.Events.OfType<TurnPassed>().Single().Side, Is.EqualTo(Side.Black));
        Assert.That(battle.State.SideToAct, Is.EqualTo(Side.White));
        Assert.That(battle.State.Round, Is.EqualTo(2));
    }
}
