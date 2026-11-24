using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SuperFantasyKingdom;

namespace DailyCaravan
{
    [HarmonyPatch(typeof(GuestCaravan), "Reroll")]
    internal static class NativeDraw
    {
        // Only adapt the installed game's GetItems -> distinct-card draw. Providers that
        // replace the picker (including a commissioned delivery) keep their entire flow.
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = new List<CodeInstruction>(instructions);
            var getItems = AccessTools.Method(typeof(GuestCaravan), "GetItems");
            int fetch = code.FindIndex(op => op.Calls(getItems));
            if (fetch < 0) return code;
            var chosen = AccessTools.Field(typeof(GuestCaravan), "m_Chosen");
            var count = AccessTools.PropertyGetter(typeof(List<string>), nameof(List<string>.Count));
            int target = fetch + 3;
            while (target + 1 < code.Count)
            {
                if (code[target - 2].LoadsField(chosen) && code[target - 1].Calls(count) &&
                    (code[target + 1].opcode == OpCodes.Blt || code[target + 1].opcode == OpCodes.Blt_S))
                    break;
                target++;
            }
            if (target + 1 >= code.Count) return code;

            var unique = il.DeclareLocal(typeof(int));
            // Leave the requested card count and the game's RNG, card creation and callbacks intact.
            code.InsertRange(target + 1, new[] {
                new CodeInstruction(OpCodes.Ldloc, unique),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), new[] {typeof(int), typeof(int)}))
            });
            code.InsertRange(fetch + 1, new[] {
                new CodeInstruction(OpCodes.Ldloca, unique),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeDraw), nameof(Registered)))
            });
            return code;
        }

        private static List<string> Registered(List<string> items, out int distinct) =>
            OfferPool.Registered(items, id => AddressablesManager.Instance.GetItem(id) != null, out distinct);
    }
}
