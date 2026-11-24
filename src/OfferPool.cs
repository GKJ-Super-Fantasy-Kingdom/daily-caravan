using System;
using System.Collections.Generic;

namespace DailyCaravan
{
    internal static class OfferPool
    {
        // Keep order and multiplicity: providers may express weights by repeating IDs.
        internal static List<string> Registered(List<string> pool, Func<string, bool> resolves, out int distinct)
        {
            var valid = new List<string>();
            var known = new Dictionary<string, bool>(StringComparer.Ordinal);
            distinct = 0;
            if (pool == null) return valid;
            foreach (var id in pool)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!known.TryGetValue(id, out bool present))
                {
                    known.Add(id, present = resolves(id));
                    if (present) distinct++;
                }
                if (present) valid.Add(id);
            }
            return valid;
        }
    }
}
