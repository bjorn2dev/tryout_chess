# Rule validation

Self-play on the 6×6 starter scenario, greedy bot against greedy bot, 1000 battles per run unless noted.
Reproduce any row with `dotnet run --project src/RogueChess.ConsoleApp -- simulate <options>`.

Targets (design.md): average 10–20 rounds, under 10% of battles ended by fatigue, first side wins 45–55%.

## Result with the current defaults

Support bonus 1 (cap 2), fatigue from round 25, king HP 6.

| Seed | Games | Avg rounds | Ended by fatigue | First side wins | Second side wins | Draws |
|------|-------|------------|------------------|-----------------|------------------|-------|
| 1    | 1000  | 15.6       | 0.6%             | 45.6%           | 54.0%            | 0.4%  |
| 2    | 1000  | 15.6       | 0.4%             | 43.5%           | 56.3%            | 0.2%  |
| 3    | 1000  | 15.5       | 0.1%             | 47.0%           | 52.9%            | 0.1%  |
| 4    | 10000 | 15.5       | 0.4%             | 46.2%           | 53.6%            | 0.2%  |

Battle length and fatigue endings meet their targets. The first-side win rate is inside the target band in the 10 000-game run (46.2%) but close to its lower edge, and single 1000-game runs fall on either side of 45%.

## Settings tried (seed 1, 1000 games)

The first row is the original design default. Only data was changed between rows.

| Setting                          | Avg rounds | Ended by fatigue | First side wins | Second side wins | Draws |
|----------------------------------|------------|------------------|-----------------|------------------|-------|
| fatigue 20 (original default)    | 15.2       | 10.2%            | 43.1%           | 54.8%            | 2.1%  |
| king HP 5                        | 14.5       | 8.2%             | 41.7%           | 55.3%            | 3.0%  |
| king HP 8                        | 17.3       | 23.0%            | 39.1%           | 54.8%            | 6.1%  |
| cap 1                            | 15.2       | 11.2%            | 42.5%           | 55.7%            | 1.8%  |
| cap 3                            | 15.2       | 10.2%            | 43.1%           | 54.8%            | 2.1%  |
| bonus 2, cap 4                   | 15.3       | 12.4%            | 44.1%           | 52.6%            | 3.3%  |
| **fatigue 25 (new default)**     | 15.6       | 0.6%             | 45.6%           | 54.0%            | 0.4%  |
| fatigue 30                       | 15.5       | 0.0%             | 46.2%           | 53.8%            | 0.0%  |
| king HP 5, fatigue 25            | 14.6       | 0.0%             | 43.3%           | 56.7%            | 0.0%  |
| king HP 8, fatigue 30            | 17.7       | 0.0%             | 46.5%           | 53.5%            | 0.0%  |

With fatigue starting in round 20 the fatigue and first-side targets were both missed. Moving the start to round 25 brings fatigue endings well under the target, because battles that ran past round 20 were being cut off by fatigue. The first-side win rate also rises, but by an amount that is within the noise of a 1000-game run. Round 25 was chosen over 30 because it still bounds a stalled battle sooner.

## What the support bonus contributes

| Setting (fatigue 25)       | Games | Avg rounds | Ended by fatigue | First side wins | Second side wins |
|----------------------------|-------|------------|------------------|-----------------|------------------|
| bonus 1, cap 2 (default)   | 1000  | 15.6       | 0.6%             | 45.6%           | 54.0%            |
| bonus 0                    | 1000  | 15.5       | 0.4%             | 48.8%           | 50.9%            |
| bonus 1, cap 2 (default)   | 10000 | 15.5       | 0.4%             | 46.2%           | 53.6%            |
| bonus 0                    | 10000 | 15.6       | 0.4%             | 49.2%           | 50.6%            |

In bot play the support bonus does not shorten battles, and it gives the second side about three points. The greedy bot never sets up support on purpose, so these runs cannot show whether the mechanic rewards coordination; that needs human play or a bot that plans for it.

## Open issues

- **Second-side edge.** The second side wins about 54% with the default rules. The edge mostly disappears with the support bonus off, which suggests the side that closes the distance first hands the other side the first supported hit. To watch in hot-seat play; possible data fixes are a lower cap or a scenario layout with more space between the armies.
- **Support bonus is unproven.** See above: no measurable effect on pacing with these bots.
- **Greedy bots are weak evidence.** They walk their king toward the enemy and never retreat. The numbers guard pacing and gross balance only, not whether the rules are fun.
