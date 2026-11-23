# Daily Caravan

Get one caravan item choice on eligible kingdom days in Super Fantasy Kingdom. The visiting wagon uses the game's merchant cards, descriptions, keyword panel, selection, and cancel button. Gamepad, keyboard, and mouse use the game's own controls.

## When the wagon arrives

- Extra visits start on day 16 by default. Change `[Schedule] FirstDay` in `BepInEx/config/sfk.dailycaravan.cfg` after the first modded launch. An extra visit requires the open gate and the game's caravan unlock.
- Normal scheduled visits and invitations still work before the configured first day. A weekly booking, a faith or token invitation, and the daily visit all share one slot. An invitation received while the wagon is waiting supplies its kingdom. Later invitations cannot start a second visit. The game still applies its normal token costs and resets.
- Challenges keep their authored caravan dates, weekly bookings and native invitations. Daily Caravan adds extra mornings only in ordinary kingdoms.
- When the Hermit or another guest is visiting, finish that interaction first. A minimized merchant screen keeps the caravan waiting. Once the guest has left and the UI is free, the wagon arrives.

## Local import

1. In Thunderstore Mod Manager, choose **Super Fantasy Kingdom** and a dedicated test profile. Install the two BepInEx dependencies listed in the manifest. Coordinate a shared-PC playtest before switching profiles or starting the game.
2. In profile **Settings**, find **Import local mod**, select `DailyCaravan-0.1.0.zip`, and complete the import. Use **Start modded**. The BepInEx log should include `Daily Caravan 0.1.0 ready.`.
3. Continue a kingdom that has opened the gate and unlocked caravans. To try an earlier eligible day, change `FirstDay` while the game is closed and relaunch during the test window.

## Controller playtest

1. On an eligible dawn, wait for the wagon and advance its dialogue with the displayed action. Use only the controller to navigate all three cards. Check the focused card's name, description and keywords before confirming anything.
2. Where the game offers **Minimize**, minimize and reopen the merchant. Check that focus is usable and the three choices are unchanged. Navigate to the displayed cancel option and activate it. Verify that no item drops and a later invitation that day produces no second choice.
3. On another eligible day, select one card. Verify that exactly one matching draggable item appears and control returns to the game. Repeat the selection or cancel flow with mouse and keyboard on another day.
4. Test a native booking plus an invitation on one day, and a challenge with the Hermit and caravan scheduled together. Finish or decline the Hermit and check that the caravan follows. On an unscheduled challenge day, check that there is no extra visit.
5. At supported save points, use **Continue** before and after resolving a visit. Check the visiting kingdom and cards before a choice, the single selected item after a choice, and the absence of a reward after a decline. Verify that the next eligible day gets its own offer.

## Save behavior

Daily Caravan stores the visiting kingdom, offered item IDs and final decision under the test profile's `BepInEx/config/DailyCaravan/` folder. Records are keyed by game profile, kingdom run, day and the loaded checkpoint. Keep this folder with the corresponding game saves when moving a run.

On a replay of the same dawn, an unresolved visit restores its choices. A recorded decline stays closed. If the loaded checkpoint predates a recorded selection, the mod restores that item as a single loose drop. A checkpoint recorded with the reward already present skips the drop. The game controls equipped items, other resources and save timing.

## Build and verify

The project targets .NET Standard 2.1. The defaults read the installed Steam game and the BepInEx libraries from the Daybook Dev profile. Override either path with `-p:GameDir=...` or `-p:ProfileDir=...`. Building writes to this project.

```powershell
dotnet build DailyCaravan.csproj -c Release
dotnet run --project tests/ScheduleChecks -c Release
python package.py
```

The local-import package is `dist/DailyCaravan-0.1.0.zip`.

## Contributing and license

Bug reports and pull requests are welcome. See the [contribution guide](CONTRIBUTING.md) for local testing and reporting instructions.

The original mod code is available under the [MIT License](LICENSE). Super Fantasy Kingdom, its game assets, and third-party dependencies retain their respective licenses and ownership.
