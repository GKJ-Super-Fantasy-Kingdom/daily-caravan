using System;
using System.Collections.Generic;
using System.Linq;
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

var config = @"C:\Users\user\AppData\Roaming\Thunderstore Mod Manager\DataFolder\SuperFantasyKingdom\profiles\GKJ - Controller Test\BepInEx\config";
var runHash = new string('a', 64);
var dawnHash = new string('b', 64);
var paths = new RecordPaths(config, 0, runHash, 16);
Check((paths.Legacy(dawnHash) + ".tmp").Length == 288 && (paths.For(dawnHash) + ".tmp").Length < 260,
    "atomic writes fit the reported Windows profile path");
Check(new[] { paths.For(dawnHash), paths.For("new"), new RecordPaths(config, 1, runHash, 16).For(dawnHash),
    new RecordPaths(config, 0, runHash, 17).For(dawnHash), new RecordPaths(config, 0, dawnHash, 16).For(dawnHash) }.Distinct().Count() == 5,
    "compact records distinguish profiles, runs, days and checkpoints");

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
    (new Visit { kingdom = 2 }, ArrivalAction.StartVisit),
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

var available = new HashSet<string> { "Shield", "ThirdParty.Charm", "Coinbag" };
var weighted = new List<string> { "Shield", "ThirdParty.Charm", "Shield", "Coinbag" };
var first = OfferPool.Registered(weighted, available.Contains, out int distinct);
Check(first.SequenceEqual(weighted) && distinct == 3, "custom IDs retain the provider's order and weights");
available.Remove("Shield");
weighted.Add("RelicOnly");
weighted.Add(null);
var next = OfferPool.Registered(weighted, available.Contains, out distinct);
Check(next.SequenceEqual(new[]{"ThirdParty.Charm", "Coinbag"}) && distinct == 2,
    "a replay draws from today's registered items after a pool change");
Check(weighted.Count == 6 && weighted[0] == "Shield", "validating a draw preserves the provider's list");
Check(OfferPool.Registered(null, available.Contains, out distinct).Count == 0 && distinct == 0,
    "an unavailable kingdom pool has zero card slots");
var single = OfferPool.Registered(new List<string>{"Coinbag", "Coinbag"}, available.Contains, out distinct);
Check(single.Count == 2 && distinct == 1, "duplicate-only stock supplies one distinct card");
arrival = new Arrival();
visit = new Visit { kingdom = 42 };
arrival.Request(visit, kingdom: 2);
Check(visit.kingdom == 42 && arrival.Take(visit, false) == ArrivalAction.StartVisit,
    "a saved provider-defined kingdom survives another invitation");

visit = new Visit();
Check(!visit.RecordDrops("ThirdParty.Charm", 0, null, false) && !visit.Finished,
    "a vetoed or rejected card cannot create a replay reward");
Check(visit.RecordDrops("ThirdParty.Charm", 1, "ThirdParty.Charm", false) && !visit.providerGrant &&
      !visit.RecordDrops("Coinbag", 1, "Coinbag", false) && visit.selected == "ThirdParty.Charm",
    "only the first successful single drop owns the day");
visit = new Visit();
Check(visit.RecordDrops("Original", 1, "Replacement", false) && visit.selected == "Replacement" && visit.providerGrant,
    "transformed grants keep the actual ID and use provider replay");
visit = new Visit();
Check(visit.RecordDrops("Bundle", 2, null, false) && visit.providerGrant, "a multi-drop callback owns its replay");
visit = new Visit();
Check(visit.RecordDrops("Coinbag", 1, "Coinbag", true) && visit.providerGrant, "custom grant hooks own replay even for the same ID");

visit = new Visit();
arrival = new Arrival();
arrival.Request(visit, origin: VisitOrigin.Daily);
Check(!visit.ResumeWithoutBooking && !Visit.AddsVisit(17, 25, true, true, false),
    "unresolved daily offer waits for the new FirstDay on reload");
Check(!new Visit { kingdom = 2 }.ResumeWithoutBooking, "legacy pending record requires current eligibility");
arrival.Request(visit, origin: VisitOrigin.Booking);
Check(visit.origin == VisitOrigin.Booking, "a native booking also owns a shared daily slot");
arrival.Request(visit, 4, VisitOrigin.Invitation);
arrival.Request(visit, 6, VisitOrigin.Invitation);
arrival.Request(visit, origin: VisitOrigin.Daily);
Check(visit.origin == VisitOrigin.Invitation && visit.kingdom == 4 && visit.ResumeWithoutBooking,
    "the first invitation survives a shared dawn and a raised threshold");

int effects = 0;
Check(!visit.GrantProviderReward("Charm", () => false) && !visit.Finished, "provider veto leaves the card available");
Check(visit.GrantProviderReward("Charm", () => {
    Check(!visit.GrantProviderReward("Charm", () => true), "a reentrant provider callback cannot grant again");
    effects++;
    return true;
}) && !visit.GrantProviderReward("Charm", () => { effects++; return true; }) && effects == 1,
    "a successful direct effect consumes the visit exactly once");
var json = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
Visit Reload(Visit source) => System.Text.Json.JsonSerializer.Deserialize<Visit>(
    System.Text.Json.JsonSerializer.Serialize(source, json), json);
visit.rewardInSave = true;
var afterGrant = Reload(visit);
arrival = new Arrival();
arrival.Request(afterGrant);
Check(arrival.Take(afterGrant, false) == ArrivalAction.Complete &&
      !afterGrant.GrantProviderReward("Charm", () => { effects++; return true; }) && effects == 1,
    "same-day checkpoint containing a direct effect cannot apply it twice");

foreach (bool decline in new[] { false, true })
{
    visit = new Visit { kingdom = 2, declined = decline, selected = decline ? null : "Charm",
        bonus = new FaithBonus { amount = 7, resource = 3, x = 1, y = 2 } };
    int resources = 0;
    void Spawn(FaithBonus bonus) => resources += bonus.amount;
    visit.bonus.Deliver(Spawn);
    var earlierDawn = Reload(visit);
    resources = 0;
    Check(earlierDawn.ResumeWithoutBooking && earlierDawn.bonus.Deliver(Spawn) &&
          !earlierDawn.bonus.Deliver(Spawn) && resources == 7,
        "earlier dawn restores the recorded caravan bonus once after " + (decline ? "decline" : "selection"));
    var checkpoint = Reload(earlierDawn);
    checkpoint.bonus.inSave = earlierDawn.bonus.Present;
    Check(!Reload(checkpoint).bonus.Deliver(Spawn) && resources == 7,
        "a checkpoint containing the faith bonus cannot issue it again");
}
