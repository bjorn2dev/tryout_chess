# Design

## Context

See proposal.md for motivation. The engine already gives everything a behaviour needs: `Battle.GetLegalActions()` returns every legal action with its attack preview, `Battle.Clone()` copies a battle, and `Patterns.Attacks` tells whether a piece attacks a square. The console app has a `greedy` bot (`Bots.cs`) that is close to a behaviour but breaks ties with a random number generator and lives outside the engine, so Unity could not use it.

## Goals / Non-Goals

**Goals:**
- An enemy whose reply the player can compute exactly, and which the UI can show before the player commits.
- New enemy personalities by composing existing rules as data, without new code.
- Behaviours live in the engine so the later Unity layer gets them unchanged.

**Non-Goals:**
- Strong play. The archetypes are meant to be readable and distinct, not optimal.
- Look-ahead of any depth; every rule looks only at the current position (protect king looks one move ahead for the king's own square only).
- Difficulty scaling, enemy armies that differ from the player's, multiple actions per turn.

## Decisions

### 1. Reactive rules instead of search or announced intents
The enemy decides on its own turn from the position in front of it, using fixed rules.

- *Why*: with deterministic rules the reply to every player move is known in advance, which turns each turn into a small puzzle with forced answers — the stated pillar. It is also the cheapest option: no search, no evaluation function to tune.
- *Alternatives*: minimax (opaque to the player, expensive to tune for HP-based rules); announced intents that the player can foil (with one action per turn most announced attacks can simply be dodged — worth revisiting for bosses with heavy, slow attacks).

### 2. A behaviour is data: an ordered list of rule instances
```
Behaviour "Hunter" = [ FinishKing, AttackKing, Advance(includeKing: true), Kill, Strike, Fallback ]
```
Each rule is a small object with a name and one method: given the battle and its legal actions, return an action or nothing. A behaviour walks its list and returns the first result together with the rule's name.

- *Why*: archetypes become a list that a designer can read and reorder; the rule name doubles as the explanation shown to the player ("Hunter: attack king").
- *Alternative*: a weighted score over all actions (rejected: the player cannot predict a weighted sum in their head).

### 3. One fixed tie-break everywhere
Candidates that a rule rates equal are ordered by origin square (rank, then file), then target square (rank, then file), and the first is taken. Ranks are compared from the acting side's own back rank, so a behaviour plays the same whether it is White or Black.

- *Why*: determinism is the whole point; a single shared ordering keeps rules from inventing their own.

### 4. Rule semantics
- *Value* of a target = definition HP + ATK (pawn 2, knight/bishop 3, rook/queen 5, king 7 with the default table). Computed from data, so custom pieces need nothing extra.
- *Distance* = Chebyshev distance (king steps).
- *Advance* requires a strict reduction in the moving piece's distance to the enemy king. Without that, a piece already next to the king would shuffle around it forever while the rest of the army stands still.
- *Protect king* tests safety on a copy of the state with the king moved, because the king stepping away can open a line.
- *Fallback* takes the first legal action in the tie-break order.

### 5. Reply prediction by cloning
`PredictReply(battle, playerAction)` clones the battle, applies the action on the clone, and asks the behaviour for its choice if the battle is still ongoing and it is the enemy's turn.

- *Why*: it cannot disagree with real play, because it runs the same code on the same position. Cloning a 6×6 battle is cheap enough to do for every legal player action when a UI wants to show all replies.
- If the enemy has no legal action after the player's move the engine passes automatically and the turn returns to the player; prediction then reports "no reply".

### 6. Code layout
```
src/RogueChess.Engine/Enemy/
  BehaviourRule        (abstract: Name, TryChoose)
  Rules                (FinishKing, AttackKing, Kill, Strike, ProtectKing, Advance, Fallback)
  Behaviour            (Name, rules; Choose -> EnemyChoice; PredictReply)
  EnemyChoice          (Action, Preview, RuleName)
  Archetypes           (Brute, Hunter, Warden)
```

### 7. Console
- `play --enemy brute|hunter|warden`: the player is White and moves first; after each accepted player action the enemy acts at once and its action is printed with the deciding rule.
- `peek <from> <to>`: prints the enemy's predicted reply to that action without playing it.
- `simulate --white/--black` additionally accept the archetype names.

### 8. Validation by simulation
Record in `docs/enemy-validation.md`, per archetype as Black on the starter scenario: result against the `random` bot and against the `greedy` bot (1000 battles each), and a round-robin between the archetypes. Expectations: every archetype wins at least 90% against `random`, and the three archetypes give clearly different numbers from each other. Archetype-vs-archetype battles are deterministic, so each pairing is a single battle per colour assignment.

## Risks / Trade-offs

- [Fully predictable enemies are easy to exploit once learned] → accepted for now; that is the puzzle. Variety later comes from army composition, new rule lists and boss mechanics, not from hiding information.
- [Archetypes may play too weakly against a human] → the simulation only shows they beat random play; human hot-seat testing against each archetype decides whether rules need reordering.
- [Hunter ignoring free kills may look foolish rather than characterful] → it is a rule-order change if so.
- [The enemy moves second, and the rule simulation showed a second-side edge of about 54%] → noted in the validation document; who moves first is a scenario setting, not part of this change.
- [Deterministic archetype-vs-archetype games give one data point per pairing] → acceptable; the pairing is there to show the archetypes differ, not to balance them.

## Open Questions

- Should the UI later show the reply for every legal player action at once, or only for the hovered one? A presentation decision for the Unity change; the engine supports both.
