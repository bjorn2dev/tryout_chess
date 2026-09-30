# tryout_chess

Prototype of the battle rules for a roguelike chess game: chess pieces with hit points, fought on a small board. The rules live in a Unity-free C# library so they can move into a Unity project later.

- `src/RogueChess.Engine` — the rules (netstandard2.1, no dependencies)
- `src/RogueChess.ConsoleApp` — hot-seat play and bot simulation
- `tests/RogueChess.Engine.Tests` — NUnit tests
- `docs/rule-validation.md`, `docs/enemy-validation.md` — simulation results for the rules and the enemies

## Build and test

```bash
dotnet build
```

```bash
dotnet test
```

## Play

Two players at one keyboard, on the 6×6 starter scenario:

```bash
dotnet run --project src/RogueChess.ConsoleApp -- play
```

Enter an action as `<from> <to>`, for example `b2 b3`. Naming a square with an enemy piece attacks it. `list` shows every legal action with its attack preview, `quit` stops. White pieces are upper case, Black lower case, and the digit after a piece is its HP.

## Play against an enemy

You are White and move first; the enemy answers at once and prints the rule that decided its action:

```bash
dotnet run --project src/RogueChess.ConsoleApp -- play --enemy brute
```

The enemies are `brute`, `hunter` and `warden`. Type `peek <from> <to>` to see the enemy's reply to an action without playing it. Enemies never use chance: the same position always gives the same reply.

An enemy tries its rules in order and the first one that applies decides:

| Enemy  | Rule order                                                        |
|--------|-------------------------------------------------------------------|
| Brute  | finish king, kill, strike, advance                                |
| Hunter | finish king, attack king, advance, kill, strike                   |
| Warden | finish king, protect king, kill, strike, advance without its king |

- **finish king**: a lethal attack on your king.
- **attack king**: the most damaging attack on your king.
- **kill**: a lethal attack, on the most valuable piece (HP plus ATK).
- **strike**: the most damaging attack.
- **protect king**: move its attacked king to a square nothing attacks.
- **advance**: the move that brings a piece closest to your king by the largest step.

If no rule applies the enemy takes its first legal action. Simulation results per enemy are in `docs/enemy-validation.md`.

## Simulate

Play bot battles and print battle length, fatigue endings and win rates:

```bash
dotnet run --project src/RogueChess.ConsoleApp -- simulate --games 1000 --seed 1
```

Options: `--white` and `--black` (`greedy`, `random`, `brute`, `hunter` or `warden`), `--bonus`, `--cap`, `--fatigue-start`, `--king-hp`, and `--record <file>` to save the first battle as a script. When Black is an enemy, only White's actions are saved, for use with `play --enemy`. Replay a script with:

```bash
dotnet run --project src/RogueChess.ConsoleApp -- play --script <file>
```

## The rules: advance-strike

- **One action per turn**: move one piece or attack with one piece. A side with no legal action passes.
- **Movement** is as in chess, except: no check or checkmate (the king may walk into danger), no castling, no en passant, no double pawn step, no promotion.
- **Attack** an enemy piece your piece could capture in chess. Damage is the attacker's ATK plus 1 for every other friendly piece that also attacks the target's square, up to +2.
- **Lethal hit** (target at 0 HP): the target is removed and the attacker takes its square.
- **Non-lethal hit**: the target stays. A piece attacking along a line moves up to the square next to the target; a knight, or a piece already next to the target, stays where it is.
- **Win** by bringing the enemy king to 0 HP.
- **Fatigue**: from round 25 on, both kings lose 1 HP at the start of every round. If both fall at once the battle is a draw.
- There is no randomness and no retaliation.

| Piece  | HP | ATK |
|--------|----|-----|
| Pawn   | 1  | 1   |
| Knight | 2  | 1   |
| Bishop | 2  | 1   |
| Rook   | 3  | 2   |
| Queen  | 3  | 2   |
| King   | 6  | 1   |
