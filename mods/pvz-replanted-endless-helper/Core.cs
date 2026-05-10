using System.Text;
using MelonLoader;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSteamworks;

[assembly: MelonInfo(typeof(PvZReplantedEndlessHelper.Core), "pvz-replanted-endless-helper", "1.1.0", "Copilot")]
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

    internal static int P2LastKnownDeviceId = -1;

    private const string SaveFileName = "CoopEndlessSave.json";

    public override void OnInitializeMelon()
    {
        HarmonyInstance.PatchAll(typeof(Core).Assembly);
        CleanupLegacySaveData();
        LoggerInstance.Msg("EndlessHelper loaded — coop endless helpers and guest reconnect are active.");
    }

    /// <summary>
    /// Removes any <c>CoopEndlessSave.json</c> left over from earlier mod versions
    /// that implemented save/restore (now removed).  Runs once on startup so stale
    /// data doesn't linger on disk or in Steam Cloud.
    /// </summary>
    private void CleanupLegacySaveData()
    {
        try
        {
            string localPath = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, SaveFileName);
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
                LoggerInstance.Msg($"[EndlessHelper] Removed legacy save file: {localPath}");
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Warning($"[EndlessHelper] Could not remove legacy local save: {ex.Message}");
        }

        try
        {
            if (SteamRemoteStorage.IsCloudEnabled && SteamRemoteStorage.FileExists(SaveFileName))
            {
                SteamRemoteStorage.FileDelete(SaveFileName);
                LoggerInstance.Msg($"[EndlessHelper] Removed legacy save file from Steam Cloud.");
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Warning($"[EndlessHelper] Could not remove legacy Steam Cloud save: {ex.Message}");
        }
    }
}
