using NUnit.Framework;
using RogueChess.Engine;
using static RogueChess.Engine.Tests.TestBoard;

namespace RogueChess.Engine.Tests;

public class EndingTests
{
    private static PieceDefinition KingWithHp(int hp) => K.WithStats(hp, 1);

    [Test]
    public void Defeating_the_king_wins_the_battle()
    {
        var battle = Make((K, Side.White, "f1"), (KingWithHp(2), Side.Black, "a5"), (R, Side.White, "a1"));

        var result = battle.Attack("a1", "a5");

        Assert.That(battle.State.Result, Is.EqualTo(BattleResult.WhiteWins));
        Assert.That(battle.State.EndReason, Is.EqualTo(EndReason.KingDefeated));
        var ended = (BattleEnded)result.Events.Last();
        Assert.That(ended.Result, Is.EqualTo(BattleResult.WhiteWins));
        Assert.That(ended.Reason, Is.EqualTo(EndReason.KingDefeated));
    }

    [Test]
    public void No_action_is_possible_after_the_end()
    {
        var battle = Make((K, Side.White, "f1"), (KingWithHp(2), Side.Black, "a5"), (R, Side.White, "a1"));
        battle.Attack("a1", "a5");
        var before = battle.State.Snapshot();

        Assert.That(battle.GetLegalActions(), Is.Empty);
        Assert.That(battle.Move("f1", "f2").Accepted, Is.False);
        Assert.That(battle.Move("a5", "a4").Accepted, Is.False);
        Assert.That(battle.State.Snapshot(), Is.EqualTo(before));
    }

    [Test]
    public void No_fatigue_before_the_start_round()
    {
        var battle = Make(new BattleRules { FatigueStartRound = 3 }, (K, Side.White, "a1"), (K, Side.Black, "f6"));

        battle.Move("a1", "a2");
        var result = battle.Move("f6", "f5");

        Assert.That(battle.State.Round, Is.EqualTo(2));
        Assert.That(result.Events.OfType<FatigueApplied>(), Is.Empty);
        Assert.That(battle.At("a2").Hp, Is.EqualTo(6));
        Assert.That(battle.At("f5").Hp, Is.EqualTo(6));
    }

    [Test]
    public void Fatigue_damages_both_kings_from_the_start_round()
    {
        var battle = Make(new BattleRules { FatigueStartRound = 2 }, (K, Side.White, "a1"), (K, Side.Black, "f6"));

        battle.Move("a1", "a2");
        var result = battle.Move("f6", "f5");

        Assert.That(result.Events.OfType<FatigueApplied>().Count(), Is.EqualTo(1));
        Assert.That(battle.At("a2").Hp, Is.EqualTo(5));
        Assert.That(battle.At("f5").Hp, Is.EqualTo(5));
        Assert.That(battle.State.Result, Is.EqualTo(BattleResult.Ongoing));
    }

    [Test]
    public void Fatigue_decides_the_battle_for_the_surviving_king()
    {
        var battle = Make(new BattleRules { FatigueStartRound = 2 },
            (KingWithHp(1), Side.White, "a1"), (K, Side.Black, "f6"));

        battle.Move("a1", "a2");
        var result = battle.Move("f6", "f5");

        Assert.That(battle.State.Result, Is.EqualTo(BattleResult.BlackWins));
        Assert.That(battle.State.EndReason, Is.EqualTo(EndReason.Fatigue));
        Assert.That(result.Events.Last(), Is.InstanceOf<BattleEnded>());
    }

    [Test]
    public void Both_kings_falling_to_fatigue_is_a_draw()
    {
        var battle = Make(new BattleRules { FatigueStartRound = 2 },
            (KingWithHp(1), Side.White, "a1"), (KingWithHp(1), Side.Black, "f6"));

        battle.Move("a1", "a2");
        battle.Move("f6", "f5");

        Assert.That(battle.State.Result, Is.EqualTo(BattleResult.Draw));
        Assert.That(battle.State.EndReason, Is.EqualTo(EndReason.Fatigue));
    }
}
