using System;
using System.Collections.Generic;
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
                    RaceDataManager.Instance.HasRaceEventFlag(RaceEvent.GateOpened) && Plugin.Request())
                    __result = false;
            }
        }

        // Intercept outside TriggerManually too: WorldManager assigns its guest slot before calling it.
        [HarmonyPatch(typeof(WorldManager), "TriggerCaravan")]
        private static class Tokens
        {
            private static bool Prefix(Races __0) => !Plugin.Request(__0);
        }

        [HarmonyPatch(typeof(GuestCaravan), nameof(GuestCaravan.TriggerManually), typeof(Races))]
        private static class Invitation
        {
            private static bool Prefix(Races __0) => Plugin.Launching || !Plugin.Request(__0);
        }

        [HarmonyPatch(typeof(GuestCaravan), "OnCaravan")]
        private static class Open
        {
            private static bool Prefix(GuestCaravan __instance) => !Plugin.Owns(__instance) || !Plugin.Visit.Finished;
        }

        [HarmonyPatch(typeof(GuestCaravan), "Reroll")]
        private static class Inventory
        {
            private static void Prefix(GuestCaravan __instance, bool ___m_FirstOpen, List<string> ___m_Chosen,
                ref bool __1)
            {
                if (!Plugin.Owns(__instance) || !___m_FirstOpen || Plugin.Visit.items == null) return;
                ___m_Chosen.Clear();
                ___m_Chosen.AddRange(Plugin.Visit.items);
                __1 = false;
            }

            private static void Postfix(GuestCaravan __instance, List<string> ___m_Chosen)
            {
                if (Plugin.Owns(__instance) && ___m_Chosen.Count > 0)
                {
                    Plugin.Visit.items = ___m_Chosen.ToArray();
                    Plugin.Persist();
                }
            }
        }

        [HarmonyPatch(typeof(GuestCaravan), "Cancel")]
        private static class Decline
        {
            private static void Postfix(GuestCaravan __instance)
            {
                if (!Plugin.Owns(__instance) || Plugin.Visit.Finished) return;
                Plugin.Visit.declined = true;
                Plugin.Persist();
            }
        }

        // A merchant card focuses and shows keywords before Select actually grants the item.
        // The callback's target distinguishes caravan cards from every other merchant, even after minimizing.
        [HarmonyPatch(typeof(UICardItem), "Select")]
        private static class Select
        {
            private static readonly AccessTools.FieldRef<UIOverlayMerchant, Action<int, ResourceAmount[]>> Callback =
                AccessTools.FieldRefAccess<UIOverlayMerchant, Action<int, ResourceAmount[]>>("m_Callback");

            private static bool Prefix(UICardItem __instance, bool ___m_CanBeClicked,
                string ___m_ItemIdentifier, out string __state)
            {
                __state = null;
                var merchant = __instance.GetComponentInParent<UIOverlayMerchant>();
                if (merchant == null || !(Callback(merchant)?.Target is GuestCaravan guest) || !Plugin.Owns(guest))
                    return true;
                if (Plugin.Visit.Finished) return false;
                if (___m_CanBeClicked) __state = ___m_ItemIdentifier;
                return true;
            }

            private static void Postfix(string __state)
            {
                if (__state == null) return;
                Plugin.Visit.selected = __state;
                Plugin.Persist();
            }
        }
    }
}
