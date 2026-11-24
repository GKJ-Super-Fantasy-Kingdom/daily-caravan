# Daily Caravan

Get one caravan item choice on eligible kingdom days in Super Fantasy Kingdom. The visiting wagon uses the game's merchant cards, descriptions, keyword panel, selection, and cancel button. Gamepad, keyboard, and mouse use the game's own controls.

**Day 17 at 1× UI scale.** A caravan from the Demon kingdom offers Rage, Hammer, and Burn. Rage has focus and its keyword description appears on the right.

![Day 17 caravan at 1× UI scale with Rage focused](https://raw.githubusercontent.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/main/docs/screenshots/day17-1x.png)

**The same visit at 1.5×.** Focus has moved to Burn. Its keyword panel sits above the rightmost card. These in-game captures are from game 0.8.068 using simulated D-pad input. The “Compare commissions” button comes from the optional Caravan Commissions mod.

![Day 17 caravan at 1.5× UI scale with Burn focused](https://raw.githubusercontent.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/main/docs/screenshots/day17-1.5x.png)

## When the wagon arrives

- Day 16 is the default **earliest** daily visit. At dawn in an ordinary run, the wagon can arrive once the configured first day has been reached, the giant gate is open, and the game has unlocked caravans. The native progression check passes if the kingdom's best day is at least 14 or the current day is 15 or later. Opening the gate after the morning check makes the next eligible dawn the first supplemental visit.
- The day-5 **Merchant** sells resources after you have helped that visitor. The **Caravan** offers an item. On the Human map, the day-29 visitor's dialogue reveals the giant key. Collect it and open the giant gate on the world map. This unlock carries over to future runs in that kingdom. The city wall gate is a separate building upgrade. Forester’s Charter and Standard use the same caravan rules.
- Change `[Schedule] FirstDay` in `BepInEx/config/sfk.dailycaravan.cfg` after the first modded launch. Setting an earlier day keeps both native unlock requirements. The native calendar has a fixed caravan entry on day 16 when the gate is open. Visitor Calendar manages its own additional portraits. To verify an arrival, look for the wagon and its merchant cards.
- Normal scheduled visits and invitations still work before the configured first day. A weekly booking, a faith or token invitation, and the daily visit all share one slot. An invitation received while the wagon is waiting supplies its kingdom. Later invitations cannot start a second visit. The game still applies its normal token costs and resets.
- Challenges keep their authored caravan dates, weekly bookings and native invitations. Daily Caravan adds extra mornings only in ordinary kingdoms.
- When the Hermit or another guest is visiting, finish that interaction first. A minimized merchant screen keeps the caravan waiting. Once the guest has left and the UI is free, the wagon arrives.

## Installation

In Thunderstore Mod Manager, select your **Super Fantasy Kingdom** profile, install **Daily Caravan** and its listed dependencies, then choose **Start modded**.

For a local ZIP, install the two BepInEx dependencies listed in the manifest. In profile **Settings**, choose **Import local mod** and select `DailyCaravan-0.1.0.zip`. The BepInEx log records `Daily Caravan 0.1.0 ready.` when the mod loads.

## Choosing an item

Advance the wagon's dialogue with the displayed action. Use the D-pad or keyboard navigation to focus a card and read its description and keywords. Confirm the card or click it with the mouse to receive the item. Where **Minimize** is available, you can reopen the same offer before deciding. Declining the visit spends that day's choice.

## Save behavior

Daily Caravan stores the visiting kingdom and final decision under your mod manager profile's `BepInEx/config/DailyCaravan/` folder. Records are keyed by game profile, kingdom run, day and the loaded checkpoint. Keep this folder with the corresponding game saves when moving a run.

On a replay of the same dawn, the game draws from the visiting kingdom's current item pool using its seeded picker. An unchanged ordinary pool repeats its offer. Content mods can change the available choices. A pool with only one or two distinct registered items offers that many cards. An empty pool can be declined.

A recorded decline stays closed. A checkpoint that already contains a reward keeps it. When you replay an earlier dawn, Daily Caravan restores a standard recorded drop once. If the item is unavailable or another plugin controls card selection or item grants, the current picker handles the choice again. Commission deliveries use Caravan Commissions' own callback and save rules.

On reload, unresolved daily offers follow the current `FirstDay`, gate and progression settings. Native bookings use the game's schedule. A recorded invitation can still arrive before `FirstDay`. Older unresolved records lack a visit origin and need an eligible dawn, a native booking or a new invitation. Completed selections and declines survive a schedule change.

The game awards its caravan faith bonus when the picker first opens. Daily Caravan records the resource type and amount separately from the card decision, including when you decline. Loading an earlier dawn restores the recorded bonus once. Older records created before bonus tracking began have no amount to restore.

Content mods that grant a direct effect from a native caravan card can call `DailyCaravanPlugin.TryGrantProviderReward(card, apply)` during selection. Supply the active `UICardItem` and a synchronous `Func<bool>` that returns true after applying the effect or false if it made no change. Daily Caravan invokes it only for an available caravan choice. The provider owns saving the effect in game data. A checkpoint containing the successful choice stays closed, and replaying a pre-grant dawn lets the provider apply a choice again. Zero-drop providers need this integration to distinguish successful effects from vetoes.

## Build and verify

The project targets .NET Standard 2.1. The defaults read the installed Steam game and the BepInEx libraries from the Daybook Dev profile. Override either path with `-p:GameDir=...` or `-p:ProfileDir=...`. Building writes to this project.

```powershell
dotnet build DailyCaravan.csproj -c Release
dotnet run --project tests/ScheduleChecks -c Release
python package.py
```

The local-import package is `dist/DailyCaravan-0.1.0.zip`.

## Contributing and license

Bug reports and pull requests are welcome. The contribution guide covers local testing and reporting. [Contribution guide](https://github.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/blob/main/CONTRIBUTING.md)

The original mod code is available under the MIT License. Super Fantasy Kingdom, its game assets, and third-party dependencies retain their respective licenses and ownership. [MIT License](https://github.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/blob/main/LICENSE)
