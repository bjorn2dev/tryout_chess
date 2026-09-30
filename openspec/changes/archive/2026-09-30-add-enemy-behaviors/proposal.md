# Proposal

## Why

The battle rules can only be played hot-seat or watched between bots, so nobody can yet tell whether a battle against the computer is fun. The game's pillar is "every battle is a puzzle", which a search-based opponent would undermine: the player cannot plan against a black box. This change adds enemies that follow fixed, readable behaviour rules, so the player can work out the enemy's reply to any move before making it.

## What Changes

- Add **enemy behaviours**: a behaviour is an ordered list of simple rules; the first rule that applies decides the enemy's action. The choice is fully deterministic — the same position always gives the same action — and names the rule that produced it.
- Add a small set of rules to build behaviours from: finish the king, attack the king, kill a piece, strike for the most damage, move the king out of danger, advance toward the enemy king, and a fallback.
- Add three starting archetypes that play visibly differently with the same army:
  - **Brute** — hits whatever it can hurt most, otherwise walks forward.
  - **Hunter** — goes for the king and ignores other pieces unless nothing else is possible.
  - **Warden** — keeps its king out of danger first and never walks its king forward.
- Add **reply prediction**: for any action the player is considering, report what the enemy will answer, without changing the battle.
- Console: play the starter scenario against a chosen archetype, with a command to preview the enemy's reply; archetypes are also available as players in `simulate`.

Out of scope: search-based AI, announced intents the player can foil (kept as a later option for bosses), enemy-specific armies or "monster" pieces, more than one enemy action per turn, difficulty levels, and any change to the battle rules themselves.

## Capabilities

### New Capabilities
- `enemy-behavior`: how a computer-controlled side chooses its action from rule-based behaviours, the available rules and archetypes, and prediction of the enemy's reply to a player action.

### Modified Capabilities

None. Behaviours only choose among the legal actions the battle rules already provide; the `battle-combat` requirements (change `add-battle-combat-rules`, not yet archived) are untouched.

## Impact

- `src/RogueChess.Engine`: new behaviour code next to the rules, under the same constraints (no Unity reference, deterministic, data-driven).
- `src/RogueChess.ConsoleApp`: `play` gains an enemy option and a reply preview; `simulate` accepts archetypes as players. The existing random tie-breaking `greedy` bot stays as a measuring tool.
- `tests/RogueChess.Engine.Tests`: new tests.
- New `docs/enemy-validation.md` with simulation results per archetype.
- Depends on the engine from `add-battle-combat-rules`; no dependency changes.
