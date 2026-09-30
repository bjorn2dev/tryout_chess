using System.Globalization;
using RogueChess.ConsoleApp;
using RogueChess.Engine;

const string Usage = """
    Usage:
      play [--script <file>]
          Hot-seat battle on the starter scenario. Enter actions as "<from> <to>", e.g. "b2 b3".
          Other input: "list" shows all legal actions, "quit" stops.
      simulate [--games N] [--seed S] [--white greedy|random] [--black greedy|random]
               [--bonus N] [--cap N] [--fatigue-start N] [--king-hp N] [--record <file>]
          Plays N bot battles and prints pacing and balance numbers.
          --record writes the actions of the first battle as a script for "play --script".
    """;

try
{
    if (args.Length == 0)
    {
        Console.WriteLine(Usage);
        return 1;
    }

    var options = ParseOptions(args.Skip(1).ToArray());
    switch (args[0])
    {
        case "play": return Play(options);
        case "simulate": return Simulate(options);
        default:
            Console.WriteLine(Usage);
            return 1;
    }
}
catch (Exception e) when (e is ArgumentException or FormatException or IOException)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string>();
    for (int i = 0; i < args.Length; i += 2)
    {
        if (!args[i].StartsWith("--") || i + 1 >= args.Length)
            throw new ArgumentException($"Expected '--option value' but got '{args[i]}'.");
        options[args[i].Substring(2)] = args[i + 1];
    }
    return options;
}

static int Int(Dictionary<string, string> options, string name, int fallback) =>
    options.TryGetValue(name, out var text) ? int.Parse(text, CultureInfo.InvariantCulture) : fallback;

static int Play(Dictionary<string, string> options)
{
    TextReader input = options.TryGetValue("script", out var script) ? new StreamReader(script) : Console.In;
    var battle = new Battle(Scenarios.Starter());

    while (battle.State.Result == BattleResult.Ongoing)
    {
        PrintBoard(battle.State);
        Console.Write($"Round {battle.State.Round}, {battle.State.SideToAct} to act > ");
        var line = input.ReadLine();
        if (line == null || line.Trim() == "quit")
        {
            Console.WriteLine();
            Console.WriteLine("Battle not finished.");
            return 1;
        }
        if (input != Console.In) Console.WriteLine(line);

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1 && words[0] == "list")
        {
            foreach (var legal in battle.GetLegalActions())
                Console.WriteLine("  " + Describe(legal));
            continue;
        }
        if (words.Length != 2 || !Coord.TryParse(words[0], out var from) || !Coord.TryParse(words[1], out var to))
        {
            Console.WriteLine("Enter an action as \"<from> <to>\", or \"list\" or \"quit\".");
            continue;
        }

        var kind = battle.State.PieceAt(to) == null ? ActionKind.Move : ActionKind.Attack;
        var result = battle.Apply(new BattleAction(kind, from, to));
        if (!result.Accepted)
        {
            Console.WriteLine(result.RejectionReason);
            continue;
        }
        foreach (var e in result.Events)
            Console.WriteLine("  " + e);
    }

    PrintBoard(battle.State);
    Console.WriteLine($"Winner: {Winner(battle.State.Result)} ({battle.State.EndReason})");
    return 0;
}

static string Winner(BattleResult result) => result switch
{
    BattleResult.WhiteWins => "White",
    BattleResult.BlackWins => "Black",
    _ => "nobody, it is a draw"
};

static string Describe(LegalAction legal)
{
    if (legal.Preview == null) return $"{legal.Action.From} {legal.Action.To}  move";
    var p = legal.Preview;
    string outcome = p.IsLethal ? "lethal" : $"target survives, attacker ends on {p.AttackerEnd}";
    return $"{legal.Action.From} {legal.Action.To}  attack for {p.Damage} ({p.Supporters.Count} supporting), {outcome}";
}

