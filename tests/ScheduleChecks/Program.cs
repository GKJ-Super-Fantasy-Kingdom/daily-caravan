using System;
using DailyCaravan;

static void Check(bool condition, string scenario)
{
    if (!condition) throw new Exception(scenario);
    Console.WriteLine("PASS " + scenario);
}

Check(!Visit.AddsVisit(15, 16, true, true, false), "first day boundary");
Check(Visit.AddsVisit(16, 16, true, true, false), "ordinary eligible dawn");
Check(!Visit.AddsVisit(20, 1, false, true, false), "locked gate");
Check(!Visit.AddsVisit(2, 1, true, false, false), "locked caravan progression");
Check(!Visit.AddsVisit(20, 1, true, true, true), "challenge receives only native bookings");

var visit = new Visit();
var arrival = new Arrival();
arrival.Request(visit);
Check(arrival.Take(visit, busy: true) == ArrivalAction.Wait, "Hermit retains the active guest slot");
arrival.Request(visit, kingdom: 2);
arrival.Request(visit, kingdom: 5);
Check(visit.kingdom == 2, "first invitation supplies the pending visit");
Check(arrival.Take(visit, busy: false) == ArrivalAction.StartVisit, "one arrival after the Hermit leaves");
arrival.Request(visit, kingdom: 6);
Check(arrival.Take(visit, false) == ArrivalAction.Wait, "subsequent invitation cannot reopen today's visit");

foreach (var (saved, expected) in new[] {
    (new Visit { kingdom = 2, items = new[] { "a", "b", "c" } }, ArrivalAction.StartVisit),
    (new Visit { kingdom = 2, declined = true }, ArrivalAction.Complete),
    (new Visit { kingdom = 2, selected = "a" }, ArrivalAction.RestoreItem),
    (new Visit { kingdom = 2, selected = "a", rewardInSave = true }, ArrivalAction.Complete)
})
{
    arrival = new Arrival();
    arrival.Request(saved, kingdom: 7);
    Check(saved.kingdom == 2 && arrival.Take(saved, false) == expected, "reload " + expected);
    arrival.Request(saved);
    Check(arrival.Take(saved, false) == ArrivalAction.Wait, "reload cannot dispatch twice");
}

for (int home = 0; home < 9; home++)
    for (int day = 1; day <= 29; day++)
    {
        int race = Visit.VisitingKingdom(9876, day, home);
        if (race < 0 || race > 8 || race == home || race != Visit.VisitingKingdom(9876, day, home))
            throw new Exception("invalid or unstable visiting kingdom");
    }
Console.WriteLine("PASS deterministic foreign kingdom across 261 kingdom/day pairs");
