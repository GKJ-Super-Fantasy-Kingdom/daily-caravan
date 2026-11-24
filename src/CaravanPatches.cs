using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;

namespace DailyCaravan
{
    internal static class CaravanPatches
    {
        private static DailyCaravanPlugin Plugin => DailyCaravanPlugin.Instance;

        [HarmonyPatch(typeof(GameDataManager), nameof(GameDataManager.Load))]
        private static class Load
        {
            private static void Prefix(GameData ___m_GameData) => Plugin.BeginLoad(___m_GameData);
        }

        [HarmonyPatch(typeof(GameDataManager), "GenerateGameData")]
        private static class Save
        {
            private static void Postfix(GameData __result) => Plugin.Snapshot(__result);
        }

        // Some guests clear the slot when their card is chosen, then leave several frames later.
        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.SetGuest))]
        private static class ActiveGuest
        {
            private static void Postfix(Guest __0)
            {
                if (__0 != null) Plugin.LastGuest = __0;
            }
        }

        // CheckDay includes both the weekly guest booking and the authored challenge date.
        // Returning false takes OnLoad's normal inactive branch until the guest slot is free.
        [HarmonyPatch(typeof(GuestCaravan), "CheckDay")]
        private static class Scheduled
        {
            private static void Postfix(ref bool __result)
            {
                if (__result && DaytimeManager.Instance.IsLoading() &&
                    RaceDataManager.Instance.HasRaceEventFlag(RaceEvent.GateOpened) && Plugin.Request(VisitOrigin.Booking))
                    __result = false;
            }
        }

        // Intercept outside TriggerManually too: WorldManager assigns its guest slot before calling it.
        [HarmonyPatch(typeof(WorldManager), "TriggerCaravan")]
        private static class Tokens
        {
            [HarmonyPriority(Priority.Last)]
            private static bool Prefix(Races __0, bool __runOriginal) => __runOriginal && !Plugin.Request(VisitOrigin.Invitation, __0);
        }

        [HarmonyPatch(typeof(GuestCaravan), nameof(GuestCaravan.TriggerManually), typeof(Races))]
        private static class Invitation
        {
            [HarmonyPriority(Priority.Last)]
            private static bool Prefix(Races __0, bool __runOriginal) => __runOriginal && (Plugin.Launching || !Plugin.Request(VisitOrigin.Invitation, __0));
        }

        [HarmonyPatch(typeof(GuestCaravan), nameof(GuestCaravan.TriggerManually), new Type[0])]
        private static class DirectOpen
        {
            [HarmonyPriority(Priority.Last)]
            private static bool Prefix(GuestCaravan __instance, Races ___m_Kingdom, bool __runOriginal) =>
                __runOriginal && (Plugin.Owns(__instance) || !Plugin.Request(VisitOrigin.Invitation, ___m_Kingdom));
        }

        [HarmonyPatch(typeof(GuestCaravan), "OnCaravan")]
        private static class Open
        {
            private static bool Prefix(GuestCaravan __instance, bool ___m_FirstOpen, out bool __state)
            {
                __state = ___m_FirstOpen;
                return !Plugin.Owns(__instance) || !Plugin.Visit.Finished;
            }

            [HarmonyAfter("gkj.sfk.caravancommissions")]
            private static void Postfix(GuestCaravan __instance, bool __state, bool __runOriginal, List<string> ___m_Chosen)
            {
                if (!__runOriginal || !Plugin.Owns(__instance) || ___m_Chosen.Count == 0) return;
                var screen = OverlayManager.Instance.GetMerchant();
                if (!screen || !ReferenceEquals(Callback(screen)?.Target, __instance)) return;
                // The game activates the merchant after OnCaravan returns.
                var first = screen.GetComponentsInChildren<UICardItem>(true).FirstOrDefault(card => CardId(card) == ___m_Chosen[0]);
                if (!first) return;
                Plugin.StartCoroutine(Present(screen, first, __instance, __state));
            }

            private static IEnumerator Present(UIOverlayMerchant screen, UICardItem first, GuestCaravan guest, bool firstOpen)
            {
                var button = first.GetCardButton();
                var gamepad = GamepadManager.Instance;
                var previousFocus = gamepad.GetGameObjectToSelect();
                float deadline = UnityEngine.Time.realtimeSinceStartup + 3f;
                while (screen && button && ReferenceEquals(Callback(screen)?.Target, guest) && Plugin.Owns(guest))
                {
                    if (screen.isActiveAndEnabled && button.isActiveAndEnabled && button.IsInteractable())
                    {
                        CaravanPortrait.Show(screen);
                        if (firstOpen && gamepad.GetGameObjectToSelect() == previousFocus)
                            gamepad.SetGameObjectToSelect(button.gameObject, force: true);
                        yield break;
                    }
                    if (UnityEngine.Time.realtimeSinceStartup >= deadline) yield break;
                    yield return null;
                }
            }
        }

        [HarmonyPatch(typeof(GuestCaravan), "Cancel")]
        private static class Decline
        {
            private static void Postfix(GuestCaravan __instance, bool __runOriginal)
            {
                if (!__runOriginal || !Plugin.Owns(__instance) || Plugin.Visit.Finished) return;
                Plugin.Visit.declined = true;
                Plugin.Persist();
            }
        }

        // A custom card/grant hook can veto, transform, or attach side effects to an award.
        // Replaying that decision from an earlier dawn belongs to the active picker.
        internal static bool HasGrantHooks()
        {
            return HasForeignHooks(typeof(UICardItem), "Select") || HasForeignHooks(typeof(ItemManager), "SpawnDragItem");
        }

        private static bool HasForeignHooks(Type type, string name) =>
            Harmony.GetPatchInfo(AccessTools.Method(type, name))?.Owners.Any(id => id != "sfk.dailycaravan") == true;

        private static readonly AccessTools.FieldRef<UIOverlayMerchant, Action<int, ResourceAmount[]>> Callback =
            AccessTools.FieldRefAccess<UIOverlayMerchant, Action<int, ResourceAmount[]>>("m_Callback");
        private static readonly AccessTools.FieldRef<UICard, bool> Ready = AccessTools.FieldRefAccess<UICard, bool>("m_CanBeClicked");
        private static readonly AccessTools.FieldRef<UICardItem, string> CardId = AccessTools.FieldRefAccess<UICardItem, string>("m_ItemIdentifier");

        private static bool IsCaravanCard(UICardItem card)
        {
            var screen = card == null ? null : card.GetComponentInParent<UIOverlayMerchant>();
            return screen != null && Callback(screen)?.Target is GuestCaravan guest && Plugin.Owns(guest);
        }

        internal static bool CanGrant(UICardItem card, out string id)
        {
            id = IsCaravanCard(card) && Ready(card) ? CardId(card) : null;
            return !string.IsNullOrEmpty(id);
        }

        // A merchant card focuses and shows keywords before Select actually grants the item.
        // The callback's target distinguishes caravan cards from every other merchant, even after minimizing.
        [HarmonyPatch(typeof(UICardItem), "Select")]
        private static class Select
        {
            private static bool Prefix(UICardItem __instance, bool ___m_CanBeClicked, out int? __state)
            {
                __state = null;
                if (!IsCaravanCard(__instance)) return true;
                if (!Plugin.Visit.CanSelect) return false;
                if (___m_CanBeClicked) __state = ItemManager.Instance.GetDragItems().Count;
                return true;
            }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(int? __state, string ___m_ItemIdentifier)
            {
                if (__state == null) return;
                var drops = ItemManager.Instance.GetDragItems();
                int added = drops.Count - __state.Value;
                if (added <= 0) return;
                string singleDrop = added == 1 ? drops[__state.Value].GetItemIdentifier() : null;
                Plugin.RecordGrant(___m_ItemIdentifier, added, singleDrop);
            }
        }
    }
}
