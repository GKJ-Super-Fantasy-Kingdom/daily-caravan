using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DailyCaravan
{
    // AddItem creates the native offer. Prepaid deliveries construct their own cards.
    internal static class CaravanOfferPresentation
    {
        private sealed class Card
        {
            internal string Item;
        }

        private static readonly ConditionalWeakTable<UICardItem, Card> Cards =
            new ConditionalWeakTable<UICardItem, Card>();
        private static readonly ConditionalWeakTable<UIOverlayMerchant, Card> LastFocus =
            new ConditionalWeakTable<UIOverlayMerchant, Card>();
        private static readonly AccessTools.FieldRef<UICardItem, Image> Icon =
            AccessTools.FieldRefAccess<UICardItem, Image>("icon");
        private static readonly AccessTools.FieldRef<UICardItem, string> ItemId =
            AccessTools.FieldRefAccess<UICardItem, string>("m_ItemIdentifier");
        private static readonly AccessTools.FieldRef<OverlayManager, UIOverlayBase> CurrentOverlay =
            AccessTools.FieldRefAccess<OverlayManager, UIOverlayBase>("m_CurrentlyOpen");
        private static readonly AccessTools.FieldRef<OverlayManager, UIOverlayBase> BackupOverlay =
            AccessTools.FieldRefAccess<OverlayManager, UIOverlayBase>("m_Backup");

        [HarmonyPatch(typeof(UIOverlayMerchant), nameof(UIOverlayMerchant.AddItem))]
        private static class RewardIcon
        {
            private static void Postfix(UIOverlayMerchant __instance, GameObject __result)
            {
                if (!CaravanPatches.OwnsNativeOffer(__instance) || !__result) return;
                var card = __result.GetComponent<UICardItem>();
                if (!card) return;
                Cards.Add(card, new Card { Item = ItemId(card) });
                CaravanCardEdge.Attach(card);
                var image = Icon(card);
                if (!image || !image.sprite) return;
                image.preserveAspect = true;
                var art = image.rectTransform;
                art.localScale *= 1.5f;
                art.anchoredPosition += Vector2.up * 4f;
            }
        }

        [HarmonyPatch(typeof(UICardItem), nameof(UICardItem.OnEnter))]
        private static class Hover
        {
            private static void Prefix(UICardItem __instance)
            {
                if (!Cards.TryGetValue(__instance, out _) || GamepadManager.Instance.IsGamepadInUse() ||
                    !CaravanPatches.CanGrant(__instance, out _)) return;
                var events = EventSystem.current;
                var selected = events?.currentSelectedGameObject;
                var previous = selected ? selected.GetComponentInParent<UICardItem>() : null;
                if (previous && Cards.TryGetValue(previous, out _) && !events.alreadySelecting &&
                    previous.GetComponentInParent<UIOverlayMerchant>() == __instance.GetComponentInParent<UIOverlayMerchant>())
                    events.SetSelectedGameObject(null);
            }
        }

        [HarmonyPatch(typeof(UICardItem), nameof(UICardItem.OnSelect), new System.Type[0])]
        private static class RememberFocus
        {
            private static void Postfix(UICardItem __instance)
            {
                if (!Cards.TryGetValue(__instance, out var card) || !GamepadManager.Instance.IsGamepadInUse() ||
                    EventSystem.current?.currentSelectedGameObject != __instance.GetCardButton().gameObject) return;
                var screen = __instance.GetComponentInParent<UIOverlayMerchant>();
                if (!CaravanPatches.OwnsNativeOffer(screen)) return;
                LastFocus.Remove(screen);
                LastFocus.Add(screen, card);
            }
        }

        [HarmonyPatch(typeof(UIOverlayMerchant), nameof(UIOverlayMerchant.Init))]
        private static class Reset
        {
            private static void Prefix(UIOverlayMerchant __instance) => LastFocus.Remove(__instance);
        }

        internal static void OnMinimize()
        {
            var overlays = OverlayManager.Instance;
            if (!overlays || !(BackupOverlay(overlays) is UIOverlayMerchant screen) ||
                !CaravanPatches.OwnsNativeOffer(screen) || !LastFocus.TryGetValue(screen, out var focus)) return;
            DailyCaravanPlugin.Instance.StartCoroutine(RestoreAfterReopen(overlays, screen, focus.Item));
        }

        private static IEnumerator RestoreAfterReopen(OverlayManager overlays, UIOverlayMerchant screen, string item)
        {
            // The backup is the original merchant, reparented under the hidden
            // overlay. Only restore focus if the game reopens that same merchant.
            while (overlays && screen && overlays.HasMinimizedOverlay() && BackupOverlay(overlays) == screen)
                yield return null;
            yield return null;

            var events = EventSystem.current;
            var gamepad = GamepadManager.Instance;
            var fallback = events?.currentSelectedGameObject;
            float deadline = Time.realtimeSinceStartup + 3f;
            // The overlay is current before its reveal activates the merchant.
            // Wait for the regenerated item button itself to become actionable.
            while (Time.realtimeSinceStartup < deadline && overlays && screen &&
                CurrentOverlay(overlays) == screen && gamepad && gamepad.IsGamepadInUse() && events &&
                events.currentSelectedGameObject == fallback && CaravanPatches.OwnsNativeOffer(screen))
            {
                var card = screen.GetComponentsInChildren<UICardItem>()
                    .FirstOrDefault(candidate => CaravanPatches.CanGrant(candidate, out var availableItem) &&
                        availableItem == item && candidate.GetCardButton().isActiveAndEnabled &&
                        candidate.GetCardButton().IsInteractable());
                if (card)
                {
                    gamepad.SetGameObjectToSelect(card.GetCardButton().gameObject, force: true);
                    yield break;
                }
                yield return null;
            }
        }
    }
}
