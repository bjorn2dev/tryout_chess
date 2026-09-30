using NUnit.Framework;
using RogueChess.Engine;
using RogueChess.Engine.Enemy;
using static RogueChess.Engine.Tests.TestBoard;

namespace RogueChess.Engine.Tests;

public class ArchetypeAndPredictionTests
{
    private static BattleAction MoveAction(string from, string to) => BattleAction.Move(Coord.Parse(from), Coord.Parse(to));
    private static BattleAction AttackAction(string from, string to) => BattleAction.Attack(Coord.Parse(from), Coord.Parse(to));

    // White can kill the knight on c4 or hit the king on a5 without killing it.
    private static Battle KillOrHitKing() =>
        Make((K, Side.White, "f1"), (K, Side.Black, "a5"), (R, Side.White, "a1"),
            (R, Side.White, "c1"), (N, Side.Black, "c4"));

    [Test]
    public void Brute_takes_the_kill()
    {
        var choice = Archetypes.Brute.Choose(KillOrHitKing());

        Assert.That(choice.Action, Is.EqualTo(AttackAction("c1", "c4")));
        Assert.That(choice.RuleName, Is.EqualTo("kill"));
    }

    [Test]
    public void Hunter_goes_for_the_king()
    {
        var choice = Archetypes.Hunter.Choose(KillOrHitKing());

        Assert.That(choice.Action, Is.EqualTo(AttackAction("a1", "a5")));
        Assert.That(choice.RuleName, Is.EqualTo("attack king"));
    }

    [Test]
    public void Hunter_walks_past_a_free_kill()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (R, Side.White, "c1"), (P, Side.Black, "c4"));

        var choice = Archetypes.Hunter.Choose(battle);

        Assert.That(choice.Action, Is.EqualTo(MoveAction("c1", "c3")));
        Assert.That(choice.RuleName, Is.EqualTo("advance"));
    }

    [Test]
    public void Warden_saves_its_king_first()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"), (R, Side.Black, "a6"),
            (R, Side.White, "c1"), (P, Side.Black, "c4"));

        var choice = Archetypes.Warden.Choose(battle);

        Assert.That(choice.Action, Is.EqualTo(MoveAction("a1", "b1")));
        Assert.That(choice.RuleName, Is.EqualTo("protect king"));
    }

    [Test]
    public void Warden_never_advances_its_king()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"));

        Assert.That(Archetypes.Warden.Choose(battle).RuleName, Is.EqualTo("fallback"));
        Assert.That(Archetypes.Brute.Choose(battle).RuleName, Is.EqualTo("advance"));
    }

    [Test]
    public void Archetypes_are_found_by_name()
    {
        Assert.That(Archetypes.ByName("hunter"), Is.SameAs(Archetypes.Hunter));
        Assert.That(Archetypes.ByName("nobody"), Is.Null);
    }

    [Test]
    public void No_reply_after_a_winning_action()
    {
        var battle = Make((K, Side.White, "f1"), (K.WithStats(2, 1), Side.Black, "a5"), (R, Side.White, "a1"));

        Assert.That(Archetypes.Brute.PredictReply(battle, AttackAction("a1", "a5")), Is.Null);
        Assert.That(battle.State.Result, Is.EqualTo(BattleResult.Ongoing));
    }

    [Test]
    public void No_reply_when_the_enemy_has_no_legal_action()
    {
        var statueKing = new PieceDefinition("King", 'K', 6, 1, true, new PatternStep[0], new PatternStep[0]);
        var battle = Make((K, Side.White, "a1"), (statueKing, Side.Black, "f6"));

        Assert.That(Archetypes.Brute.PredictReply(battle, MoveAction("a1", "a2")), Is.Null);
    }

    [Test]
    public void Predicting_the_reply_to_an_illegal_action_is_an_error()
    {
        var battle = new Battle(Scenarios.Starter());

        Assert.Throws<ArgumentException>(() => Archetypes.Brute.PredictReply(battle, MoveAction("b2", "b5")));
    }

    [Test]
    public void Predicted_reply_equals_the_actual_reply_and_leaves_the_battle_unchanged()
    {
        int predictions = 0;
        foreach (var enemy in Archetypes.All)
        {
            foreach (int seed in new[] { 1, 2, 3 })
            {
                var rng = new Random(seed);
                var battle = new Battle(Scenarios.Starter());
                while (battle.State.Result == BattleResult.Ongoing)
                {
                    // White is the player, picking random legal actions.
                    var legal = battle.GetLegalActions();
                    var playerAction = legal[rng.Next(legal.Count)].Action;

                    var before = battle.State.Snapshot();
                    var predicted = enemy.PredictReply(battle, playerAction);
                    Assert.That(battle.State.Snapshot(), Is.EqualTo(before));

                    battle.Apply(playerAction);
                    predictions++;
                    if (predicted == null)
                    {
                        Assert.That(battle.State.Result != BattleResult.Ongoing || battle.State.SideToAct == Side.White, Is.True);
                        continue;
                    }

                    Assert.That(battle.State.SideToAct, Is.EqualTo(Side.Black));
                    var actual = enemy.Choose(battle);
                    Assert.That(actual.Action, Is.EqualTo(predicted.Action));
                    Assert.That(actual.RuleName, Is.EqualTo(predicted.RuleName));
                    Assert.That(battle.Apply(actual.Action).Accepted, Is.True);
                }
            }
        }
        Assert.That(predictions, Is.GreaterThan(50));
    }

    [Test]
    public void Archetypes_only_return_accepted_actions_as_either_colour()
    {
        foreach (var archetype in Archetypes.All)
        {
            foreach (var archetypeSide in new[] { Side.White, Side.Black })
            {
                var rng = new Random(7);
                var battle = new Battle(Scenarios.Starter());
                int choices = 0;
                while (battle.State.Result == BattleResult.Ongoing)
                {
                    if (battle.State.SideToAct == archetypeSide)
                    {
                        var choice = archetype.Choose(battle);
                        Assert.That(archetype.Choose(battle).Action, Is.EqualTo(choice.Action));
                        Assert.That(battle.Apply(choice.Action).Accepted, Is.True);
                        choices++;
                    }
                    else
                    {
                        var legal = battle.GetLegalActions();
                        battle.Apply(legal[rng.Next(legal.Count)].Action);
                    }
                }
                Assert.That(choices, Is.GreaterThan(3));
            }
        }
    }
}
