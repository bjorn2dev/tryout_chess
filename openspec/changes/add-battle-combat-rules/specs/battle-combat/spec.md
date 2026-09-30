# Spec Delta

## Purpose

Defines the rules of a single battle between two armies of chess-like pieces with hit points and attack values: how turns, movement, attacks, damage, victory and battle termination work, and what the rules engine reports to its callers.

## ADDED Requirements

### Requirement: Battle setup
A battle SHALL be created from a configuration that gives the board width and height, the placement of each side's pieces, each piece's hit points (HP), attack value (ATK), move pattern and capture pattern, and the rule parameters (support bonus per piece, support bonus cap, fatigue start round, fatigue damage). Each side MUST have exactly one king. The first side to act SHALL be the side named first in the configuration.

#### Scenario: Valid configuration
- **WHEN** a battle is created on a 6×6 board with one king per side
- **THEN** the battle starts in round 1 with the first side to act and every piece at its configured HP

#### Scenario: Missing king
- **WHEN** a battle is created with a side that has no king or more than one king
- **THEN** creation is rejected with an error and no battle exists

#### Scenario: Piece outside the board
- **WHEN** a configuration places a piece outside the board or two pieces on the same square
- **THEN** creation is rejected with an error

### Requirement: One action per turn
On its turn a side SHALL perform exactly one action: move one of its pieces or attack with one of its pieces. After a valid action the turn SHALL pass to the other side. A round SHALL consist of one turn by each side. A side with no legal action SHALL pass automatically.

#### Scenario: Turn passes after an action
- **WHEN** the side to act performs a valid move or attack
- **THEN** the other side becomes the side to act

#### Scenario: Invalid action is rejected
- **WHEN** a side submits an action that is not in its list of legal actions, or acts out of turn
- **THEN** the action is rejected, the battle state is unchanged and the side to act does not change

#### Scenario: No legal action
- **WHEN** the side to act has no legal move and no legal attack
- **THEN** its turn is passed and the other side becomes the side to act

### Requirement: Movement
A piece SHALL move according to its move pattern to an empty square on the board. Sliding pieces MUST NOT pass through occupied squares; jumping pieces ignore intervening pieces. A move SHALL NOT be restricted by whether it exposes the mover's own king to attack. Pawns SHALL move one square straight forward for their side. Castling, en passant, the double pawn step and promotion SHALL NOT exist.

#### Scenario: Slider blocked
- **WHEN** a rook has a piece two squares away on its file
- **THEN** the rook can move one square along that file and cannot move to or beyond the occupied square

#### Scenario: King may step into danger
- **WHEN** a king moves to a square that an enemy piece attacks
- **THEN** the move is legal

#### Scenario: Pawn on the far edge
- **WHEN** a pawn stands on the last rank in its forward direction
- **THEN** the pawn has no legal move and is not promoted

### Requirement: Attack targeting
A piece SHALL be able to attack an enemy piece that stands on a square its capture pattern reaches, with a clear path for sliding pieces. A piece MUST NOT attack a friendly piece or an empty square. Pawns SHALL attack one square diagonally forward.

#### Scenario: Attack within capture pattern
- **WHEN** a bishop has an enemy piece on one of its diagonals with no piece in between
- **THEN** attacking that piece is a legal action

#### Scenario: Blocked line of attack
- **WHEN** another piece stands between a bishop and an enemy piece on its diagonal
- **THEN** attacking that enemy piece is not a legal action

### Requirement: Attack damage with support bonus
An attack SHALL deal damage equal to the attacker's ATK plus a support bonus. The support bonus SHALL be the configured bonus per piece multiplied by the number of supporters, limited to the configured cap. A supporter is any other friendly piece whose capture pattern reaches the target's square in the position before the attack. Damage SHALL be deterministic; the battle rules MUST NOT use randomness. The target SHALL NOT retaliate.

#### Scenario: Unsupported attack
- **WHEN** a piece with ATK 1 attacks a target that no other friendly piece attacks
- **THEN** the target loses 1 HP and the attacker loses no HP

#### Scenario: Supported attack
- **WHEN** a piece with ATK 1 attacks a target that one other friendly piece also attacks, with a bonus per piece of 1
- **THEN** the target loses 2 HP

