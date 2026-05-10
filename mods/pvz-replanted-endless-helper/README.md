# PvZ Replanted Endless Helper Mod

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

MelonLoader mod for PvZ Replanted that automatically applies cooperative endless helpers. In co-op endless it removes the sun cap, clears planting cooldowns, and restores the P2 controller after reconnects. A separate safety guard prevents black-slot seed packets from being picked up in every mode.

## Features

| Behavior | Scope | Notes |
|----------|-------|-------|
| Unlimited sun | Co-op endless only | Bypasses the native 9990 cap by restoring the unclamped value after `Board.AddSunMoney`. |
| Instant seed cooldown | Co-op endless only | Clears `SeedPacket` refresh state and the seed bank cooldown overlay immediately after planting, then keeps the UI in sync during later ticks. |
| Cob Cannon P2 fix | Co-op endless only | Patches `Board.MouseDown` to always route each player's fire click through their own per-player cursor (`CursorObjects[playerIndex]`), preventing the shared `mCursorObject` from causing cross-player cannon misfires. |
| P2 reconnect recovery | Co-op endless only | Tracks the guest controller identity and rebinds it to player 2 after reconnects. |
| Black-slot guard | All modes | Blocks `SeedType.None` packets from being picked up, which avoids the crash that can happen during the seed-selection to gameplay transition. |
| Mode cache | Internal | Caches `GameplayActivity.IsCoopMode()` on each level load so the patches do not query it every frame. |

There are no in-game hotkeys. The gameplay patches activate automatically when a co-op endless level initializes.

## Requirements

| Dependency | Version | Notes |
|------------|---------|-------|
| [MelonLoader](https://github.com/LavaGang/MelonLoader) | v0.7.2 Open-Beta | Installed into the game directory |
| [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) | 6.0+ x64 | Required for building the mod |
| PvZ Replanted | 1.5.1469_Steam+ | Steam build |

## Installation

1. Install MelonLoader v0.7.2 into the PvZ Replanted game directory.
2. Copy the built `PvZReplantedEndlessHelper.dll` into:

   ```
   <Steam game directory>/PVZ Replanted/Mods/
   ```

3. Launch the game and enter a co-op endless level.

## Build From Source

```powershell
git clone <repo-url>
Set-Location .\pvz-replanted-mods

# Build and copy the DLL into Mods using the default Steam path
dotnet build .\mods\pvz-replanted-endless-helper\PvZReplantedEndlessHelper.csproj -c Release

# Override GameDir if the game is installed elsewhere
dotnet build .\mods\pvz-replanted-endless-helper\PvZReplantedEndlessHelper.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\PVZ Replanted"
```

After the build, the DLL is copied to `<GameDir>/Mods/` automatically.

`InteropRefDir` defaults to `$(GameDir)\MelonLoader\Il2CppAssemblies`, so no manual interop copy is needed.

## Project Layout

```
pvz-replanted-mods/
└── mods/
    └── pvz-replanted-endless-helper/
        ├── Core.cs
        ├── SunCapPatch.cs
        ├── CooldownPatch.cs
        ├── CobCannonPatch.cs
        ├── GamepadReconnectPatch.cs
        ├── PvZReplantedEndlessHelper.csproj
        ├── README.md
        └── README.zh-CN.md
```

## How It Works

PvZ Replanted is a Unity 6 IL2CPP game, so this mod uses MelonLoader and Harmony to patch IL2CPP interop wrappers at runtime.

The main flow in this repository is:

1. `Core.cs` initializes the mod and patches the assembly.
2. `SunCapPatch.cs` caches whether the current level is co-op endless, then patches `Board.AddSunMoney` to restore the unclamped sun total through the backing array.
3. `CooldownPatch.cs` keeps seed packets ready in co-op endless by clearing refresh state in `SeedPacket.WasPlanted`, `SeedBankEntryModel.OnTick`, and `SeedBankEntryModel.UpdateModelData`.
4. `CobCannonPatch.cs` fixes Cob Cannon firing in co-op: `mCursorObject` is a shared mutable reference updated by whichever player most recently armed a cannon, so when P1 has cannon A and P2 has cannon B, P2 selecting B overwrites `mCursorObject` with B's state — causing P1 to fire B and P2 to fire B again. The patch applies to every `playerIndex`: it reads `CursorObjects[playerIndex]` (the per-player cursor) and temporarily swaps it into `mCursorObject` before the native call, then restores the original value in the Postfix. Each player now always fires their own cannon.
5. `GamepadReconnectPatch.cs` tracks the last guest device and restores P2 ownership when an unpaired controller reconnects.
6. `CooldownPatch.cs` also blocks `SeedType.None` packets in `SeedPacket.CanPickUp`, which prevents the black-slot crash.

## Disclaimer

This mod is intended for personal entertainment only. Do not use it in multiplayer, ranked, or competitive contexts.
