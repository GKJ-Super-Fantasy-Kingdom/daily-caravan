namespace DailyCaravan
{
    internal enum ArrivalAction { Wait, Complete, RestoreItem, StartVisit }

    internal sealed class Arrival
    {
        private bool pending;
        internal bool Dispatched { get; private set; }
        internal bool Pending => pending;

        internal void Request(Visit visit, int? kingdom = null, VisitOrigin origin = VisitOrigin.Unknown)
        {
            if (Dispatched) return;
            pending = true;
            if (visit.Finished) return;
            if (kingdom.HasValue && (visit.kingdom < 0 ||
                (origin == VisitOrigin.Invitation && (visit.origin == VisitOrigin.Daily || visit.origin == VisitOrigin.Booking))))
                visit.kingdom = kingdom.Value;
            if (origin > visit.origin) visit.origin = origin;
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
