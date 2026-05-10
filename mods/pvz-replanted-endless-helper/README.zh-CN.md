# PvZ Replanted — 合作无尽助手 Mod

英文文档： [README.md](README.md)

> 适用于「植物大战僵尸：重植版」（PvZ: Replanted）的 MelonLoader Mod，在合作无尽模式中自动解除阳光上限、清空种植冷却，并修复 P2 手柄断联后的重连识别问题，同时提供黑卡槽防崩溃保护。

## 功能

| 行为 | 生效范围 | 说明 |
|------|----------|------|
| 自动无限阳光 | 合作无尽 | 通过 `Board.AddSunMoney` 的前后置补丁恢复原本会被 9990 截断的阳光值。 |
| 自动无冷却 | 合作无尽 | 在种植后立即清空 `SeedPacket` 的刷新计数，并同步清理种子栏的冷却遮罩。 |
| 玉米加农炮 P2 修复 | 合作无尽 | 修复 `Board.MouseDown` 的路由判断，使 P2 的发射点击使用正确的 per-player cursor，P2 无需 P1 同时瞄准即可独立发射。 |
| P2 手柄重连修复 | 合作无尽 | 记录 P2 最近一次设备 ID，在手柄断联后重新插入时优先恢复 Guest 玩家绑定。 |
| 黑卡槽保护 | 全局 | 阻止 `SeedType.None` 被拾取，避免种子选择到实战过渡时的黑卡槽崩溃。 |
| 模式缓存 | 内部 | 在每次 `Board.InitLevel` 后缓存 `IsCoopMode()`，避免高频补丁反复查询。 |

- 除了黑卡槽保护外，其余游戏性补丁只在合作无尽模式下生效。
- 没有 F1 / F2 热键；进入合作无尽关卡后会自动启用。
- 每次绕过阳光上限或重新绑定 P2 时，MelonLoader 控制台都会输出日志。

## 前置条件

| 依赖 | 版本 | 说明 |
|------|------|------|
| [MelonLoader](https://github.com/LavaGang/MelonLoader) | v0.7.2 Open-Beta | 安装到游戏根目录 |
| [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) | 6.0+ x64 | 用于编译 Mod |
| PvZ Replanted | 1.5.1469_Steam+ | Steam 版 |

## 安装（直接使用）

1. 确认 MelonLoader v0.7.2 已安装到游戏目录。
2. 将编译好的 `PvZReplantedEndlessHelper.dll` 放入：

   ```
   <Steam游戏目录>/PVZ Replanted/Mods/
   ```

3. 启动游戏，进入合作无尽关卡即可自动生效。

## 从源码构建

```powershell
# 克隆仓库
git clone <repo-url>
Set-Location .\pvz-replanted-mods

# 构建并自动部署到 Mods 目录（使用默认 Steam 路径）
dotnet build .\mods\pvz-replanted-endless-helper\PvZReplantedEndlessHelper.csproj -c Release

# 若游戏不在默认路径，通过 GameDir 属性指定
dotnet build .\mods\pvz-replanted-endless-helper\PvZReplantedEndlessHelper.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\PVZ Replanted"
```

构建完成后，DLL 会自动复制到 `<GameDir>/Mods/`。

`InteropRefDir` 默认指向 `$(GameDir)\MelonLoader\Il2CppAssemblies`，无需手动复制互操作程序集。

## 项目结构

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

## 技术说明

游戏基于 **Unity 6 + IL2CPP** 构建，使用 **MelonLoader + Harmony** 在运行时 hook IL2CPP 互操作层。

### 自动无限阳光

原游戏会在 `Board.AddSunMoney` 内部将 `mSunMoney` 硬限制在 9990。Mod 通过 **Harmony Prefix + Postfix** 组合先记录理论值，再在原生代码执行后把被截断的结果恢复回来。

### 自动无冷却

`SeedPacket.mRefreshCounter` 会从 `mRefreshTime` 倒数到 0，期间卡片会显示灰色冷却遮罩。Mod 在 `SeedPacket.WasPlanted`、`SeedBankEntryModel.OnTick` 和 `SeedBankEntryModel.UpdateModelData` 里清空刷新状态，让真实植物卡片在合作无尽中立刻恢复可用。

### 玉米加农炮 P2 修复

`Board.MouseDown` 内部依赖 `mCursorObject.mCursorType` 判断点击类型，但 `mCursorObject` 始终指向 P1 的 cursor（类型为 `Normal`）。当 P2 瞄准炮击目标时，其 cursor 存储于 `Board.CursorObjects[1]`，路由逻辑从不读取该值，导致 P2 的发射点击被丢弃。Mod 在 `Board.MouseDown` 的 Prefix/Postfix 里临时将 `mCursorObject` 替换为 `CursorObjects[playerIndex]`，使路由逻辑能正确识别 `CobCannonTarget`，再还原原值。

### P2 手柄重连修复

Mod 会跟踪 P2 最近一次绑定的 `DeviceId`。当 Guest 手柄断联并重新插入后，`GuestPlayerInputActivity` 会优先把该设备重新绑定到 P2，避免两个手柄同时落到 P1。

### 黑卡槽保护

`SeedPacket.CanPickUp` 会在任何模式下阻止 `SeedType.None` 的卡槽被拾取。这能避免种子选择阶段到实战阶段切换时的黑卡槽崩溃。

### 模式检测

Mod 在每次 `Board.InitLevel` 调用后缓存 `GameplayActivity.IsCoopMode()` 的返回值，避免在每帧的高频补丁中重复查询。

## 免责声明

本 Mod 仅供个人娱乐使用，不用于竞技或牟利。请勿在多人联机排行榜场景中使用。
