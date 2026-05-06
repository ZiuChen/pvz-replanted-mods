using HarmonyLib;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.Gameplay;

namespace PvZReplantedEndlessHelper;

// ─── No-cooldown: reset packet state right after planting ────────────────────
//
// WasPlanted() native code sets mRefreshing=true / mRefreshCounter=mRefreshTime
// before our Postfix runs.  We immediately zero those out so the next
// UpdateModelData() tick sees the packet as "ready" and clears the overlay.
//
// The Prefix also guards against the "black slot" crash: P2 deselecting a plant
// during the seed-selection-to-gameplay transition leaves mPacketType=None.
// Letting native WasPlanted() run on a None-type packet crashes the game.

[HarmonyPatch(typeof(SeedPacket), nameof(SeedPacket.WasPlanted))]
internal static class WasPlantedPatch
{
    private static bool Prefix(SeedPacket __instance)
    {
        if (__instance == null) return false;
        // Unconditional guard: planting a None-type packet always crashes.
        if (__instance.mPacketType == SeedType.None) return false;
        return true;
    }

    private static void Postfix(SeedPacket __instance)
    {
        if (!Core.IsCoopEndless) return;
        try
        {
            if (__instance == null) return;
            __instance.mRefreshCounter = 0;
            __instance.mRefreshing     = false;
        }
        catch (System.Exception ex)
        {
            MelonLoader.MelonLogger.Warning($"[EndlessHelper] WasPlantedPatch error: {ex.Message}");
        }
    }
}

// ─── Prevent picking up black-slot cards ─────────────────────────────────────
//
// CanPickUp() is the gate before a player grabs a seed card.  Returning false
// for None-type slots means the cursor never reaches WasPlanted(), giving a
// clean UX instead of an invisible crash guard.

[HarmonyPatch(typeof(SeedPacket), nameof(SeedPacket.CanPickUp))]
internal static class CanPickUpPatch
{
    private static bool Prefix(SeedPacket __instance, ref bool __result)
    {
        if (__instance == null) return true;
        if (__instance.mPacketType == SeedType.None)
        {
            __result = false;
            return false;
        }
        return true;
    }
}

// ─── Fix persistent grey overlay — clear packet state BEFORE each tick ───────
//
// The grey overlay is driven by SeedPacket.mRefreshing / mRefreshCounter.
// UpdateModelData() (called inside OnTick) reads those fields and pushes them
// into the data-model layer, which the UI observes.  Our WasPlanted Postfix
// clears the fields immediately after planting, but native game code may also
// set mRefreshing=true in other paths.
//
// Patching OnTick() as a Prefix ensures the packet always looks "ready" before
// ANY native tick logic runs, so UpdateModelData sees a clean state every frame.
// The UpdateModelData Postfix below is kept as a belt-and-suspenders fallback.

[HarmonyPatch(typeof(SeedBankEntryModel), nameof(SeedBankEntryModel.OnTick))]
internal static class OnTickPatch
{
    private static void Prefix(SeedBankEntryModel __instance)
    {
        if (!Core.IsCoopEndless) return;
        try
        {
            var pkt = __instance?.m_seedPacket;
            if (pkt == null || pkt.mPacketType == SeedType.None) return;
            pkt.mRefreshing     = false;
            pkt.mRefreshCounter = 0;
        }
        catch (System.Exception ex)
        {
            MelonLoader.MelonLogger.Warning($"[EndlessHelper] OnTickPatch error: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(SeedBankEntryModel), nameof(SeedBankEntryModel.UpdateModelData))]
internal static class UpdateModelDataPatch
{
    private static void Postfix(SeedBankEntryModel __instance)
    {
        if (!Core.IsCoopEndless) return;
        try
        {
            if (__instance == null) return;
            // Only clear the overlay for slots that have a real plant type.
            // Leaving None-type slots (empty/uninitialized) untouched lets the
            // game keep them visually disabled, which prevents the cursor from
            // interacting with them (otherwise the UI shows them as pickable while
            // CanPickUp returns false — causing the game to freeze).
            var pkt = __instance.m_seedPacket;
            if (pkt == null || pkt.mPacketType == SeedType.None) return;
            __instance.m_refreshingModel.Value     = false;
            __instance.m_refreshPercentModel.Value = 0.0;
            __instance.m_refreshCounterModel.Value = 0.0;
            __instance.m_showDisabled.Value        = false;
        }
        catch (System.Exception ex)
        {
            MelonLoader.MelonLogger.Warning($"[EndlessHelper] UpdateModelDataPatch error: {ex.Message}");
        }
    }
}
