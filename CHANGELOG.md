# Changelog

## 0.1.4

Clarify the mod’s description and compatibility for players.

## 0.1.3

- Daily visits in ordinary kingdoms start on Day 16 by default once the giant gate is open. Set `AllowBeforeGiantGate = true` in the Schedule section to allow visits before opening the gate. The normal caravan progression check still applies.
- Visitor Calendar 0.1.2 forecasts these visits without filling in missed days.

## 0.1.2

- Shortened checkpoint filenames so caravan decisions save in long Windows mod-manager profile paths. Existing readable records still load.
- Clarified how invitations work before the giant gate opens.

## 0.1.1

- Added the game's caravan portrait beside the title of ordinary item offers.
- Set initial controller focus to the first offered item so its description is available when the wagon opens.
- Kept longer keyword descriptions clear of the rightmost card and its kingdom badge at 1.5Ã— UI scale.

## 0.1.0

- Added a configurable daily caravan for unlocked kingdoms and a shared limit for scheduled and invited visits.
- Kept challenge guest schedules and queued arrivals behind active guests, dialogue and overlays.
- Added checkpoint records for replaying offers, selections and declines through the native game UI.
- Updated local compatibility handling for changing item pools, short offers, dawn skips, and provider-controlled grants.
- Kept native caravan keyword explanations clear of item cards when space is available below the HUD.
- Rechecked unresolved daily visits after schedule changes, tracked provider-confirmed direct rewards, and preserved the native faith bonus across dawn replays and checkpoints.
- Included the contribution guide in the local-import package.
