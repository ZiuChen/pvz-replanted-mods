using MelonLoader;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;

[assembly: MelonInfo(typeof(PvZReplantedEndlessHelper.Core), "pvz-replanted-endless-helper", "1.0.0", "Copilot")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]

namespace PvZReplantedEndlessHelper;

public sealed class Core : MelonMod
{
    /// <summary>
    /// Cached per-level flag: true when the current session is a cooperative
    /// endless run.  Updated by <see cref="BoardInitPatch"/> each time a level
    /// initialises so the feature patches don't call IsCoopMode() every frame.
    /// </summary>
    internal static bool IsCoopEndless = false;

    internal static Board CurrentBoard;
    internal static GameplayActivity CurrentGameplayActivity;

    public override void OnInitializeMelon()
    {
        HarmonyInstance.PatchAll(typeof(Core).Assembly);
        LoggerInstance.Msg("EndlessHelper loaded — unlimited sun + no cooldown active in co-op endless.");
    }
}
