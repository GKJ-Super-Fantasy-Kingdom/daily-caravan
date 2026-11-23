namespace DailyCaravan
{
    internal enum ArrivalAction { Wait, Complete, RestoreItem, StartVisit }

    internal sealed class Arrival
    {
        private bool pending;
        internal bool Dispatched { get; private set; }
        internal bool Pending => pending;

        internal void Request(Visit visit, int? kingdom = null)
        {
            if (Dispatched) return;
            pending = true;
            if (kingdom.HasValue && visit.kingdom < 0) visit.kingdom = kingdom.Value;
        }

        internal ArrivalAction Take(Visit visit, bool busy)
        {
            if (!pending || busy) return ArrivalAction.Wait;
            pending = false;
            Dispatched = true;
            if (visit.declined || visit.rewardInSave) return ArrivalAction.Complete;
            return visit.Finished ? ArrivalAction.RestoreItem : ArrivalAction.StartVisit;
        }
    }
}
