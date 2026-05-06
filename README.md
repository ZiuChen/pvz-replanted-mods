# pvz-replanted-mods

用于统一维护多个 **PvZ Replanted** MelonLoader Mod 的轻量 monorepo。

当前包含：

- `mods/pvz-replanted-endless-helper`：合作无尽辅助
- `mods/pvz-replanted-vase-reveal`：砸罐子透视

## 目录结构

```text
pvz-replanted-mods/
├── Directory.Build.props
├── Directory.Build.targets
├── pvz-replanted-mods.sln
└── mods/
    ├── pvz-replanted-endless-helper/
    └── pvz-replanted-vase-reveal/
```

## 构建

```powershell
Set-Location .\pvz-replanted-mods

# 构建整个仓库
dotnet build .\pvz-replanted-mods.sln -c Release

# 或只构建单个 mod
dotnet build .\mods\pvz-replanted-endless-helper\PvZReplantedEndlessHelper.csproj -c Release
dotnet build .\mods\pvz-replanted-vase-reveal\PvZReplantedVaseReveal.csproj -c Release
```

如游戏目录不是默认 Steam 路径，可覆盖 `GameDir`：

```powershell
dotnet build .\pvz-replanted-mods.sln -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\PVZ Replanted"
```

`pvz-replanted-vase-reveal` 默认使用项目目录下的 `InteropRefs`；`pvz-replanted-endless-helper` 默认直接引用游戏目录中的 `MelonLoader\Il2CppAssemblies`。
