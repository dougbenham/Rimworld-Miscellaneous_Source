using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobot
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.ReadyForNextToil))]
    public class PatchJobDriver_ReadyForNextToil
    {
        // Unforbid resources that were mined outside of home area
        static void Prefix(JobDriver __instance)
        {
            var pos = __instance?.job?.targetA.Cell;
            if (pos.HasValue && pos.Value.IsValid
                             && __instance is JobDriver_Mine
                             && __instance.pawn?.GetType().Namespace == "AIRobot"
                             && __instance.pawn.Map != null)
            {
                foreach (var forbidden in pos.Value.GetThingList(__instance.pawn.Map).Where(t => t.IsForbidden(Faction.OfPlayer)))
                    forbidden.SetForbidden(false, false);
            }
        }
    }
}