// White pieces are upper case, Black lower case; the digit is the piece's HP.
static void PrintBoard(BattleState state)
{
    Console.WriteLine();
    for (int rank = state.Height - 1; rank >= 0; rank--)
    {
        Console.Write($"{rank + 1,2} ");
        for (int file = 0; file < state.Width; file++)
        {
            var piece = state.PieceAt(new Coord(file, rank));
            if (piece == null)
            {
                Console.Write(" .  ");
                continue;
            }
            char symbol = piece.Side == Side.White ? piece.Definition.Symbol : char.ToLowerInvariant(piece.Definition.Symbol);
            Console.Write($" {symbol}{piece.Hp,-2}");
        }
        Console.WriteLine();
    }
    Console.Write("   ");
    for (int file = 0; file < state.Width; file++)
        Console.Write($" {(char)('a' + file)}  ");
    Console.WriteLine();
}

static int Simulate(Dictionary<string, string> options)
{
    int games = Int(options, "games", 1000);
    int seed = Int(options, "seed", 1);
    string whiteName = options.GetValueOrDefault("white", "greedy");
    string blackName = options.GetValueOrDefault("black", "greedy");
    var white = Bots.Parse(whiteName);
    var black = Bots.Parse(blackName);

    var defaults = new BattleRules();
    var rules = new BattleRules
    {
        SupportBonusPerPiece = Int(options, "bonus", defaults.SupportBonusPerPiece),
        SupportBonusCap = Int(options, "cap", defaults.SupportBonusCap),
        FatigueStartRound = Int(options, "fatigue-start", defaults.FatigueStartRound),
        FatigueDamage = defaults.FatigueDamage
    };
    int kingHp = Int(options, "king-hp", StandardPieces.King.Hp);
    var king = StandardPieces.King.WithStats(kingHp, StandardPieces.King.Atk);

    var rng = new Random(seed);
    long totalRounds = 0;
    int whiteWins = 0, blackWins = 0, draws = 0, fatigueEndings = 0;

    for (int game = 0; game < games; game++)
    {
        var battle = new Battle(Scenarios.Starter(rules, king));
        var record = game == 0 && options.ContainsKey("record") ? new List<string>() : null;

        while (battle.State.Result == BattleResult.Ongoing)
        {
            var bot = battle.State.SideToAct == Side.White ? white : black;
            var choice = bot(battle, battle.GetLegalActions(), rng);
            record?.Add($"{choice.Action.From} {choice.Action.To}");
            battle.Apply(choice.Action);
        }

        if (record != null) File.WriteAllLines(options["record"], record);
        totalRounds += battle.State.Round;
        if (battle.State.EndReason == EndReason.Fatigue) fatigueEndings++;
        switch (battle.State.Result)
        {
            case BattleResult.WhiteWins: whiteWins++; break;
            case BattleResult.BlackWins: blackWins++; break;
            default: draws++; break;
        }
    }

    var culture = CultureInfo.InvariantCulture;
    Console.WriteLine($"Battles: {games} (seed {seed}), White {whiteName} vs Black {blackName}");
    Console.WriteLine($"Rules: support bonus {rules.SupportBonusPerPiece} (cap {rules.SupportBonusCap}), " +
                      $"fatigue from round {rules.FatigueStartRound}, king HP {kingHp}");
    Console.WriteLine(string.Format(culture, "Average rounds:       {0:F1}", (double)totalRounds / games));
    Console.WriteLine(string.Format(culture, "Ended by fatigue:     {0:F1}%", 100.0 * fatigueEndings / games));
    Console.WriteLine(string.Format(culture, "First side (White):   {0:F1}% wins", 100.0 * whiteWins / games));
    Console.WriteLine(string.Format(culture, "Second side (Black):  {0:F1}% wins", 100.0 * blackWins / games));
    Console.WriteLine(string.Format(culture, "Draws:                {0:F1}%", 100.0 * draws / games));
    return 0;
}
