using System;
using System.Collections.Generic;

namespace RogueChess.Engine
{
    public enum ActionKind
    {
        Move,
        Attack
    }

    public readonly struct BattleAction : IEquatable<BattleAction>
    {
        public ActionKind Kind { get; }
        public Coord From { get; }
        /// <summary>Destination square for a move, the target's square for an attack.</summary>
        public Coord To { get; }

        public BattleAction(ActionKind kind, Coord from, Coord to)
        {
            Kind = kind;
            From = from;
            To = to;
        }

        public static BattleAction Move(Coord from, Coord to) => new BattleAction(ActionKind.Move, from, to);
        public static BattleAction Attack(Coord from, Coord target) => new BattleAction(ActionKind.Attack, from, target);

        public bool Equals(BattleAction other) => Kind == other.Kind && From == other.From && To == other.To;
        public override bool Equals(object obj) => obj is BattleAction other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397 ^ From.GetHashCode()) * 397 ^ To.GetHashCode();
        public override string ToString() => $"{From} {(Kind == ActionKind.Attack ? "x" : "-")} {To}";
    }

    /// <summary>What an attack will do, known before it is performed.</summary>
    public sealed class AttackPreview
    {
        public int Damage { get; }
        public IReadOnlyList<Coord> Supporters { get; }
        public bool IsLethal { get; }
        public Coord AttackerEnd { get; }

        public AttackPreview(int damage, IReadOnlyList<Coord> supporters, bool isLethal, Coord attackerEnd)
        {
            Damage = damage;
            Supporters = supporters;
            IsLethal = isLethal;
            AttackerEnd = attackerEnd;
        }
    }

    public sealed class LegalAction
    {
        public BattleAction Action { get; }
        /// <summary>Null for a move.</summary>
        public AttackPreview Preview { get; }

        public LegalAction(BattleAction action, AttackPreview preview)
        {
            Action = action;
            Preview = preview;
        }
    }

    public sealed class ActionResult
    {
        public bool Accepted { get; }
        public string RejectionReason { get; }
        public IReadOnlyList<BattleEvent> Events { get; }

        private ActionResult(bool accepted, string rejectionReason, IReadOnlyList<BattleEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
        }

        public static ActionResult Ok(IReadOnlyList<BattleEvent> events) => new ActionResult(true, null, events);
        public static ActionResult Rejected(string reason) => new ActionResult(false, reason, Array.Empty<BattleEvent>());
    }
}
