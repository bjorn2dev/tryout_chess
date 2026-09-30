# Proposal

## Why

The roguelike plan (`ROGUELIKE_GAME_PLAN.md` §2.2) names "reach-strike" as its most important unconfirmed design choice, and it was never worked out. As sketched it has known problems: sliding pieces become risk-free archers, surviving pieces clog the board, fights drag on, and there is nothing that forces a battle to end. Everything else in the game (AI, army draft, relics, run loop) sits on top of this rule set, so it has to be pinned down and proven playable before any Unity work starts.

## What Changes

- Define the battle rule set, **advance-strike**, replacing the "reach-strike" sketch:
  - One action per turn: move **or** attack. No check, checkmate, stalemate, castling, en passant, double pawn step or promotion.
  - An attack uses the piece's capture pattern and deals `ATK + support bonus`.
  - **Support bonus**: every other friendly piece that also attacks the target square adds damage. Chess's "count the attackers on a square" becomes the damage formula, so coordinated attacks finish pieces quickly and beating the king feels like building a mating net.
  - **Lethal hit**: the target is removed and the attacker takes its square (classic capture).
  - **Non-lethal hit**: the attacker *advances* along its line of attack to the square next to the target. Sliders must commit and end up exposed; jumpers (knight) and already-adjacent attackers stay put.
  - Win by reducing the enemy king to 0 HP.
  - **Fatigue**: from a configured round on, both kings lose HP every round, so every battle terminates.
  - No randomness and no retaliation.
- The engine exposes, for every legal attack, a preview (damage, supporters, lethal or not, where the attacker ends) and returns an ordered event list for every applied action.
- Deliver the rules as a Unity-free C# library with tests, a console harness for hot-seat playtesting, and a self-play simulation that reports battle length and first-player win rate against the design targets.

Out of scope: enemy AI, Unity presentation, terrain and irregular boards, status effects, abilities, custom pieces, relics, and anything that persists between battles.

## Capabilities

### New Capabilities
- `battle-combat`: the rules of a single battle — turn structure, movement, attack damage with support bonus, advance-strike resolution, win condition, fatigue, action preview and action events.

### Modified Capabilities

None. The project has no specs yet.

## Impact

- This repository is empty apart from the README, so all code is new: an engine library, a test project and a console harness.
- The existing `ChessAPI` engine is a reference only; none of its code is changed or copied. Its index-based move math assumes an 8×8 board and cannot serve variable board sizes.
- The engine must stay free of `UnityEngine` and compile for Unity later (`netstandard2.1`), so it can be dropped into a Unity assembly unchanged.
- Supersedes §2.2 and open question 1 of `ROGUELIKE_GAME_PLAN.md`; M2 of that roadmap should build on this rule set.
