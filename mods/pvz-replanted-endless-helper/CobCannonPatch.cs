using HarmonyLib;
using Il2CppReloaded.Gameplay;
using MelonLoader;

namespace PvZReplantedEndlessHelper;

// Root cause:
//   Board maintains a per-player CursorObjects (MultiplayerType<CursorObject>), but
//   Board.MouseDown routes based on the singular mCursorObject.  mCursorObject is a
//   shared mutable reference: whichever player most recently armed a CobCannon wins,
//   overwriting the other player's state.  This means:
//     - P2 arms cannon B  → mCursorObject now points to B
//     - P1 fires          → routing reads mCursorObject (B) and fires B instead of A
//     - P2 fires          → our old patch swaps to CursorObjects[1] (also B) → B fires again
//
// Fix:
//   For ANY playerIndex, when that player's CursorObjects[playerIndex] is in
//   CobCannonTarget mode, temporarily replace mCursorObject with the player's own cursor
//   before the native method runs, then restore it.  Each player's fire click now always
//   uses their own per-player cursor regardless of which cannon the other player selected.

[HarmonyPatch(typeof(Board), nameof(Board.MouseDown))]
internal static class CobCannonMouseDownPatch
{
    private static CursorObject _savedCursor = null;

    private static void Prefix(Board __instance, int playerIndex)
    {
        _savedCursor = null;
        if (!Core.IsCoopEndless) return;
        try
        {
            var playerCursor = __instance.CursorObjects[playerIndex];
            if (playerCursor == null || playerCursor.mCursorType != CursorType.CobCannonTarget) return;
            _savedCursor = __instance.mCursorObject;
            __instance.mCursorObject = playerCursor;
            MelonLogger.Msg($"[EndlessHelper] CobCannon: routing P{playerIndex + 1} fire click via correct cursor (cannon {playerCursor.mCobCannonPlantID}).");
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
