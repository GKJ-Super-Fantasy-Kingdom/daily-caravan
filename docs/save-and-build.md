# Save and Build Reference

## Save Behavior

Daily Caravan stores the visiting kingdom and final decision under your mod manager profile's `BepInEx/config/DailyCaravan/` folder. Records are keyed by game profile, kingdom run, day and the loaded checkpoint. Keep this folder with the corresponding game saves when moving a run.

On a replay of the same dawn, the game draws from the visiting kingdom's current item pool using its seeded picker. An unchanged ordinary pool repeats its offer. Content mods can change the available choices. A pool with only one or two distinct registered items offers that many cards. An empty pool can be declined.

A recorded decline stays closed. A checkpoint that already contains a reward keeps it. When you replay an earlier dawn, Daily Caravan restores a standard recorded drop once. If the item is unavailable or another plugin controls card selection or item grants, the current picker handles the choice again. Commission deliveries use Caravan Commissions' own callback and save rules.

On reload, unresolved daily offers follow the current `FirstDay`, gate and progression settings. Native bookings use the game's schedule. A recorded invitation can still arrive before `FirstDay`. Older unresolved records lack a visit origin and need an eligible dawn, a native booking or a new invitation. Completed selections and declines survive a schedule change.

The game awards its caravan faith bonus when the picker first opens. Daily Caravan records the resource type and amount separately from the card decision, including when you decline. Loading an earlier dawn restores the recorded bonus once. Older records created before bonus tracking began have no amount to restore.

Content mods that grant a direct effect from a native caravan card can call `DailyCaravanPlugin.TryGrantProviderReward(card, apply)` during selection. Supply the active `UICardItem` and a synchronous `Func<bool>` that returns true after applying the effect or false if it made no change. Daily Caravan invokes it only for an available caravan choice. The provider owns saving the effect in game data. A checkpoint containing the successful choice stays closed, and replaying a pre-grant dawn lets the provider apply a choice again. Zero-drop providers need this integration to distinguish successful effects from vetoes.

## Build and Verify

The project targets .NET Standard 2.1. The defaults read the installed Steam game and the BepInEx libraries from the Daybook Dev profile. Override either path with `-p:GameDir=...` or `-p:ProfileDir=...`. Building writes to this project.

```powershell
dotnet build DailyCaravan.csproj -c Release
dotnet run --project tests/ScheduleChecks -c Release
python package.py
```

The local-import package is `dist/DailyCaravan-0.1.5.zip`.

## Gate Setting Test

On a copy of a Day 22 save with the gate closed, Continue opened on Day 23. With `AllowBeforeGiantGate = true` and `FirstDay = 16`, Days 23, 24, and 25 each brought one caravan and one item. With the default gate setting, no daily caravan arrived on Day 23.

## Contributing and License

Bug reports and pull requests are welcome. The contribution guide covers local testing and reporting. [Contribution guide](https://github.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/blob/main/CONTRIBUTING.md)

The original mod code is available under the MIT License. Super Fantasy Kingdom, its game assets, and third-party dependencies retain their respective licenses and ownership. [MIT License](https://github.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/blob/main/LICENSE)
