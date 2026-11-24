using System;

namespace DailyCaravan
{
    public enum VisitOrigin { Unknown, Daily, Booking, Invitation }

    [Serializable]
    public sealed class FaithBonus
    {
        public int amount, resource;
        public float x, y, z;
        public bool onScreen, inSave;
        [NonSerialized] private bool delivered;

        internal bool Present => inSave || delivered;
        internal bool NeedsRestore => amount > 0 && !Present;

        internal bool Deliver(Action<FaithBonus> spawn)
        {
            if (!NeedsRestore) return false;
            delivered = true;
            try { spawn(this); }
            catch { delivered = false; throw; }
            return true;
        }
    }

    [Serializable]
    public sealed class Visit
    {
        public int kingdom = -1;
        // Older 0.1.0 records and consumers can still read this field. New draws use the active game.
        public string[] items;
        public bool providerGrant;
        public string selected;
        public bool declined;
        public bool rewardInSave;
        public VisitOrigin origin;
        public FaithBonus bonus;
        private bool granting;

        public bool Finished => declined || !string.IsNullOrEmpty(selected);
        internal bool CanSelect => !Finished && !granting;
        internal bool ResumeWithoutBooking => Finished || rewardInSave || origin == VisitOrigin.Invitation;

        internal bool GrantProviderReward(string card, Func<bool> grant)
        {
            if (!CanSelect || string.IsNullOrEmpty(card) || grant == null) return false;
            granting = true;
            try
            {
                if (!grant()) return false;
                selected = card;
                providerGrant = true;
                return true;
            }
            finally { granting = false; }
        }

        internal bool RecordDrops(string card, int added, string singleDrop, bool grantHook)
        {
            if (!CanSelect || added <= 0) return false;
            selected = added == 1 ? singleDrop : card;
            providerGrant = grantHook || added != 1 || selected != card;
            return true;
        }

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
