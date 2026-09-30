# Tasks

## 1. Behaviour framework

- [x] 1.1 Add the rule base type, the choice result (action, preview, rule name) and the behaviour that tries its rules in order, with the shared tie-break ordering from design.md; verify with tests using stub rules that the earlier rule wins and the choice names it
- [x] 1.2 Add the fallback rule; verify with tests that it chooses a legal action when no other rule applies, that the choice names the fallback, and that asking twice in the same position gives the same action

## 2. Rules

- [x] 2.1 Implement finish king, attack king, kill and strike; verify with tests for the "Attack rules" scenarios, including target value and the fixed tie-break between equal attacks
- [x] 2.2 Implement protect king, testing safety on the position after the king's move; verify with tests for the "King safety rule" scenarios, including a case where the king stepping away opens a line onto its new square
- [x] 2.3 Implement advance with strict distance reduction and the option to exclude the king; verify with tests for the "Advance rule" scenarios

## 3. Archetypes and prediction

- [x] 3.1 Define the Brute, Hunter and Warden archetypes; verify with tests for the four "Starting archetypes" scenarios
- [x] 3.2 Implement reply prediction on a cloned battle; verify with tests for the "Reply prediction" scenarios, including a test that plays full battles and checks at every player turn that the predicted reply equals the enemy's actual action and that predicting leaves the battle unchanged
- [x] 3.3 Verify with a test that every archetype, playing either colour from the starter scenario against scripted legal opposition, only ever returns actions the battle accepts and returns the same action when asked twice

## 4. Console

- [x] 4.1 Add `play --enemy <archetype>`: the enemy acts after each accepted player action and its action is printed with the deciding rule; verify by playing a scripted input file against each archetype to a finished battle
- [x] 4.2 Add the `peek <from> <to>` command that prints the predicted reply without playing; verify in a scripted run that the line printed by `peek` matches the enemy action printed after playing the same action
- [x] 4.3 Accept archetype names for `--white` and `--black` in `simulate`; verify that an archetype-vs-archetype run prints the same result for different seeds
- [x] 4.4 Update the README with the enemy option, the `peek` command and a short description of each archetype's rule order; verify each documented command runs as written

## 5. Validation

- [x] 5.1 Run each archetype as Black against the `random` and `greedy` bots (1000 battles each) and the archetype round-robin, and record the numbers in `docs/enemy-validation.md`
- [x] 5.2 Compare the numbers with the expectations in design.md (at least 90% wins against `random`, clearly different results between archetypes) and record in `docs/enemy-validation.md` which are met and any that are not as open issues
