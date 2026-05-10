using HarmonyLib;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.Gameplay;
using MelonLoader;

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
            MelonLogger.Warning($"[EndlessHelper] WasPlantedPatch error: {ex.Message}");
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

// ─── Fix persistent grey overlay: zero packet state every frame ──────────────
//
// SeedPacket.Update() runs every frame and drives the mRefreshing /
// mRefreshCounter fields that the model layer reads.  Clearing them in the
// Prefix ensures the packet NEVER appears to be recharging during a co-op
// endless session, regardless of which downstream call triggered the update.

[HarmonyPatch(typeof(SeedPacket), nameof(SeedPacket.Update))]
internal static class SeedPacketUpdatePatch
{
    private static void Prefix(SeedPacket __instance)
    {
        if (!Core.IsCoopEndless) return;
        try
        {
            if (__instance == null || __instance.mPacketType == SeedType.None) return;
            __instance.mRefreshing     = false;
            __instance.mRefreshCounter = 0;
        }
        catch (System.Exception ex)
        {
            MelonLogger.Warning($"[EndlessHelper] SeedPacketUpdatePatch error: {ex.Message}");
        }
    }
}

// ─── Belt-and-suspenders: also zero on OnTick ────────────────────────────────
//
// Prefix: clears raw packet flags so the game's OnTick sees "ready".
// Postfix: overrides m_showDisabled AFTER the game's OnTick has had its say.
//          In co-op endless there is no cooldown, so m_showDisabled should
//          only be true when the player cannot afford the card (m_canAffordModel=false).

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
            MelonLoader.MelonLogger.Warning($"[EndlessHelper] OnTickPatch.Prefix error: {ex.Message}");
        }
    }

    private static void Postfix(SeedBankEntryModel __instance)
    {
        if (!Core.IsCoopEndless) return;
        try
        {
            if (__instance == null) return;
            var pkt = __instance.m_seedPacket;
            if (pkt == null || pkt.mPacketType == SeedType.None) return;

            // Keep overlay only when player genuinely can't afford the card.
            // No cooldown exists in co-op endless, so any other disabling is spurious.
            bool canAfford = __instance.m_canAffordModel?.Value ?? true;
            __instance.m_showDisabled.Value = !canAfford;
        }
        catch (System.Exception ex)
        {
            MelonLoader.MelonLogger.Warning($"[EndlessHelper] OnTickPatch.Postfix error: {ex.Message}");
        }
    }
}

// ─── Zero packet state BEFORE UpdateModelData reads it, then zero model ──────
//
// Prefix: clears the source packet fields so UpdateModelData sees "ready".
// Postfix: zeroes the model's own observable values as a final safety net,
//          in case UpdateModelData uses a code path our Prefix cannot reach.

[HarmonyPatch(typeof(SeedBankEntryModel), nameof(SeedBankEntryModel.UpdateModelData))]
internal static class UpdateModelDataPatch
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
            MelonLogger.Warning($"[EndlessHelper] UpdateModelDataPatch.Prefix error: {ex.Message}");
        }
    }

    private static void Postfix(SeedBankEntryModel __instance)
    {
        if (!Core.IsCoopEndless) return;
        try
        {
            if (__instance == null) return;
            var pkt = __instance.m_seedPacket;
            if (pkt == null || pkt.mPacketType == SeedType.None) return;
            __instance.m_refreshingModel.Value     = false;
            __instance.m_refreshPercentModel.Value = 1.0;
            __instance.m_refreshCounterModel.Value = 0.0;
            bool wasDisabled = __instance.m_showDisabled.Value;
            __instance.m_showDisabled.Value        = false;
            if (wasDisabled)
                MelonLogger.Msg($"[EndlessHelper] Cleared showDisabled for {pkt.mPacketType}");
        }
        catch (System.Exception ex)
        {
            MelonLogger.Warning($"[EndlessHelper] UpdateModelDataPatch.Postfix error: {ex.Message}");
        }
    }
}

