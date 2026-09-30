using NUnit.Framework;
using RogueChess.Engine;
using static RogueChess.Engine.Tests.TestBoard;

namespace RogueChess.Engine.Tests;

public class AttackTests
{
    [Test]
    public void Enemy_on_a_clear_diagonal_can_be_attacked()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (B, Side.White, "c1"), (P, Side.Black, "e3"));

        Assert.That(battle.FindAttack("c1", "e3"), Is.Not.Null);
    }

    [Test]
    public void Blocked_line_of_attack_is_not_legal()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (B, Side.White, "c1"),
            (P, Side.White, "d2"), (P, Side.Black, "e3"));

        Assert.That(battle.FindAttack("c1", "e3"), Is.Null);
    }

    [Test]
    public void Pawn_attacks_diagonally_forward_only()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (P, Side.White, "c3"),
            (R, Side.Black, "c4"), (R, Side.Black, "d4"), (R, Side.Black, "b2"));

        Assert.That(battle.FindAttack("c3", "d4"), Is.Not.Null);
        Assert.That(battle.FindAttack("c3", "c4"), Is.Null);
        Assert.That(battle.FindAttack("c3", "b2"), Is.Null);
    }

    [Test]
    public void Unsupported_attack_deals_atk_and_is_not_retaliated()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (N, Side.White, "b1"), (R, Side.Black, "c3"));

        battle.Attack("b1", "c3");

        Assert.That(battle.At("c3").Hp, Is.EqualTo(2));
        Assert.That(battle.At("b1").Hp, Is.EqualTo(2));
    }

    [Test]
    public void One_supporter_adds_one_damage()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (N, Side.White, "b1"),
            (B, Side.White, "a1"), (R, Side.Black, "c3"));

        battle.Attack("b1", "c3");

        Assert.That(battle.At("c3").Hp, Is.EqualTo(1));
    }

    [Test]
    public void Support_bonus_is_capped()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "c3"), (N, Side.White, "b1"),
            (B, Side.White, "a1"), (R, Side.White, "c1"), (P, Side.White, "d2"));

        Assert.That(battle.FindAttack("b1", "c3").Preview.Supporters, Has.Count.EqualTo(3));
        battle.Attack("b1", "c3");

        Assert.That(battle.At("c3").Hp, Is.EqualTo(3));
    }

    [Test]
    public void Blocked_supporter_does_not_count()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (N, Side.White, "b1"),
            (R, Side.White, "c1"), (P, Side.White, "c2"), (R, Side.Black, "c3"));

        battle.Attack("b1", "c3");

        Assert.That(battle.At("c3").Hp, Is.EqualTo(2));
    }

    [Test]
    public void Support_bonus_of_zero_disables_the_mechanic()
    {
        var battle = Make(new BattleRules { SupportBonusPerPiece = 0 },
            (K, Side.White, "f1"), (K, Side.Black, "f6"), (N, Side.White, "b1"),
            (B, Side.White, "a1"), (R, Side.Black, "c3"));

        battle.Attack("b1", "c3");

        Assert.That(battle.At("c3").Hp, Is.EqualTo(2));
    }

    [Test]
    public void Lethal_hit_removes_the_target_and_takes_its_square()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (R, Side.White, "a1"), (P, Side.Black, "a4"));

        battle.Attack("a1", "a4");

        Assert.That(battle.At("a4").Definition, Is.SameAs(R));
        Assert.That(battle.At("a1"), Is.Null);
        Assert.That(battle.State.Pieces.Any(p => p.Definition == P), Is.False);
    }

    [Test]
    public void Slider_advances_next_to_a_surviving_target()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "a5"), (R, Side.White, "a1"));

        battle.Attack("a1", "a5");

        Assert.That(battle.At("a5").Definition.IsKing, Is.True);
        Assert.That(battle.At("a5").Hp, Is.EqualTo(4));
        Assert.That(battle.At("a4").Definition, Is.SameAs(R));
        Assert.That(battle.At("a1"), Is.Null);
    }

    [Test]
    public void Jumper_stays_after_a_non_lethal_hit()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (N, Side.White, "b1"), (R, Side.Black, "c3"));

        battle.Attack("b1", "c3");

        Assert.That(battle.At("b1").Definition, Is.SameAs(N));
        Assert.That(battle.At("c3").Definition, Is.SameAs(R));
    }

    [Test]
    public void Adjacent_attacker_stays_after_a_non_lethal_hit()
    {
        var battle = Make((K, Side.White, "f1"), (K, Side.Black, "f6"), (P, Side.White, "b2"), (R, Side.Black, "c3"));

        battle.Attack("b2", "c3");

        Assert.That(battle.At("b2").Definition, Is.SameAs(P));
        Assert.That(battle.At("c3").Hp, Is.EqualTo(2));
    }
}
