using RogueChess.Engine;
using RogueChess.Engine.Enemy;

namespace RogueChess.ConsoleApp;

/// <summary>Simple players used only to measure the rules, not the game's enemy AI.</summary>
internal static class Bots
{
    public delegate LegalAction Bot(Battle battle, IReadOnlyList<LegalAction> actions, Random rng);

    public static Bot Parse(string name)
    {
        if (name == "greedy") return Greedy;
        if (name == "random") return RandomBot;

        var archetype = Archetypes.ByName(name);
        if (archetype == null)
            throw new ArgumentException($"Unknown player '{name}'. Use greedy, random, brute, hunter or warden.");
        return (battle, actions, rng) =>
        {
            var choice = archetype.Choose(battle);
            return new LegalAction(choice.Action, choice.Preview);
        };
    }

    public static LegalAction RandomBot(Battle battle, IReadOnlyList<LegalAction> actions, Random rng) =>
        actions[rng.Next(actions.Count)];

    /// <summary>
    /// Takes the most damaging attack (preferring kills and hits on the king);
    /// without an attack it steps toward the enemy king. Ties are broken at random.
    /// </summary>
    public static LegalAction Greedy(Battle battle, IReadOnlyList<LegalAction> actions, Random rng)
    {
        var enemyKing = battle.State.King(battle.State.SideToAct.Opponent()).Position;
        int bestScore = int.MinValue;
        var best = new List<LegalAction>();

        foreach (var legal in actions)
        {
            int score;
            if (legal.Preview != null)
            {
                bool onKing = legal.Action.To == enemyKing;
                score = 100 + legal.Preview.Damage * 10
                    + (legal.Preview.IsLethal ? 50 : 0)
                    + (onKing ? (legal.Preview.IsLethal ? 1000 : 30) : 0);
            }
            else
            {
                score = -Distance(legal.Action.To, enemyKing);
            }

            if (score > bestScore)
            {
                bestScore = score;
                best.Clear();
            }
            if (score == bestScore) best.Add(legal);
        }
        return best[rng.Next(best.Count)];
    }

    private static int Distance(Coord a, Coord b) =>
        Math.Max(Math.Abs(a.File - b.File), Math.Abs(a.Rank - b.Rank));
}
