using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using UnityEngine;

namespace PvZReplantedVaseReveal;

[HarmonyPatch(typeof(ScaryPotController), nameof(ScaryPotController.Update))]
internal static class ScaryPotRevealPatch
{
    private const float OuterAlpha = 0.35f;
    private const float ThumbnailScale = 0.4f;

    private static void Postfix(ScaryPotController __instance)
    {
        // Guard: destroyed / inactive GameObjects can cause native crashes in IL2CPP interop
        try
        {
            if (__instance == null || !__instance.gameObject.activeInHierarchy)
                return;
        }
        catch { return; }

        try
        {
            var gridItem = __instance.m_gridItem;
            if (gridItem == null || gridItem.mGridItemType != GridItemType.ScaryPot || gridItem.mDead)
                return;

            // Only touch pots that are still in an unbroken ScaryPot state
            var state = gridItem.GridItemState;
            if (state != GridItemState.ScaryPotQuestion &&
                state != GridItemState.ScaryPotLeaf &&
                state != GridItemState.ScaryPotZombie)
                return;

            switch (Core.CurrentMode)
            {
                case RevealMode.TypeHint:
                    ApplyTypeHint(gridItem, __instance);
                    break;
                case RevealMode.FullReveal:
                    ApplyFullReveal(gridItem, __instance);
                    break;
            }
        }
        catch (System.Exception ex)
        {
            MelonLoader.MelonLogger.Error($"[VaseReveal] Patch error: {ex.Message}");
        }
    }

    // ── TypeHint ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully-opaque pot with a colour hint: green = plant, gold = zombie.
    /// Driven by GridItemState — the game's own sprite sheet handles the colouring.
    /// </summary>
    private static void ApplyTypeHint(GridItem gridItem, ScaryPotController pot)
    {
        SetAlpha(pot.m_outsideRenderer, 1f);

        gridItem.GridItemState = gridItem.mScaryPotType switch
        {
            ScaryPotType.Seed   => GridItemState.ScaryPotLeaf,
            ScaryPotType.Zombie => GridItemState.ScaryPotZombie,
            _                   => GridItemState.ScaryPotQuestion,
        };

        pot.m_previewDrawer?.ClearPreview();
        pot.m_sunsContainer?.SetActive(false);
    }

    // ── FullReveal ────────────────────────────────────────────────────────────

    /// <summary>
    /// Uses semi-transparent outer shell + snaps the preview sprite to the
    /// visual centre of the pot at thumbnail scale.
    /// </summary>
    private static void ApplyFullReveal(GridItem gridItem, ScaryPotController pot)
    {
        // Semi-transparent outer shell (same approach as v0.4)
        SetAlpha(pot.m_outsideRenderer, OuterAlpha);
        gridItem.GridItemState = GridItemState.ScaryPotQuestion;

        var drawer = pot.m_previewDrawer;
        pot.m_sunsContainer?.SetActive(false);

        switch (gridItem.mScaryPotType)
        {
            case ScaryPotType.Seed when drawer != null:
                drawer.SetPreview(gridItem.mSeedType, false);
                SnapPreview(drawer, pot.m_outsideRenderer);
                break;
            case ScaryPotType.Zombie when drawer != null:
                drawer.SetPreview(gridItem.mZombieType);
                SnapPreview(drawer, pot.m_outsideRenderer);
                break;
            case ScaryPotType.Sun:
                drawer?.ClearPreview();
                pot.m_sunsContainer?.SetActive(true);
                break;
            default:
                drawer?.ClearPreview();
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetAlpha(SpriteRenderer r, float a)
    {
        if (r == null) return;
        var c = r.color;
        if (c.a == a) return;
        c.a = a;
        r.color = c;
    }

    /// <summary>
    /// Snaps the preview sprite to the visual centre of the outer pot sprite
    /// using the SpriteRenderer's world-space bounds (avoids hardcoded Y offsets).
    /// Also resets any data-driven offset/scale SetPreview() may have written.
    /// </summary>
    private static void SnapPreview(PreviewDrawerController d, SpriteRenderer outside)
    {
        d.m_currentOffset = new Vector2(0f, 0f);
        d.m_currentScale = new Vector2(ThumbnailScale, ThumbnailScale);

        // Use the bounding-box centre of the rendered sprite — reliable regardless
        // of where the transform pivot is placed.
        var anchor = outside != null ? outside.bounds.center : Vector3.zero;
        // Shift slightly in front of the pot layer
        anchor.z -= 0.1f;

        var t = d.m_previewTransform;
        if (t != null)
        {
            t.position = anchor;
            t.localScale = new Vector3(ThumbnailScale, ThumbnailScale, 1f);
        }

        var s = d.m_previewSprite;
        if (s != null && t != null && s.transform != t)
            s.transform.localPosition = Vector3.zero;
    }
}

