using NUnit.Framework;
using RogueChess.Engine;
using static RogueChess.Engine.Tests.TestBoard;

namespace RogueChess.Engine.Tests;

public class PreviewAndEventTests
{
    /// <summary>Positions reached by playing random legal actions from the starter scenario.</summary>
    private static IEnumerable<Battle> RandomGamePositions(int seed)
    {
        var rng = new Random(seed);
        var battle = new Battle(Scenarios.Starter());
        while (battle.State.Result == BattleResult.Ongoing)
        {
            yield return battle;
            var actions = battle.GetLegalActions();
            // Favour attacks so the games cover plenty of them.
            var attacks = actions.Where(a => a.Preview != null).ToList();
            var pick = attacks.Count > 0 && rng.Next(2) == 0 ? attacks : actions.ToList();
            battle.Apply(pick[rng.Next(pick.Count)].Action);
        }
    }

    [Test]
    public void Preview_states_damage_supporters_lethality_and_end_square()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "d5"), (R, Side.White, "d1"),
            (N, Side.White, "c3"), (P, Side.Black, "a4"), (R, Side.White, "a1"));

        var onKing = battle.FindAttack("d1", "d5").Preview;
        Assert.That(onKing.Damage, Is.EqualTo(3));
        Assert.That(onKing.Supporters, Is.EqualTo(new[] { Coord.Parse("c3") }));
        Assert.That(onKing.IsLethal, Is.False);
        Assert.That(onKing.AttackerEnd, Is.EqualTo(Coord.Parse("d4")));

        var onPawn = battle.FindAttack("a1", "a4").Preview;
        Assert.That(onPawn.IsLethal, Is.True);
        Assert.That(onPawn.AttackerEnd, Is.EqualTo(Coord.Parse("a4")));

        battle.Attack("d1", "d5");
        Assert.That(battle.At("d5").Hp, Is.EqualTo(3));
        Assert.That(battle.At("d4").Definition, Is.SameAs(R));
    }

    [Test]
    public void Every_previewed_attack_has_exactly_the_previewed_outcome()
    {
        int checkedAttacks = 0;
        foreach (int seed in new[] { 1, 2, 3, 4, 5 })
        {
            foreach (var battle in RandomGamePositions(seed))
            {
                foreach (var legal in battle.GetLegalActions().Where(a => a.Preview != null))
                {
                    var copy = battle.Clone();
                    var attackerId = copy.State.PieceAt(legal.Action.From).Id;
                    var target = copy.State.PieceAt(legal.Action.To);
                    int hpBefore = target.Hp;

                    var result = copy.Apply(legal.Action);
                    Assert.That(result.Accepted, Is.True);

                    // Look only at the attack itself; fatigue may follow in the same event list.
                    var attack = result.Events.TakeWhile(e => !(e is FatigueApplied)).ToList();
                    var damaged = attack.OfType<PieceDamaged>().Single();
                    bool defeated = attack.OfType<PieceDefeated>().Any(d => d.Piece.Id == target.Id);
                    var moved = attack.OfType<PieceMoved>().SingleOrDefault(m => m.Piece.Id == attackerId);

                    Assert.That(damaged.Amount, Is.EqualTo(legal.Preview.Damage));
                    Assert.That(damaged.RemainingHp, Is.EqualTo(Math.Max(0, hpBefore - legal.Preview.Damage)));
                    Assert.That(defeated, Is.EqualTo(legal.Preview.IsLethal));
                    Assert.That(moved?.To ?? legal.Action.From, Is.EqualTo(legal.Preview.AttackerEnd));
                    checkedAttacks++;
                }
            }
        }
        Assert.That(checkedAttacks, Is.GreaterThan(100));
    }

    [Test]
    public void Lethal_attack_events_are_in_order()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (R, Side.White, "a1"), (P, Side.Black, "a4"));

        var events = battle.Attack("a1", "a4").Events;

        Assert.That(events.Select(e => e.GetType()), Is.EqualTo(new[]
            { typeof(PieceAttacked), typeof(PieceDamaged), typeof(PieceDefeated), typeof(PieceMoved) }));
        var attacked = (PieceAttacked)events[0];
        Assert.That(attacked.Damage, Is.EqualTo(2));
        Assert.That(attacked.Supporters, Is.Empty);
        Assert.That(((PieceDamaged)events[1]).RemainingHp, Is.EqualTo(0));
        Assert.That(((PieceMoved)events[3]).To, Is.EqualTo(Coord.Parse("a4")));
    }

    [Test]
    public void Winning_attack_ends_with_battle_ended()
    {
        var battle = Make((K, Side.White, "f1"), (K.WithStats(1, 1), Side.Black, "a5"), (R, Side.White, "a1"));

        var ended = battle.Attack("a1", "a5").Events.Last() as BattleEnded;

        Assert.That(ended, Is.Not.Null);
        Assert.That(ended.Result, Is.EqualTo(BattleResult.WhiteWins));
        Assert.That(ended.Reason, Is.EqualTo(EndReason.KingDefeated));
    }

    [Test]
    public void Events_alone_reproduce_the_resulting_position()
    {
        int actions = 0;
        foreach (int seed in new[] { 11, 12, 13 })
        {
            var rng = new Random(seed);
            var battle = new Battle(Scenarios.Starter(new BattleRules { FatigueStartRound = 8 }));
            while (battle.State.Result == BattleResult.Ongoing)
            {
                var previous = battle.State.Clone();
                var legal = battle.GetLegalActions();
                var result = battle.Apply(legal[rng.Next(legal.Count)].Action);

                Replay(previous, result.Events);

                Assert.That(Position(previous), Is.EqualTo(Position(battle.State)));
                actions++;
            }
        }
        Assert.That(actions, Is.GreaterThan(30));
    }

    private static void Replay(BattleState state, IEnumerable<BattleEvent> events)
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case PieceMoved moved:
                    state.Pieces.Single(p => p.Id == moved.Piece.Id).Position = moved.To;
                    break;
                case PieceDamaged damaged:
                    state.Pieces.Single(p => p.Id == damaged.Piece.Id).Hp = damaged.RemainingHp;
                    break;
                case PieceDefeated defeated:
                    state.Pieces.RemoveAll(p => p.Id == defeated.Piece.Id);
                    break;
            }
        }
    }
}
