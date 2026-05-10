using HarmonyLib;
using Il2CppReloaded.Gameplay;
using MelonLoader;

namespace PvZReplantedEndlessHelper;

// Root cause:
//   Board maintains a per-player CursorObjects (MultiplayerType<CursorObject>), but
//   Board.MouseDown routes based on the singular mCursorObject, which always reflects
//   the P1 cursor.  When P2 arms a CobCannon, P2's cursor type becomes CobCannonTarget
//   in CursorObjects[1], but the routing check sees mCursorObject (P1, type Normal) and
//   never dispatches to MouseDownCobcannonFire — so P2's fire click does nothing.
//   Because mCursorObject is also what MouseDownCobcannonFire uses to look up the armed
//   plant ID, a second swap-without-restore bug caused P1 to fire a bonus shot when P1
//   also had the cannon armed as a workaround.
//
// Fix:
//   In the Prefix of Board.MouseDown, when playerIndex > 0 and that player's cursor is
//   in CobCannonTarget mode, temporarily replace mCursorObject with CursorObjects[playerIndex].
//   The native method then routes and executes the fire correctly.  The Postfix restores
//   mCursorObject so P1's state is unaffected.

[HarmonyPatch(typeof(Board), nameof(Board.MouseDown))]
internal static class CobCannonMouseDownPatch
{
    private static CursorObject _savedCursor = null;

    private static void Prefix(Board __instance, int playerIndex)
    {
        _savedCursor = null;
        if (!Core.IsCoopEndless || playerIndex == 0) return;
        try
        {
            var playerCursor = __instance.CursorObjects[playerIndex];
            if (playerCursor == null || playerCursor.mCursorType != CursorType.CobCannonTarget) return;
            _savedCursor = __instance.mCursorObject;
            __instance.mCursorObject = playerCursor;
            MelonLogger.Msg($"[EndlessHelper] CobCannon: routing P{playerIndex + 1} fire click via correct cursor.");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[EndlessHelper] CobCannonMouseDownPatch.Prefix error: {ex.Message}");
        }
    }

    private static void Postfix(Board __instance)
    {
        if (_savedCursor == null) return;
        try
        {
            __instance.mCursorObject = _savedCursor;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[EndlessHelper] CobCannonMouseDownPatch.Postfix error: {ex.Message}");
        }
        finally
        {
            _savedCursor = null;
        }
    }
}
