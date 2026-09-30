# Enemy validation

Simulation of the three enemy archetypes on the 6×6 starter scenario with the default rules.
Reproduce a row with `dotnet run --project src/RogueChess.ConsoleApp -- simulate --white <player> --black <player>`.

Expectations (design.md): every archetype wins at least 90% against the `random` bot, and the three archetypes give clearly different results from each other.

## Archetype as Black against the bots

1000 battles each, seed 1. White moves first.

| White  | Black (enemy) | Enemy wins | White wins | Draws | Avg rounds | Ended by fatigue |
|--------|---------------|------------|------------|-------|------------|------------------|
| random | Brute         | 99.9%      | 0.1%       | 0.0%  | 17.0       | 7.1%             |
| random | Hunter        | 97.1%      | 2.7%       | 0.2%  | 18.1       | 4.3%             |
| random | Warden        | 100.0%     | 0.0%       | 0.0%  | 17.8       | 10.7%            |
| greedy | Brute         | 62.3%      | 37.6%      | 0.1%  | 14.6       | 0.1%             |
| greedy | Hunter        | 6.4%       | 93.6%      | 0.0%  | 13.6       | 0.0%             |
| greedy | Warden        | 82.5%      | 12.7%      | 4.8%  | 19.4       | 20.3%            |

## Archetype against archetype

Both sides are deterministic, so each pairing is one battle.

| White  | Black  | Winner | Rounds | End reason    |
|--------|--------|--------|--------|---------------|
| Brute  | Brute  | Black  | 8      | king defeated |
| Brute  | Hunter | White  | 12     | king defeated |
| Brute  | Warden | Black  | 12     | king defeated |
| Hunter | Brute  | White  | 18     | king defeated |
| Hunter | Hunter | White  | 17     | king defeated |
| Hunter | Warden | Black  | 18     | king defeated |
| Warden | Brute  | White  | 10     | king defeated |
| Warden | Hunter | White  | 12     | king defeated |
| Warden | Warden | White  | 28     | fatigue       |

## Against the expectations

- **At least 90% against random: met.** Brute 99.9%, Hunter 97.1%, Warden 100%.
- **Clearly different from each other: met.** Against the greedy bot the enemy wins 62% as Brute, 6% as Hunter and 83% as Warden, and the Warden's battles are about five rounds longer.

## Open issues

- **Hunter is weak.** It wins only 6% against the greedy bot. Walking past free kills costs it its army before it reaches the king. If it should be a real threat rather than an easy encounter, the rule order needs changing (for example kill before advance when the target attacks one of its own pieces).
- **Warden slows battles down.** A fifth of its battles against the greedy bot end by fatigue and the average is 19.4 rounds, close to the upper pacing target of 20. Warden against Warden never reaches a king at all and is decided by fatigue in round 28.
- **Brute is the strongest and also wins the mirror as the second side in 8 rounds.** This matches the second-side edge seen in `rule-validation.md`; in play against a human the enemy is always the second side.
- **These numbers say nothing about a human opponent.** A player who uses `peek` knows every reply in advance; whether the archetypes are then a puzzle or a pushover needs hot-seat play against each of them.
