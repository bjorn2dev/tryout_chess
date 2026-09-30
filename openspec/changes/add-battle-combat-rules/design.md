# Design

## Context

See proposal.md for motivation. The repository contains only a README; there is no Unity project yet. The existing `ChessAPI` engine (separate repo) computes moves from linear board indexes (`MoveValidatorHelper.GetMovementRange`: 7/9 diagonal, 8 vertical), which hard-codes 8 files, and simulates moves by cloning a dictionary of tiles keyed by string annotations. Neither fits variable boards, so the battle engine is written fresh and `ChessAPI` serves only as a reference for piece patterns.

Constraints:
- The engine must later drop into a Unity assembly without `UnityEngine` references: target `netstandard2.1`, C# 9 language features at most, no dependency injection framework.
- The rules must be judged for fun before Unity work starts, so they need to be playable and measurable from the command line.

## Goals / Non-Goals

**Goals:**
- A rule set that keeps chess movement recognisable, removes risk-free ranged attacks, and ends battles in roughly 10–20 rounds.
- All tuning values (stats, support bonus, cap, fatigue) are data, so balance changes need no code change.
- A result model (events, previews) that a Unity presentation layer and a future AI can consume as is.

**Non-Goals:**
- A search-based AI. The simulation bots exist only to measure the rules.
- Performance work for deep search (make/unmake, bitboards).
- Save files, seeds, run state.

## Decisions

### 1. Advance-strike instead of reach-strike
On a non-lethal hit the attacker moves to the square next to the target along its line of attack.

- *Why*: in the reach-strike sketch a queen or rook hits from across the board and stays safe, so sliders dominate and nobody needs to approach. Advancing turns every attack into a commitment: the attacker ends in contact, where the target and its neighbours can hit back next turn. Positions stay readable because the attacker always ends either on the target's square or beside it.
- The knight has no line, so it stays where it is. That gives it a distinct role as the one hit-and-stay harasser without adding a special rule.
- *Alternatives*: reach-strike (rejected: risk-free archers); bump back to the start square (same problem); retaliation damage (adds bookkeeping per attack and makes previews harder to read — can return later as a piece ability).

### 2. Support bonus as the damage formula
Damage is `ATK + min(cap, bonusPerPiece × supporters)`, where supporters are other friendly pieces attacking the target square before the attack.

- *Why*: this is the chess idea of counting attackers on a square, reused as damage. It rewards coordination, shortens fights (fewer chip-damage turns, less clogging) and makes the king fight resemble building a mating net. It needs no new concept for a chess player.
- Supporters are counted in the pre-attack position, without x-ray through the attacker. Simple to explain and to preview.
- Setting `bonusPerPiece` to 0 disables the mechanic, so it can be A/B tested in the simulation.
- *Alternatives*: flat ATK only (rejected: slow, no incentive to coordinate); flanking by adjacency (less chess-like, favours short-range pieces).

### 3. Small integer stats
Initial tuning table, to be adjusted from simulation results:

| Piece  | HP | ATK |
|--------|----|-----|
| Pawn   | 1  | 1   |
| Knight | 2  | 1   |
| Bishop | 2  | 1   |
| Rook   | 3  | 2   |
| Queen  | 3  | 2   |
| King   | 6  | 1   |

Defaults: `bonusPerPiece = 1`, `cap = 2`, `fatigueStartRound = 25` (raised from 20 after simulation, see `docs/rule-validation.md`), `fatigueDamage = 1`.

- *Why*: with small numbers the player can read "this dies in one supported hit" without arithmetic. Pawns still die to any hit, as in chess.

### 4. Fatigue hits both kings at round start
- *Why*: guarantees termination (needed for the simulation and for pacing) and is symmetric, so it does not favour the side that moves first. A simultaneous double defeat is a draw; what a draw means for a run is decided by the run loop later.
- *Alternative*: a hard round limit with a tiebreak on king HP (rejected: abrupt, and needs an arbitrary tiebreak).

### 5. No king safety, no special moves
Legal actions are the pseudo-legal ones. Castling, en passant, double step and promotion are dropped.

- *Why*: they exist to serve checkmate and the 8×8 opening position, neither of which applies on small boards with HP. Dropping them removes most of the classic engine's complexity.

### 6. Code layout
```
src/RogueChess.Engine/          netstandard2.1, no UnityEngine
  Coord, Side, PieceDefinition (patterns as direction vectors + range + slide/jump)
  BattleConfig, BattleState, BattleAction, ActionPreview, BattleEvent
  Battle (GetLegalActions, Apply)
src/RogueChess.ConsoleApp/      ASCII hot-seat play + `simulate` command
tests/RogueChess.Engine.Tests/  NUnit
```

- Patterns are data: a list of `(dx, dy)` directions with a maximum range and a slide/jump flag, separately for move and capture. This replaces the `MovementType` enum of `ChessAPI` and is what custom pieces will use later.
- Coordinates are `(file, rank)` integers; no string annotations inside the engine.
- `Battle.Apply` mutates its state and returns events; state is a plain, copyable data object so a later AI can clone it.
- NUnit rather than xUnit, because Unity Test Framework uses NUnit and the tests can move along with the engine.

### 7. Measuring the rules with self-play
The console `simulate` command plays N battles between two simple bots (greedy: highest-damage attack, preferring lethal and king hits, otherwise step toward the enemy king; and random) on the starter scenario and prints average rounds, share of battles ended by fatigue, and first-side win rate.

- Targets: average 10–20 rounds, under 10% ended by fatigue, first-side win rate between 45% and 55% for greedy vs greedy.
- Starter scenario: 6×6, back rank `. R N K B .`, pawns on files b–e of the second rank, mirrored for the other side.
- The bots use a seeded RNG owned by the console app; the engine itself stays deterministic.

## Risks / Trade-offs

- [Support bonus makes ganging up on the king too fast] → the cap and king HP are data; the simulation reports battle length so this shows up immediately.
- [Pawns that cannot promote become dead weight on the far edge] → accepted for the prototype; promotion can return as a per-piece rule once the core is proven.
- [First-move advantage on small boards] → measured by the simulation; mitigations are scenario layout or giving the second side a small HP edge.
- [Bot results do not prove the game is fun] → the console hot-seat mode exists for human playtesting; simulation only guards pacing and balance.
- [Advance rule can pull a slider into a bad square unexpectedly] → the attack preview states the attacker's end square before the player commits.

## Open Questions

- Should non-king pieces also suffer fatigue late in a battle? Deferred until playtesting shows whether battles stall with kings hiding.
- Exact stat values: the table above is a starting point, to be tuned with the simulation.
