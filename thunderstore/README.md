Choose one item from a visiting kingdom’s caravan on each eligible morning. By default, the daily wagon can arrive from day 16 onward, once you have opened the giant gate and unlocked caravans.

## Features

- Pick one item from the visiting kingdom’s stock. The native merchant cards show each item’s effect and keywords before you choose.
- Scheduled wagons, invitations, and the daily caravan share one visit per day. If the Hermit or another guest arrives first, finish that interaction to let the wagon through.
- Replaying the same dawn remembers a selection or decline. A checkpoint that already contains your reward keeps it.

## How to Use

1. Open the giant gate on the world map. On the Human map, the day-29 visitor reveals the giant key. Gate progress carries over to future runs in that kingdom. Caravan progression also requires a kingdom best of at least day 14 or a current run on day 15 or later.
2. On an eligible morning, advance the wagon’s dialogue to open its cards. Look for the **Caravan** panel. The day-5 Merchant is a separate visitor who sells resources after you help them.
3. Use the D-pad to focus a card and read its keywords. Confirm with the bottom face button (A on an Xbox controller), press **Enter** on a focused card, or click the card with the mouse. The chosen item appears as a draggable drop.
4. Choose **Minimize** where available to come back to the offer. Declining spends the day’s choice.

To change the first daily visit, set `[Schedule] FirstDay` in `BepInEx/config/sfk.dailycaravan.cfg` after the first modded launch, then restart the game. An earlier setting still requires both unlocks. Native bookings and invitations can arrive before your configured day.

## Screenshots

![Day 17 caravan at 1× UI scale offering Rage, Hammer, and Burn, with Rage focused](https://raw.githubusercontent.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/main/docs/screenshots/day17-1x.png)

**1× UI scale.** The Demon kingdom offers three items. Focusing Rage opens its keyword description on the right.

![Day 17 caravan at 1.5× UI scale with Burn focused and its keyword panel above the rightmost card](https://raw.githubusercontent.com/GKJ-Super-Fantasy-Kingdom/daily-caravan/main/docs/screenshots/day17-1.5x.png)

**1.5× UI scale.** The same offer has Burn focused. Its keyword panel sits above the card. Both captures are from Super Fantasy Kingdom 0.8.068 with simulated D-pad input. The “Compare commissions” button comes from Caravan Commissions.

## Compatibility

- Standard and Forester’s Charter use the same unlock rules. Challenges keep their authored caravan dates, weekly bookings, and invitations.
- Daily Caravan works on its own. Weekly Hermit can share a dawn, Visitor Calendar can display visitor portraits, and Caravan Commissions handles its own orders and deliveries.
- The wagon draws from the visiting kingdom’s item pool. If another mod changes the stock before a reload, the available cards can change. A pool with fewer than three distinct registered items shows fewer choices.

## Installation

In Thunderstore Mod Manager, select your **Super Fantasy Kingdom** profile and install **Daily Caravan** with its listed dependencies, BepInExPack and HideManagerGameObject_Config. Choose **Start modded** and continue an eligible kingdom.
