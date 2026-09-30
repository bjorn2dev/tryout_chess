using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine
{
    /// <summary>The advance-strike rules for a single battle.</summary>
    public sealed class Battle
    {
        private readonly BattleRules _rules;
        private readonly Side _firstToAct;

        public BattleState State { get; }

        public Battle(BattleConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();

            _rules = config.Rules;
            _firstToAct = config.FirstToAct;
            State = new BattleState(config.Width, config.Height) { SideToAct = config.FirstToAct };
            int id = 0;
            foreach (var placement in config.Placements)
                State.Pieces.Add(new Piece(id++, placement.Definition, placement.Side, placement.Position, placement.Definition.Hp));

            if (LegalActionsFor(State.SideToAct).Count == 0)
                EndTurn(new List<BattleEvent>());
        }

        private Battle(BattleState state, BattleRules rules, Side firstToAct)
        {
            State = state;
            _rules = rules;
            _firstToAct = firstToAct;
        }

        public Battle Clone() => new Battle(State.Clone(), _rules, _firstToAct);

        /// <summary>All legal actions for the side to act; empty once the battle has ended.</summary>
        public IReadOnlyList<LegalAction> GetLegalActions() =>
            State.Result == BattleResult.Ongoing ? LegalActionsFor(State.SideToAct) : new List<LegalAction>();

        public ActionResult Apply(BattleAction action)
        {
            if (State.Result != BattleResult.Ongoing)
                return ActionResult.Rejected("The battle has ended.");

            var legal = LegalActionsFor(State.SideToAct).FirstOrDefault(a => a.Action.Equals(action));
            if (legal == null)
                return ActionResult.Rejected($"{action} is not a legal action for {State.SideToAct}.");

            var events = new List<BattleEvent>();
            var actor = State.PieceAt(action.From);
            if (action.Kind == ActionKind.Move)
                MovePiece(actor, action.To, events);
            else
                ResolveAttack(actor, State.PieceAt(action.To), legal.Preview, events);

            if (State.Result == BattleResult.Ongoing)
                EndTurn(events);
            return ActionResult.Ok(events);
        }

        private List<LegalAction> LegalActionsFor(Side side)
        {
            var actions = new List<LegalAction>();
            foreach (var piece in State.Pieces.Where(p => p.Side == side))
            {
                foreach (var square in Patterns.MoveSquares(State, piece))
                    actions.Add(new LegalAction(BattleAction.Move(piece.Position, square), null));

                foreach (var reach in Patterns.AttackTargets(State, piece))
                    actions.Add(new LegalAction(BattleAction.Attack(piece.Position, reach.Target.Position), Preview(piece, reach)));
            }
            return actions;
        }

        private AttackPreview Preview(Piece attacker, AttackReach reach)
        {
            var target = reach.Target;
            var supporters = State.Pieces
                .Where(p => p.Side == attacker.Side && p != attacker && Patterns.Attacks(State, p, target.Position))
                .Select(p => p.Position)
                .ToList();
            int bonus = Math.Min(_rules.SupportBonusCap, _rules.SupportBonusPerPiece * supporters.Count);
            int damage = attacker.Definition.Atk + bonus;
            bool lethal = damage >= target.Hp;
            return new AttackPreview(damage, supporters, lethal, lethal ? target.Position : reach.EndIfTargetSurvives);
        }

        private static void MovePiece(Piece piece, Coord to, List<BattleEvent> events)
        {
            var from = piece.Position;
            piece.Position = to;
            events.Add(new PieceMoved(new PieceRef(piece), from, to));
        }

        private void ResolveAttack(Piece attacker, Piece target, AttackPreview preview, List<BattleEvent> events)
        {
            events.Add(new PieceAttacked(new PieceRef(attacker), new PieceRef(target),
                attacker.Position, target.Position, preview.Damage, preview.Supporters));
            Damage(target, preview.Damage, events);

            if (preview.IsLethal)
                Defeat(target, events);
            if (preview.AttackerEnd != attacker.Position)
                MovePiece(attacker, preview.AttackerEnd, events);

            if (preview.IsLethal && target.Definition.IsKing)
                End(attacker.Side == Side.White ? BattleResult.WhiteWins : BattleResult.BlackWins, EndReason.KingDefeated, events);
        }

        private static void Damage(Piece piece, int amount, List<BattleEvent> events)
        {
            piece.Hp = Math.Max(0, piece.Hp - amount);
            events.Add(new PieceDamaged(new PieceRef(piece), amount, piece.Hp));
        }

        private void Defeat(Piece piece, List<BattleEvent> events)
        {
            State.Pieces.Remove(piece);
            events.Add(new PieceDefeated(new PieceRef(piece), piece.Position));
        }

        private void End(BattleResult result, EndReason reason, List<BattleEvent> events)
        {
            State.Result = result;
            State.EndReason = reason;
            events.Add(new BattleEnded(result, reason));
        }

        // Hands the turn over, starting a new round (with fatigue) when the first side is up again.
        // Sides without a legal action pass; fatigue guarantees this loop ends.
        private void EndTurn(List<BattleEvent> events)
        {
            while (true)
            {
                State.SideToAct = State.SideToAct.Opponent();
                if (State.SideToAct == _firstToAct)
                {
                    State.Round++;
                    if (State.Round >= _rules.FatigueStartRound)
                    {
                        ApplyFatigue(events);
                        if (State.Result != BattleResult.Ongoing) return;
                    }
                }

                if (LegalActionsFor(State.SideToAct).Count > 0) return;
                events.Add(new TurnPassed(State.SideToAct));
            }
        }

        private void ApplyFatigue(List<BattleEvent> events)
        {
            events.Add(new FatigueApplied(State.Round, _rules.FatigueDamage));
            var kings = new[] { State.King(Side.White), State.King(Side.Black) };
            foreach (var king in kings)
                Damage(king, _rules.FatigueDamage, events);

            var fallen = kings.Where(k => k.Hp <= 0).ToList();
            foreach (var king in fallen)
                Defeat(king, events);

            if (fallen.Count == 2)
                End(BattleResult.Draw, EndReason.Fatigue, events);
            else if (fallen.Count == 1)
                End(fallen[0].Side == Side.White ? BattleResult.BlackWins : BattleResult.WhiteWins, EndReason.Fatigue, events);
        }
    }
}