#### Scenario: Support bonus is capped
- **WHEN** a piece with ATK 1 attacks a target that three other friendly pieces also attack, with a bonus per piece of 1 and a cap of 2
- **THEN** the target loses 3 HP

#### Scenario: Blocked supporter does not count
- **WHEN** a friendly rook is on the target's file but another piece stands between them
- **THEN** that rook is not counted as a supporter

### Requirement: Lethal attack captures
When an attack reduces the target's HP to 0 or below, the target SHALL be removed from the board and the attacker SHALL move onto the target's square.

#### Scenario: Lethal hit
- **WHEN** a rook with ATK 2 attacks a pawn with 1 HP three squares away
- **THEN** the pawn is removed and the rook stands on the pawn's former square

### Requirement: Non-lethal attack advances the attacker
When the target survives an attack, the target SHALL stay on its square with reduced HP. An attacker that attacked along a line SHALL end on the square of that line adjacent to the target. An attacker that jumps, or that was already adjacent to the target, SHALL stay on its square.

#### Scenario: Slider advances to contact
- **WHEN** a rook with ATK 2 on a1 attacks a king with 6 HP on a5 and the target survives
- **THEN** the king stays on a5 with 4 HP and the rook stands on a4

#### Scenario: Jumper stays
- **WHEN** a knight attacks a target that survives
- **THEN** the knight stays on the square it attacked from

#### Scenario: Adjacent attacker stays
- **WHEN** a pawn attacks a diagonally adjacent target that survives
- **THEN** the pawn stays on its square

### Requirement: Victory by defeating the king
The battle SHALL end immediately when a king's HP reaches 0 or below, and the other side SHALL win. No action is possible after the battle has ended. Check, checkmate and stalemate SHALL NOT exist as rules.

#### Scenario: King defeated by an attack
- **WHEN** an attack reduces the enemy king's HP to 0
- **THEN** the battle ends, the attacking side is the winner and the end reason is "king defeated"

#### Scenario: Action after the end
- **WHEN** a side submits an action after the battle has ended
- **THEN** the action is rejected and the battle state is unchanged

### Requirement: Fatigue ends stalled battles
From the configured fatigue start round onward, at the start of every round both kings SHALL lose the configured fatigue damage at the same moment. If exactly one king reaches 0 HP the other side SHALL win with end reason "fatigue". If both kings reach 0 HP in the same fatigue step the battle SHALL end as a draw.

#### Scenario: No fatigue before the start round
- **WHEN** a round begins before the fatigue start round
- **THEN** no king loses HP from fatigue

#### Scenario: Fatigue decides the battle
- **WHEN** a fatigue step reduces one king to 0 HP while the other king has HP left
- **THEN** the battle ends and the side whose king survives wins with end reason "fatigue"

#### Scenario: Both kings fall
- **WHEN** a fatigue step reduces both kings to 0 HP
- **THEN** the battle ends as a draw

### Requirement: Legal actions and attack preview
The rules engine SHALL provide the complete list of legal actions for the side to act. For every legal attack it SHALL state the damage it will deal, the supporters counted, whether the hit is lethal, and the square the attacker will end on. The previewed outcome MUST equal the outcome of performing that action.

#### Scenario: Preview matches the result
- **WHEN** a caller performs an attack that was previewed as 3 damage, non-lethal, attacker ending on d4
- **THEN** the target loses exactly 3 HP, survives, and the attacker stands on d4

#### Scenario: Lethal flag
- **WHEN** an attack's damage is equal to or greater than the target's current HP
- **THEN** its preview is marked lethal and gives the target's square as the attacker's end square

### Requirement: Action events
Every applied action SHALL return an ordered list of events describing what happened, covering at least: piece moved, piece attacked (with damage and supporters), piece damaged (with remaining HP), piece defeated, fatigue applied, turn passed, and battle ended (with winner and reason). Callers MUST be able to reconstruct the resulting position from the previous position and the events alone.

#### Scenario: Events of a lethal attack
- **WHEN** a lethal attack is applied
- **THEN** the events are, in order: piece attacked, piece damaged, piece defeated, piece moved onto the target's square

#### Scenario: Events of a winning attack
- **WHEN** an attack defeats the enemy king
- **THEN** the last event is battle ended, naming the winner and the reason
