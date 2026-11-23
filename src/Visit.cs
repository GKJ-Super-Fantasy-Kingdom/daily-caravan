using System;

namespace DailyCaravan
{
    [Serializable]
    public sealed class Visit
    {
        public int kingdom = -1;
        public string[] items;
        public string selected;
        public bool declined;
        public bool rewardInSave;

        public bool Finished => declined || !string.IsNullOrEmpty(selected);

        internal static bool AddsVisit(int day, int firstDay, bool gateOpen, bool caravansUnlocked, bool challenge)
        {
            return !challenge && gateOpen && caravansUnlocked && day >= firstDay;
        }

        internal static int VisitingKingdom(int seed, int day, int home)
        {
            // A separate RNG makes replays independent of other guests and their dialogue.
            var random = new Random(unchecked(seed * 397 ^ day));
            int kingdom = random.Next(8);
            return kingdom >= home && home >= 0 && home < 9 ? kingdom + 1 : kingdom;
        }
    }
}
