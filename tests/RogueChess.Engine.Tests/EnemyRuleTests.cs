using NUnit.Framework;
using RogueChess.Engine;
using RogueChess.Engine.Enemy;
using static RogueChess.Engine.Tests.TestBoard;

namespace RogueChess.Engine.Tests;

public class EnemyRuleTests
{
    private sealed class StubRule : BehaviourRule
    {
        private readonly Func<IReadOnlyList<LegalAction>, LegalAction> _choose;

        public StubRule(string name, Func<IReadOnlyList<LegalAction>, LegalAction> choose)
        {
            Name = name;
            _choose = choose;
        }

        public override string Name { get; }
        public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions) => _choose(actions);
    }

    private static Behaviour Only(BehaviourRule rule) => new Behaviour("test", rule, new Rules.Fallback());

    private static BattleAction MoveAction(string from, string to) => BattleAction.Move(Coord.Parse(from), Coord.Parse(to));
    private static BattleAction AttackAction(string from, string to) => BattleAction.Attack(Coord.Parse(from), Coord.Parse(to));

    private static LegalAction Try(BehaviourRule rule, Battle battle) =>
        rule.TryChoose(battle, ActionOrder.Sort(battle.State, battle.GetLegalActions()));

    // --- framework ---

    [Test]
    public void Earlier_rule_wins_and_is_named()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"));
        var behaviour = new Behaviour("test",
            new StubRule("never", _ => null),
            new StubRule("first", actions => actions[1]),
            new StubRule("second", actions => actions[2]),
            new Rules.Fallback());

        var choice = behaviour.Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("first"));
        Assert.That(choice.Action, Is.EqualTo(MoveAction("a1", "a2")));
    }

    [Test]
    public void Fallback_chooses_a_legal_action_and_is_named()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"));

        var choice = Only(new StubRule("never", _ => null)).Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("fallback"));
        Assert.That(battle.Apply(choice.Action).Accepted, Is.True);
    }

    [Test]
    public void Same_position_gives_the_same_action()
    {
        var battle = new Battle(Scenarios.Starter());

        foreach (var behaviour in Archetypes.All)
            Assert.That(behaviour.Choose(battle).Action, Is.EqualTo(behaviour.Choose(battle).Action));
    }

    [Test]
    public void Behaviour_must_end_with_the_fallback()
    {
        Assert.Throws<ArgumentException>(() => new Behaviour("bad", new Rules.Strike()));
        Assert.Throws<ArgumentException>(() => new Behaviour("empty"));
    }

    [Test]
    public void No_choice_without_a_legal_action()
    {
        var battle = Make((K, Side.White, "f1"), (K.WithStats(1, 1), Side.Black, "a5"), (R, Side.White, "a1"));
        battle.Attack("a1", "a5");

        Assert.That(Archetypes.Brute.Choose(battle), Is.Null);
    }

    // --- attack rules ---

    [Test]
    public void Finish_king_prefers_the_king_over_another_kill()
    {
        var battle = Make((K, Side.White, "f1"), (K.WithStats(2, 1), Side.Black, "a5"), (R, Side.White, "a1"),
            (R, Side.White, "c1"), (R.WithStats(1, 2), Side.Black, "c4"));

        var choice = Only(new Rules.FinishKing()).Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("finish king"));
        Assert.That(choice.Action, Is.EqualTo(AttackAction("a1", "a5")));
    }

    [Test]
    public void Finish_king_needs_a_lethal_hit()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "a5"), (R, Side.White, "a1"));

        Assert.That(Try(new Rules.FinishKing(), battle), Is.Null);
    }

    [Test]
    public void Attack_king_takes_the_most_damaging_hit()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "d5"), (R, Side.White, "d1"), (N, Side.White, "c3"));

        var choice = Only(new Rules.AttackKing()).Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("attack king"));
        Assert.That(choice.Action, Is.EqualTo(AttackAction("d1", "d5")));
        Assert.That(choice.Preview.Damage, Is.EqualTo(3));
    }

    [Test]
    public void Kill_prefers_the_more_valuable_target()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (R, Side.White, "a1"), (P, Side.Black, "a4"),
            (R, Side.White, "c1"), (N, Side.Black, "c4"));

        var choice = Only(new Rules.Kill()).Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("kill"));
        Assert.That(choice.Action, Is.EqualTo(AttackAction("c1", "c4")));
    }

    [Test]
    public void Strike_prefers_more_damage()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "d5"), (R, Side.White, "d1"), (N, Side.White, "c3"),
            (P, Side.White, "a2"), (R, Side.Black, "b3"));

        var choice = Only(new Rules.Strike()).Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("strike"));
        Assert.That(choice.Action, Is.EqualTo(AttackAction("d1", "d5")));
    }

    [Test]
    public void Strike_prefers_the_more_valuable_target_at_equal_damage()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (P, Side.White, "c2"),
            (N, Side.Black, "b3"), (R, Side.Black, "d3"));

        Assert.That(Only(new Rules.Strike()).Choose(battle).Action, Is.EqualTo(AttackAction("c2", "d3")));
    }

    [Test]
    public void Equal_attacks_are_decided_by_the_fixed_order()
    {
        var asWhite = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (P, Side.White, "c2"),
            (P, Side.Black, "b3"), (P, Side.Black, "d3"));
        var config = Config(null, (K, Side.White, "f1"), (K, Side.Black, "f6"), (P, Side.Black, "c5"),
            (P, Side.White, "b4"), (P, Side.White, "d4"));
        config.FirstToAct = Side.Black;
        var asBlack = new Battle(config);

        var strike = Only(new Rules.Strike());

        Assert.That(strike.Choose(asWhite).Action, Is.EqualTo(AttackAction("c2", "b3")));
        Assert.That(strike.Choose(asWhite).Action, Is.EqualTo(AttackAction("c2", "b3")));
        Assert.That(strike.Choose(asBlack).Action, Is.EqualTo(AttackAction("c5", "b4")));
    }

    [Test]
    public void Attack_rules_do_not_apply_without_an_attack()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"));

        Assert.That(Try(new Rules.FinishKing(), battle), Is.Null);
        Assert.That(Try(new Rules.AttackKing(), battle), Is.Null);
        Assert.That(Try(new Rules.Kill(), battle), Is.Null);
        Assert.That(Try(new Rules.Strike(), battle), Is.Null);
    }

    // --- protect king ---

    [Test]
    public void King_steps_out_of_an_attack()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"), (R, Side.Black, "a6"));

        var choice = Only(new Rules.ProtectKing()).Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("protect king"));
        Assert.That(choice.Action, Is.EqualTo(MoveAction("a1", "b1")));
    }

    [Test]
    public void Protect_king_does_not_apply_when_the_king_is_not_attacked()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"), (R, Side.Black, "c6"));

        Assert.That(Try(new Rules.ProtectKing(), battle), Is.Null);
    }

    [Test]
    public void Protect_king_does_not_apply_without_a_safe_square()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f5"), (R, Side.Black, "a6"), (R, Side.Black, "b6"));

        Assert.That(Try(new Rules.ProtectKing(), battle), Is.Null);
    }

    [Test]
    public void Square_behind_the_king_on_the_line_of_attack_is_not_safe()
    {
        // a2 is shielded by the king itself until the king steps onto it.
        var battle = Make((K, Side.White, "a3"), (K, Side.Black, "f6"), (R, Side.Black, "a6"));

        Assert.That(Only(new Rules.ProtectKing()).Choose(battle).Action, Is.EqualTo(MoveAction("a3", "b2")));
    }

    // --- advance ---

    [Test]
    public void Advance_takes_the_largest_step_toward_the_king()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "a6"), (R, Side.White, "a1"), (P, Side.White, "c2"));

        var choice = Only(new Rules.Advance()).Choose(battle);

        Assert.That(choice.RuleName, Is.EqualTo("advance"));
        Assert.That(choice.Action, Is.EqualTo(MoveAction("a1", "a5")));
    }

    [Test]
    public void Advance_does_not_apply_when_no_move_gets_closer()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "b2"));

        Assert.That(Try(new Rules.Advance(), battle), Is.Null);
    }

    [Test]
    public void Advance_can_exclude_the_king()
    {
        var battle = Make((K, Side.White, "a1"), (K, Side.Black, "f6"));

        Assert.That(Try(new Rules.Advance(includeKing: false), battle), Is.Null);
        Assert.That(Try(new Rules.Advance(), battle), Is.Not.Null);
    }
}
