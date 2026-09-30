using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueChess.Engine.Enemy
{
    public static class Archetypes
    {
        /// <summary>Hits whatever it can hurt most, otherwise walks forward.</summary>
        public static readonly Behaviour Brute = new Behaviour("Brute",
            new Rules.FinishKing(), new Rules.Kill(), new Rules.Strike(), new Rules.Advance(), new Rules.Fallback());

        /// <summary>Goes for the king and ignores other pieces unless nothing else is possible.</summary>
        public static readonly Behaviour Hunter = new Behaviour("Hunter",
            new Rules.FinishKing(), new Rules.AttackKing(), new Rules.Advance(), new Rules.Kill(), new Rules.Strike(),
            new Rules.Fallback());

        /// <summary>Keeps its king out of danger first and never walks its king forward.</summary>
        public static readonly Behaviour Warden = new Behaviour("Warden",
            new Rules.FinishKing(), new Rules.ProtectKing(), new Rules.Kill(), new Rules.Strike(),
            new Rules.Advance(includeKing: false), new Rules.Fallback());

        public static readonly IReadOnlyList<Behaviour> All = new[] { Brute, Hunter, Warden };

        /// <summary>Finds an archetype by name, ignoring case; null when there is none.</summary>
        public static Behaviour ByName(string name) =>
            All.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
