# Tasks

## 1. Solution scaffold

- [x] 1.1 Create the solution with `src/RogueChess.Engine` (netstandard2.1, C# 9, no UnityEngine or DI packages), `src/RogueChess.ConsoleApp` and `tests/RogueChess.Engine.Tests` (NUnit); verify `dotnet build` and `dotnet test` succeed
- [x] 1.2 Add a .NET `.gitignore` and verify `git status` shows no `bin/` or `obj/` files after a build

## 2. Board, pieces and setup

- [x] 2.1 Implement coordinates, sides, and piece definitions with move and capture patterns as direction vectors with range and slide/jump flag; verify with tests that each of the six standard definitions yields the expected reachable squares on an empty 6×6 board
- [x] 2.2 Implement the battle configuration and battle state (board size, placements, HP/ATK, rule parameters) with validation; verify with tests for the "Battle setup" scenarios (valid, missing or duplicate king, piece off-board, shared square)
- [x] 2.3 Add the default stat table and the 6×6 starter scenario from design.md as data; verify with a test that the starter scenario creates a valid battle

## 3. Movement and turns

- [x] 3.1 Implement legal move generation (blocked sliders, jumping knight, forward-only pawn, no king-safety filter, no special moves); verify with tests for the "Movement" scenarios
- [x] 3.2 Implement applying an action with turn and round tracking, rejection of illegal or out-of-turn actions, and automatic pass; verify with tests for the "One action per turn" scenarios

## 4. Attack resolution

- [x] 4.1 Implement attack targeting from capture patterns; verify with tests for the "Attack targeting" scenarios
- [x] 4.2 Implement damage with support bonus and cap, counted in the pre-attack position; verify with tests for the four "Attack damage with support bonus" scenarios, including bonus per piece set to 0
- [x] 4.3 Implement lethal capture and non-lethal advance (slider to adjacent square, jumper and adjacent attacker stay); verify with tests for the "Lethal attack captures" and "Non-lethal attack advances the attacker" scenarios

## 5. Ending a battle

- [x] 5.1 Implement victory on king defeat and rejection of actions after the end; verify with tests for the "Victory by defeating the king" scenarios
- [x] 5.2 Implement fatigue at round start with win, and draw on double defeat; verify with tests for the "Fatigue ends stalled battles" scenarios

## 6. Preview and events

- [x] 6.1 Add attack previews (damage, supporters, lethal flag, attacker end square) to the legal action list; verify with a test that, for every legal attack in several positions, applying it produces exactly the previewed outcome
- [x] 6.2 Return ordered events from every applied action; verify with tests for the "Action events" scenarios and a test that replays events onto a copy of the previous position and gets the same resulting position

## 7. Console harness

- [x] 7.1 Implement ASCII hot-seat play of the starter scenario: board with HP per piece, legal actions with previews, input of an action, printed events; verify by playing a scripted input file to a finished battle and checking the printed winner
- [x] 7.2 Implement the `simulate` command with greedy and random bots and a seed option, printing average rounds, share ended by fatigue and first-side win rate; verify that two runs with the same seed print identical numbers
- [x] 7.3 Document in the README how to build, test, play and simulate, plus a one-page summary of the rules; verify each documented command runs as written

## 8. Rule validation

- [x] 8.1 Run 1000 greedy-vs-greedy battles on the starter scenario and compare with the targets in design.md (10–20 rounds average, under 10% fatigue endings, 45–55% first-side win rate); record the numbers in `docs/rule-validation.md`
- [x] 8.2 If a target is missed, adjust only data (stats, bonus, cap, fatigue) and rerun; record each tried setting and its numbers in `docs/rule-validation.md`, and list any target that tuning could not reach as an open issue there
- [x] 8.3 Run the simulation with the support bonus set to 0 and record the comparison in `docs/rule-validation.md`, to show what the mechanic contributes
