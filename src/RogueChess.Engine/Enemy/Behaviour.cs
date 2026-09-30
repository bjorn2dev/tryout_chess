using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine.Enemy
{
    /// <summary>One step of a behaviour: picks an action when it applies, otherwise returns null.</summary>
    public abstract class BehaviourRule
    {
        public abstract string Name { get; }

        /// <param name="actions">The legal actions of the side to act, in the fixed tie-break order.</param>
        public abstract LegalAction TryChoose(Battle battle, IReadOnlyList<LegalAction> actions);
    }

    public sealed class EnemyChoice
    {
        public BattleAction Action { get; }
        /// <summary>Null for a move.</summary>
        public AttackPreview Preview { get; }
        /// <summary>Name of the rule that decided the action.</summary>
        public string RuleName { get; }

        public EnemyChoice(LegalAction legal, string ruleName)
        {
            Action = legal.Action;
            Preview = legal.Preview;
            RuleName = ruleName;
        }
    }

    /// <summary>
    /// An ordered list of rules; the first rule that applies decides the action.
    /// Choices depend only on the position, so the same position always gives the same action.
    /// </summary>
    public sealed class Behaviour
    {
        public string Name { get; }
        public IReadOnlyList<BehaviourRule> Rules { get; }

        public Behaviour(string name, params BehaviourRule[] rules)
        {
            if (rules.Length == 0 || !(rules[rules.Length - 1] is Enemy.Rules.Fallback))
                throw new ArgumentException("A behaviour must end with the fallback rule.");
            Name = name;
            Rules = rules.ToList();
        }

        /// <summary>The action for the side to act, or null when it has no legal action.</summary>
        public EnemyChoice Choose(Battle battle)
        {
            var actions = ActionOrder.Sort(battle.State, battle.GetLegalActions());
            if (actions.Count == 0) return null;

            foreach (var rule in Rules)
            {
                var chosen = rule.TryChoose(battle, actions);
                if (chosen != null) return new EnemyChoice(chosen, rule.Name);
            }
            return null;
        }

        /// <summary>
        /// The reply this behaviour will give to the given action of the side to act, without
        /// changing the battle. Null when there is no reply: the action ends the battle or
        /// leaves the enemy without a legal action.
        /// </summary>
        public EnemyChoice PredictReply(Battle battle, BattleAction playerAction)
        {
            var player = battle.State.SideToAct;
            var copy = battle.Clone();
            var result = copy.Apply(playerAction);
            if (!result.Accepted)
                throw new ArgumentException(result.RejectionReason);

            if (copy.State.Result != BattleResult.Ongoing || copy.State.SideToAct == player)
                return null;
            return Choose(copy);
        }
    }

    /// <summary>
    /// The fixed tie-break order: by origin square, then target square, each by rank then file.
    /// Ranks count from the acting side's own back rank, so a behaviour plays the same as either colour.
    /// </summary>
    public static class ActionOrder
    {
        public static List<LegalAction> Sort(BattleState state, IEnumerable<LegalAction> actions)
        {
            var side = state.SideToAct;
            int Rank(Coord c) => side == Side.White ? c.Rank : state.Height - 1 - c.Rank;

            return actions
                .OrderBy(a => Rank(a.Action.From)).ThenBy(a => a.Action.From.File)
                .ThenBy(a => Rank(a.Action.To)).ThenBy(a => a.Action.To.File)
                .ToList();
        }
    }
}
