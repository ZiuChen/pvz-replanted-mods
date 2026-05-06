# PvZ Replanted Vase Reveal Mod

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

MelonLoader mod for PvZ Replanted that reveals or hints vase contents in Vasebreaker.

## Features

The mod currently supports two modes. Press F8 in-game to switch between them.

| Mode | Behavior | Purpose |
|------|----------|---------|
| TypeHint | Keeps the vase opaque and uses native vase variants to distinguish plant, zombie, and sun vases | Low-noise identification |
| FullReveal | Makes the vase semi-transparent and draws the exact content preview inside it | Direct reveal |

## Requirements

| Dependency | Version | Notes |
|------------|---------|-------|
| [MelonLoader](https://github.com/LavaGang/MelonLoader) | v0.7.2 Open-Beta | Installed into the game directory |
| [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) | 6.0+ x64 | Required for building |
| PvZ Replanted | 1.5.1469_Steam+ | Steam build |

## Installation

1. Install MelonLoader v0.7.2 into the PvZ Replanted game directory.
2. Copy the built PvZReplantedVaseReveal.dll into:

    ```
    <Steam game directory>/PVZ Replanted/Mods/
    ```

3. Launch the game. MelonLoader will load the mod automatically.

On first launch, MelonLoader may need around 8 minutes to generate IL2CPP interop assemblies.

## Build From Source

```powershell
git clone <repo-url>
Set-Location .\pvz-replanted-mods

# Override GameDir if needed
dotnet build .\mods\pvz-replanted-vase-reveal\PvZReplantedVaseReveal.csproj -c Release
```

After the build, the DLL is copied to <GameDir>/Mods/ automatically.

To override the game path:

```powershell
dotnet build .\mods\pvz-replanted-vase-reveal\PvZReplantedVaseReveal.csproj -p:GameDir="D:\SteamLibrary\steamapps\common\PVZ Replanted"
```

If MelonLoader fails while trying to install the runtime, set these environment variables first:

```powershell
$env:DOTNET_ROOT     = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:DOTNET_ROOT_X64 = "$env:LOCALAPPDATA\Microsoft\dotnet"
```

## Project Layout

```
pvz-replanted-mods/
└── mods/
    └── pvz-replanted-vase-reveal/
        ├── README.md
        ├── README.zh-CN.md
        ├── Core.cs
        ├── ScaryPotRevealPatch.cs
        ├── PvZReplantedVaseReveal.csproj
        ├── InteropRefs/
        └── docs/
```

## How It Works

PvZ Replanted is a Unity 6 IL2CPP game, so there is no editable managed gameplay DLL to patch directly. This project uses MelonLoader to load a .NET mod assembly into the game process, then uses Harmony to patch managed IL2CPP interop wrappers at runtime.

The main flow in this repository is:

1. [Core.cs](Core.cs#L24) initializes the mod and calls [PatchAll](Core.cs#L26).
2. [ScaryPotRevealPatch.cs](ScaryPotRevealPatch.cs#L8) patches ScaryPotController.Update.
3. The patch reads GridItem state and applies either a hint-only mode or a full reveal mode by changing state, alpha, and preview rendering.

More technical notes are available in [docs/progress.md](docs/progress.md) and [README.zh-CN.md](README.zh-CN.md#L96).

## macOS / CrossOver

Running through CrossOver should work in principle if MelonLoader itself works inside the bottle and the .NET runtime path is configured correctly.

## Disclaimer

This mod is intended for personal entertainment only. Do not use it in multiplayer, ranked, or competitive contexts.
