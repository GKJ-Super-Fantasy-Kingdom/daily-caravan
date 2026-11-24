using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Spawner;
using UnityEngine;

namespace DailyCaravan
{
    // Observe the game's actual first-open award, including its kingdom, price and position.
    [HarmonyPatch(typeof(GuestCaravan), "OnCaravan")]
    internal static class CaravanFaithBonus
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var spawn = AccessTools.Method(typeof(DroppedShardSpawner), nameof(DroppedShardSpawner.Spawn));
            var grant = AccessTools.Method(typeof(CaravanFaithBonus), nameof(Grant));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(spawn))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = grant;
                }
                yield return instruction;
            }
        }

        private static void Grant(DroppedShardSpawner spawner, Vector3 position, int amount, ResourceType resource,
            bool onScreen, GuestCaravan guest)
        {
            var plugin = DailyCaravanPlugin.Instance;
            if (plugin != null && plugin.Owns(guest))
                plugin.GrantFaithBonus(spawner, position, amount, resource, onScreen);
            else
                spawner.Spawn(position, amount, resource, onScreen);
        }
    }
}
