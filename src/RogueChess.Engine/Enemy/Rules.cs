using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine.Enemy
{
    /// <summary>The rules behaviours are built from.</summary>
    public static class Rules
    {
        // First candidate with the highest score; candidates arrive in tie-break order.
        private static LegalAction Best<TScore>(IEnumerable<LegalAction> candidates, Func<LegalAction, TScore> score)
            where TScore : IComparable<TScore>
        {
            LegalAction best = null;
            TScore bestScore = default;
            foreach (var candidate in candidates)
            {
                var s = score(candidate);
                if (best == null || s.CompareTo(bestScore) > 0)
                {
                    best = candidate;
                    bestScore = s;
                }
            }
            return best;
        }

        private static IEnumerable<LegalAction> Attacks(IEnumerable<LegalAction> actions) =>
            actions.Where(a => a.Action.Kind == ActionKind.Attack);

        private static Piece EnemyKing(Battle battle) => battle.State.King(battle.State.SideToAct.Opponent());

        private static Piece Target(Battle battle, LegalAction attack) => battle.State.PieceAt(attack.Action.To);

        private static int Value(Piece piece) => piece.Definition.Hp + piece.Definition.Atk;

        private static int Distance(Coord a, Coord b) =>
            Math.Max(Math.Abs(a.File - b.File), Math.Abs(a.Rank - b.Rank));

        /// <summary>A lethal attack on the enemy king.</summary>
        public sealed class FinishKing : BehaviourRule
        {
            public override string Name => "finish king";

            public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions)
            {
                var king = EnemyKing(battle).Position;
                return Attacks(actions).FirstOrDefault(a => a.Action.To == king && a.Preview.IsLethal);
            }
        }

        /// <summary>The most damaging attack on the enemy king.</summary>
        public sealed class AttackKing : BehaviourRule
        {
            public override string Name => "attack king";

            public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions)
            {
                var king = EnemyKing(battle).Position;
                return Best(Attacks(actions).Where(a => a.Action.To == king), a => a.Preview.Damage);
            }
        }

        /// <summary>A lethal attack, on the most valuable target.</summary>
        public sealed class Kill : BehaviourRule
        {
            public override string Name => "kill";

            public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions) =>
                Best(Attacks(actions).Where(a => a.Preview.IsLethal), a => Value(Target(battle, a)));
        }

        /// <summary>The attack with the most damage; among equals, on the most valuable target.</summary>
        public sealed class Strike : BehaviourRule
        {
            public override string Name => "strike";

            public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions) =>
                Best(Attacks(actions), a => (a.Preview.Damage, Value(Target(battle, a))));
        }

        /// <summary>Moves the own king off an attacked square onto one no enemy piece attacks.</summary>
        public sealed class ProtectKing : BehaviourRule
        {
            public override string Name => "protect king";

            public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions)
            {
                var state = battle.State;
                var king = state.King(state.SideToAct);
                if (!IsAttacked(state, king.Side, king.Position)) return null;

                return actions.FirstOrDefault(a =>
                    a.Action.Kind == ActionKind.Move && a.Action.From == king.Position && IsSafeAfterMove(state, king, a.Action.To));
            }

            private static bool IsAttacked(BattleState state, Side side, Coord square) =>
                state.Pieces.Any(p => p.Side != side && Patterns.Attacks(state, p, square));

            // Checked on a copy with the king moved: stepping away can open a line onto the new square.
            private static bool IsSafeAfterMove(BattleState state, Piece king, Coord to)
            {
                var copy = state.Clone();
                copy.PieceAt(king.Position).Position = to;
                return !IsAttacked(copy, king.Side, to);
            }
        }

        /// <summary>The move that brings a piece closest to the enemy king by the largest step.</summary>
        public sealed class Advance : BehaviourRule
        {
            private readonly bool _includeKing;

            public Advance(bool includeKing = true)
            {
                _includeKing = includeKing;
            }

            public override string Name => "advance";

            public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions)
            {
                var target = EnemyKing(battle).Position;
                int Reduction(LegalAction a) => Distance(a.Action.From, target) - Distance(a.Action.To, target);

                var candidates = actions.Where(a =>
                    a.Action.Kind == ActionKind.Move
                    && (_includeKing || !battle.State.PieceAt(a.Action.From).Definition.IsKing)
                    && Reduction(a) > 0);
                return Best(candidates, a => (Reduction(a), -Distance(a.Action.To, target)));
            }
        }

        /// <summary>The first legal action in tie-break order; applies whenever any action is legal.</summary>
        public sealed class Fallback : BehaviourRule
        {
            public override string Name => "fallback";

            public override LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions) =>
                actions.FirstOrDefault();
        }
    }
}
