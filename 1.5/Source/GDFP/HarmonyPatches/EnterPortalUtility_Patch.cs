﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GDFP.HarmonyPatches;


[HarmonyPatch(typeof(EnterPortalUtility))]
public static class EnterPortalUtility_Patch
{
    public static AcceptanceReport CanOpenPortal(Pawn pawn, MapPortal portal)
    {
        if (pawn == null)
        {
            return true;
        }

        if (!pawn.CanReach(portal, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
        {
            return "NoPath".Translate();
        }

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            return "Incapable".Translate();
        }

        if (portal is Building_Quackaai { IsOpen: true })
        {
            return "Quacka'ai already open";
        }

        return true;
    }

    [HarmonyPatch(nameof(EnterPortalUtility.GetFloatMenuOptFor), typeof(Pawn), typeof(IntVec3))]
    [HarmonyPrefix]
    public static bool GetFloatMenuOptFor_Patch(Pawn pawn, IntVec3 clickCell, ref FloatMenuOption __result)
    {
        List<Thing> doors = clickCell.GetThingList(pawn.Map).Where(t => t is Building_Quackaai or Building_QuackaaiExit).ToList();
        if (doors.Count <= 0)
        {
            return true;
        }

        if (doors.First() is not Building_Quackaai portal)
        {
            return true;
        }

        // TODO: Check if door is "opened" if not, option to open, otherwise, option to enter
        if (portal.IsOpen)
        {
            AcceptanceReport acceptanceReport = EnterPortalUtility.CanEnterPortal(pawn, portal);
            if (!acceptanceReport.Accepted)
            {
                __result = new FloatMenuOption("CannotEnterPortal".Translate(portal.Label) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null, MenuOptionPriority.Default,
                    null, null, 0f, null, null, true, 0);
                return false;
            }

            __result = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(portal.EnterCommandString, delegate
            {
                Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, portal);
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
            }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0), pawn, portal, "ReservedBy", null);
        }
        else
        {
            AcceptanceReport acceptanceReport = CanOpenPortal(pawn, portal);
            if (!acceptanceReport.Accepted)
            {
                __result = new FloatMenuOption("GDFP_CannotOpen".Translate(portal.Label) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null, MenuOptionPriority.Default,
                    null, null, 0f, null, null, true, 0);
                return false;
            }

            if (portal.selectedAddress == null)
            {
                __result = new FloatMenuOption("GDFP_NoAddressSelected".Translate(portal.Label), null, MenuOptionPriority.Default,
                    null, null, 0f, null, null, true, 0);
                return false;
            }

            __result = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(portal.OpenCommandString, delegate
            {
                Job job = JobMaker.MakeJob(GDFPDefOf.GDFP_OpenGate, portal);
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
            }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0), pawn, portal, "ReservedBy", null);
        }

        return false;
    }

    [HarmonyPatch(nameof(EnterPortalUtility.GetFloatMenuOptFor), typeof(List<Pawn>), typeof(IntVec3))]
    [HarmonyPrefix]
    public static bool GetFloatMenuOptFor_Patch(List<Pawn> pawns, IntVec3 clickCell, ref FloatMenuOption __result)
    {
        List<Thing> doors = clickCell.GetThingList(pawns[0].Map).Where(t => t is Building_Quackaai or Building_QuackaaiExit).ToList();
        if (doors.Count <= 0)
        {
            return true;
        }

        if (doors.First() is not Building_Quackaai portal)
        {
            return true;
        }

        __result = null;

        // TODO: Check if door is "opened" if not, option to open, otherwise, option to enter
        if (portal.IsOpen)
        {
            List<Pawn> tmpPortalEnteringPawns = AccessTools.StaticFieldRefAccess<List<Pawn>>(typeof(EnterPortalUtility), "tmpPortalEnteringPawns");

            AcceptanceReport acceptanceReport = EnterPortalUtility.CanEnterPortal(null, portal);
            if (!acceptanceReport.Accepted)
            {
                __result = new FloatMenuOption("CannotEnterPortal".Translate(portal) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null, MenuOptionPriority.Default, null,
                    null, 0f, null, null, true, 0);
                return false;
            }

            foreach (Pawn pawn in pawns)
            {
                if (EnterPortalUtility.CanEnterPortal(pawn, portal).Accepted)
                {
                    tmpPortalEnteringPawns.Add(pawn);
                }
            }

            if (!tmpPortalEnteringPawns.NullOrEmpty())
            {
                __result = new FloatMenuOption(portal.EnterCommandString, delegate
                {
                    foreach (Pawn pawn2 in tmpPortalEnteringPawns)
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, portal);
                        job.playerForced = true;
                        pawn2.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
                    }
                }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0);
                return false;
            }
        }
        else
        {
            foreach (Pawn pawn in pawns)
            {
                if (CanOpenPortal(pawn, portal).Accepted)
                {
                    __result = new FloatMenuOption(portal.OpenCommandString, delegate
                    {
                        Job job = JobMaker.MakeJob(GDFPDefOf.GDFP_OpenGate, portal);
                        job.playerForced = true;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
                    }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0);
                    return false;
                }
            }
        }

        return false;
    }
}
