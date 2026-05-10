using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;

namespace PvZReplantedEndlessHelper;

/// <summary>
/// Caches Core.IsCoopEndless on each level init.
/// </summary>
[HarmonyPatch(typeof(Board), nameof(Board.InitLevel))]
internal static class BoardInitPatch
{
    private static void Postfix(Board __instance)
    {
        try
        {
            GameplayActivity app = __instance?.mApp;
            bool isCoop = app != null && app.IsCoopMode();
            Core.IsCoopEndless = isCoop;
            MelonLoader.MelonLogger.Msg($"[EndlessHelper] BoardInitPatch: IsCoopMode={isCoop}");
        }
        catch (System.Exception ex)
        {
            Core.IsCoopEndless = false;
            MelonLoader.MelonLogger.Warning($"[EndlessHelper] BoardInitPatch error: {ex.Message}");
        }
    }
}

/// <summary>
/// Bypasses the 9990 sun cap in cooperative endless mode.
///
/// The native <c>AddSunMoney</c> clamps sun to 9990 internally.
/// We record the un-clamped target in the Prefix, then restore it in the
/// Postfix by writing directly to the backing <c>Il2CppArrayBase&lt;Sun&gt;</c>
/// rather than through the indexer, which avoids value-type copy issues.
/// </summary>
[HarmonyPatch(typeof(Board), nameof(Board.AddSunMoney))]
internal static class SunCapPatch
{
    private static readonly int[] _uncappedTarget = new int[2];

    private static void Prefix(Board __instance, int theAmount, int playerIndex)
    {
        if (!Core.IsCoopEndless) return;
        if ((uint)playerIndex >= (uint)_uncappedTarget.Length) return;

        try
        {
            int current = __instance.mSunMoney[playerIndex].Amount;
            _uncappedTarget[playerIndex] = current + theAmount;
        }
        catch { _uncappedTarget[playerIndex] = 0; }
    }

    private static void Postfix(Board __instance, int playerIndex)
    {
        if (!Core.IsCoopEndless) return;
        if ((uint)playerIndex >= (uint)_uncappedTarget.Length) return;

        try
        {
            int target = _uncappedTarget[playerIndex];
            if (target <= 0) return;

            // Write via backing array to avoid IL2CPP value-type copy pitfalls
            // when going through the MultiplayerType<Sun> indexer.
            var backing = __instance.mSunMoney.m_values;
            if (backing == null || playerIndex >= backing.Count) return;

            var sun = backing[playerIndex];
            if (sun.Amount < target)
            {
                sun.Amount = target;
                backing[playerIndex] = sun;
                MelonLoader.MelonLogger.Msg($"[EndlessHelper] Sun cap bypass: player {playerIndex} → {target}");
            }
        }
        catch (System.Exception ex)
        {
            MelonLoader.MelonLogger.Warning($"[EndlessHelper] SunCapPatch error: {ex.Message}");
        }
    }
}
