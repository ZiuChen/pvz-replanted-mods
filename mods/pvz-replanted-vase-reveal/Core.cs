using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(PvZReplantedVaseReveal.Core), "pvz-replanted-vase-reveal", "0.5.0", "Copilot")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]

namespace PvZReplantedVaseReveal;

public enum RevealMode
{
    /// <summary>Color-coded pot (green = plant, gold = zombie). No transparency.</summary>
    TypeHint,
    /// <summary>Native Plantern-style transparent pot + exact content preview.</summary>
    FullReveal,
}

public sealed class Core : MelonMod
{
    internal static RevealMode CurrentMode = RevealMode.TypeHint;

    private const KeyCode ToggleKey = KeyCode.F8;

    public override void OnInitializeMelon()
    {
        HarmonyInstance.PatchAll(typeof(Core).Assembly);
        LoggerInstance.Msg($"VaseReveal loaded (v0.5.0). Press {ToggleKey} to toggle TypeHint / FullReveal.");
    }

    public override void OnUpdate()
    {
        if (!Input.GetKeyDown(ToggleKey))
            return;

        CurrentMode = CurrentMode == RevealMode.TypeHint
            ? RevealMode.FullReveal
            : RevealMode.TypeHint;

        LoggerInstance.Msg($"[VaseReveal] Mode → {CurrentMode}");
    }
}
