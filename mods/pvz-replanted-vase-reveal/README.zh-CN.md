# PvZ Replanted — 砸罐子透视 Mod

> 适用于「植物大战僵尸：重植版」（PvZ: Replanted）的 MelonLoader Mod，用于在砸罐子模式中快速识别或直接显示罐子内容。

## 当前功能

Mod 当前提供两种模式，默认进入 **TypeHint**，按 **F8** 在两种模式间切换：

| 模式 | 表现 | 用途 |
|------|------|------|
| TypeHint | 罐体保持不透明，通过游戏原生罐体样式区分植物 / 僵尸 / 阳光 | 低干扰识别 |
| FullReveal | 罐体半透明，并显示罐内具体内容预览 | 直接查看内容 |

### 当前行为

- **默认模式**：`TypeHint`
- **切换按键**：`F8`
- **植物罐**：TypeHint 下显示绿叶罐样式
- **僵尸罐**：TypeHint 下显示僵尸罐样式
- **阳光罐**：沿用游戏内阳光预览容器
- **FullReveal**：调用 `PreviewDrawerController` 绘制植物 / 僵尸预览，并将预览吸附到罐体中心

### 当前状态

- `TypeHint` 已基本稳定，可用于快速判断类型
- `FullReveal` 已实现半透明 + 内容预览，但视觉表现仍在继续调优
- 与灯笼草完全一致的原生透视效果仍在调研中，当前版本尚未复用到该机制

## 前置条件

| 依赖 | 版本 | 说明 |
|------|------|------|
| [MelonLoader](https://github.com/LavaGang/MelonLoader) | v0.7.2 Open-Beta | 安装到游戏根目录 |
| [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) | 6.0+ x64 | 用于编译 Mod |
| PvZ Replanted | 1.5.1469_Steam+ | Steam 版 |

## 安装方法（直接使用）

1. 确认 MelonLoader v0.7.2 已安装到游戏目录
2. 将编译好的 `PvZReplantedVaseReveal.dll` 放入：
   ```
   <Steam游戏目录>/PVZ Replanted/Mods/
   ```
3. 启动游戏，MelonLoader 会自动加载 Mod

> **首次运行**：MelonLoader 需要约 8 分钟生成 Il2Cpp 互操作程序集，请耐心等待。

进入砸罐子关卡后，按 **F8** 可在 `TypeHint` 与 `FullReveal` 之间切换。

## 从源码构建

```powershell
# 克隆仓库
git clone <repo-url>
Set-Location .\pvz-replanted-mods

# 确保游戏目录正确（默认路径如下，可通过 GameDir 属性覆盖）
# C:\Program Files (x86)\Steam\steamapps\common\PVZ Replanted

# 构建并自动部署到 Mods 目录
dotnet build .\mods\pvz-replanted-vase-reveal\PvZReplantedVaseReveal.csproj -c Release
```

构建完成后，DLL 会自动复制到 `<GameDir>/Mods/`。

### 自定义游戏路径

```powershell
dotnet build .\mods\pvz-replanted-vase-reveal\PvZReplantedVaseReveal.csproj -p:GameDir="D:\SteamLibrary\steamapps\common\PVZ Replanted"
```

## 环境变量（重要）

如果 MelonLoader 启动时报错 "Downloading .NET Runtime installer... Unhandled exception"，需要手动指定 .NET 路径：

```powershell
# 用户级安装的 .NET 时需要设置
$env:DOTNET_ROOT     = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:DOTNET_ROOT_X64 = "$env:LOCALAPPDATA\Microsoft\dotnet"
```

建议在 Steam 游戏属性 → 启动选项中添加，或写入系统环境变量。

## 项目结构

```
pvz-replanted-mods/
└── mods/
    └── pvz-replanted-vase-reveal/
        ├── Core.cs                    # MelonLoader Mod 入口，注册 Harmony patches
        ├── ScaryPotRevealPatch.cs     # 核心 patch：透视 ScaryPot 内容
        ├── PvZReplantedVaseReveal.csproj
        ├── InteropRefs/               # Il2Cpp 互操作 DLL（从游戏 MelonLoader 目录复制）
        └── docs/
            └── progress.md            # 方案探索记录
```

## 技术背景

游戏基于 **Unity 6 (6000.0.52f1) + IL2CPP** 构建，无法直接修改托管 DLL。Mod 使用 MelonLoader + Harmony 在运行时 hook IL2CPP 互操作层，通过修改 `GridItem` 状态、`ScaryPotController` 外层渲染器透明度，以及 `PreviewDrawerController` 的预览位置与缩放来实现效果。

关键类型：
- `Il2CppReloaded.Gameplay.GridItem` — 罐子的数据模型（`mScaryPotType`、`mSeedType`、`mZombieType`）
- `Il2CppSource.Controllers.ScaryPotController` — 罐子的视图控制器
- `Il2CppSource.Controllers.PreviewDrawerController` — 内容物预览渲染器

当前实现要点：
- `TypeHint` 通过 `GridItemState` 切换为游戏已有的植物 / 僵尸罐体样式
- `FullReveal` 通过降低 `m_outsideRenderer` 的 alpha 制造透视效果
- 预览内容通过 `SetPreview(...)` 驱动，并在每帧修正偏移与缩放
- `UpdateImageColumn(1)` 已确认不是灯笼草透视方案，而是会把罐体错误切成植物罐样式

详细技术过程见 [docs/progress.md](docs/progress.md)。

## macOS / CrossOver

理论上可在 CrossOver 下运行，但需额外配置：
1. 在 CrossOver bottle 的环境变量中设置 `DOTNET_ROOT`
2. MelonLoader v0.7.2 在 Wine 层已有用户验证可运行
3. 当前方案本质是 IL2CPP + MelonLoader 运行时 patch，只要 MelonLoader 在 bottle 内正常工作，Mod 逻辑本身可随之运行

## 免责声明

本 Mod 仅供个人娱乐使用，不用于竞技或牟利。请勿在多人联机或排行榜场景中使用。
