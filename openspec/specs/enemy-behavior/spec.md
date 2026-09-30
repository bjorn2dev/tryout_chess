# enemy-behavior Specification

## Purpose
Defines how a computer-controlled side chooses its action in a battle: through rule-based behaviours that are deterministic and readable, so a player can predict the enemy's reply to any move.

## Requirements

### Requirement: Deterministic choice among legal actions
A behaviour SHALL choose exactly one action for the side to act whenever that side has at least one legal action, and the chosen action MUST be one of the battle's legal actions. The choice SHALL depend only on the battle position: the same position MUST always produce the same action. Behaviours MUST NOT use randomness. When several actions are equally good under a rule, the tie SHALL be broken by a fixed ordering of the actions' squares.

#### Scenario: Chosen action is legal
- **WHEN** a behaviour chooses an action in any position where its side has a legal action
- **THEN** the battle accepts that action

#### Scenario: Same position, same action
- **WHEN** a behaviour is asked twice for its action in the same position
- **THEN** it returns the same action both times

#### Scenario: Equal options
- **WHEN** two attacks are equally good under the deciding rule
- **THEN** the same one of the two is chosen every time that position occurs

### Requirement: Behaviour is an ordered list of rules
A behaviour SHALL consist of an ordered list of rules. The rules SHALL be tried in order and the first rule that applies in the position SHALL decide the action. Every choice SHALL report the name of the rule that decided it. Every behaviour MUST end with a fallback that applies whenever a legal action exists.

#### Scenario: Earlier rule wins
- **WHEN** both an earlier and a later rule of a behaviour apply in a position
- **THEN** the action comes from the earlier rule and the choice names that rule

#### Scenario: Fallback
- **WHEN** no other rule of a behaviour applies but a legal action exists
- **THEN** the fallback chooses a legal action and the choice names the fallback

### Requirement: Attack rules
The following attack rules SHALL be available, each applying only when a matching attack is legal:
- **Finish king**: a lethal attack on the enemy king.
- **Attack king**: an attack on the enemy king; the one with the highest damage.
- **Kill**: a lethal attack; the one on the target with the highest value, where value is the target's starting HP plus its ATK.
- **Strike**: any attack; the one with the highest damage, and among equal damage the target with the highest value.

#### Scenario: Finish king
- **WHEN** a lethal attack on the enemy king and a lethal attack on a rook are both legal
- **THEN** the finish king rule chooses the attack on the king

#### Scenario: Kill prefers the more valuable target
- **WHEN** a lethal attack on a pawn and a lethal attack on a knight are both legal
- **THEN** the kill rule chooses the attack on the knight

#### Scenario: Strike prefers more damage
- **WHEN** one legal attack deals 3 damage and another deals 1
- **THEN** the strike rule chooses the attack that deals 3

#### Scenario: No matching attack
- **WHEN** no attack is legal
- **THEN** none of the attack rules applies

### Requirement: King safety rule
A **protect king** rule SHALL be available. It SHALL apply when an enemy piece attacks the square of the behaviour's own king and the king has a move to a square that no enemy piece attacks after the move. It SHALL choose such a move.

#### Scenario: King steps out of an attack
- **WHEN** an enemy rook attacks the own king along a file and the king can step to a square no enemy piece attacks
- **THEN** the protect king rule moves the king to such a square

#### Scenario: King is not under attack
- **WHEN** no enemy piece attacks the own king's square
- **THEN** the protect king rule does not apply

#### Scenario: No safe square
- **WHEN** the own king is attacked and every square it can move to is also attacked
- **THEN** the protect king rule does not apply

### Requirement: Advance rule
An **advance** rule SHALL be available. It SHALL apply when a move exists that brings the moving piece closer to the enemy king, and SHALL choose the move with the largest reduction in distance, preferring among equals the move that ends closest to the enemy king. Distance is the number of king steps between two squares. The rule SHALL be configurable to exclude moves by the own king.

#### Scenario: Largest step toward the king
- **WHEN** a rook can move four squares closer to the enemy king and a pawn can move one square closer
- **THEN** the advance rule chooses the rook move

#### Scenario: No move gets closer
- **WHEN** no legal move reduces a piece's distance to the enemy king
- **THEN** the advance rule does not apply

#### Scenario: King excluded
- **WHEN** the advance rule excludes the own king and only a king move would get closer to the enemy king
- **THEN** the advance rule does not apply

### Requirement: Starting archetypes
Three archetypes SHALL be available, each a behaviour with this rule order:
- **Brute**: finish king, kill, strike, advance, fallback.
- **Hunter**: finish king, attack king, advance, kill, strike, fallback.
- **Warden**: finish king, protect king, kill, strike, advance without the king, fallback.

#### Scenario: Brute takes the kill
- **WHEN** a brute can either kill a knight or make a non-lethal attack on the enemy king
- **THEN** it kills the knight

#### Scenario: Hunter goes for the king
- **WHEN** a hunter can either kill a knight or make a non-lethal attack on the enemy king
- **THEN** it attacks the king

#### Scenario: Hunter walks past a free kill
- **WHEN** a hunter cannot attack the enemy king, can kill a pawn, and has a move that brings a piece closer to the enemy king
- **THEN** it makes that move

#### Scenario: Warden saves its king first
- **WHEN** a warden's king is attacked, has a safe square to move to, and the warden could also kill a pawn
- **THEN** it moves its king to the safe square

### Requirement: Reply prediction
For any legal action of the player, the system SHALL report the action the enemy behaviour will choose in reply, together with the deciding rule and, for an attack, its preview. Predicting MUST NOT change the battle. The predicted reply MUST equal the enemy's actual reply when the player performs that action. When the player's action ends the battle or leaves the enemy without a legal action, the prediction SHALL state that there is no reply.

#### Scenario: Prediction matches what happens
- **WHEN** the reply to a player action is predicted and the player then performs that action
- **THEN** the enemy's action is the predicted one

#### Scenario: Prediction leaves the battle unchanged
- **WHEN** a reply is predicted
- **THEN** the position, the side to act and the round are the same as before

#### Scenario: No reply after a winning action
- **WHEN** the player's action defeats the enemy king
- **THEN** the prediction states that there is no reply
