# 开发进度与方案记录

> 记录「PvZ 砸罐子透视 Mod」从调研到实现的完整过程，供后续维护参考。

---

## 阶段 1：游戏架构调研

### 结论

| 项目 | 内容 |
|------|------|
| 游戏版本 | 1.5.1469_Steam |
| 引擎 | Unity 6 (6000.0.52f1) |
| 编译模式 | **IL2CPP**（非 Mono，无可编辑的托管 DLL） |
| 元数据版本 | v31.1 |
| 开发商 | PopCap Games |

### 关键发现

- 游戏使用 IL2CPP 编译，`<GameDir>/PVZ Replanted_Data/Managed/` 中无源码 DLL
- 必须通过 **MelonLoader + Harmony** 在运行时 hook IL2CPP 互操作层
- 直接修改游戏文件不可行（IL2CPP 二进制难以反编译/回注）

---

## 阶段 2：工具链搭建

### 工具清单

| 工具 | 版本 | 用途 |
|------|------|------|
| MelonLoader | v0.7.2 Open-Beta | Mod 加载框架 |
| .NET SDK | 6.0.428 x64 | 编译 Mod DLL |
| Il2CppDumper | 6.7.46 | 提取游戏符号 |
| Cpp2IL | 2022.1.0-pre-release.21 | MelonLoader 内置，生成互操作程序集 |

### 踩坑：Cpp2IL 版本兼容性

最初下载的 Cpp2IL **2022.0.7** 只支持元数据 v24–v29，游戏使用 v31.1，直接报错退出。  
解决方案：改用 **Il2CppDumper 6.7.46** 提取符号（生成 `dump.cs` 和 DummyDll），  
互操作程序集由 MelonLoader 内置的更新版 Cpp2IL 自动生成（首次运行约 8 分钟）。

### 踩坑：MelonLoader 启动崩溃

**现象**：首次启动游戏时弹出 MelonLoader 对话框：
```
[MelonLoader.Bootstrap] Downloading the .NET Runtime installer...
[MelonLoader.Bootstrap] Installing the .NET Runtime...
Unhandled exception.
```

**原因**：.NET SDK 安装到了用户目录（非系统级），MelonLoader 找不到 `dotnet` 可执行文件。

**修复**：设置环境变量：
```powershell
$env:DOTNET_ROOT     = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:DOTNET_ROOT_X64 = "$env:LOCALAPPDATA\Microsoft\dotnet"
```

---

## 阶段 3：符号分析

通过 Il2CppDumper 的 `dump.cs` 找到以下关键类型：

### `Il2CppReloaded.Gameplay.GridItem`（数据层）

```csharp
// 主要字段
GridItemType   mGridItemType    // 类型，ScaryPot = 罐子
GridItemState  GridItemState    // 视觉状态（影响罐子外观）
ScaryPotType   mScaryPotType    // 内容类型：Seed / Zombie / Sun
SeedType       mSeedType        // 具体植物种类
ZombieType     mZombieType      // 具体僵尸种类
int            mSunCount        // 阳光数量
bool           mDead            // 是否已被砸碎

// GridItemState 枚举关键值
ScaryPotQuestion  // 问号罐（默认）
ScaryPotLeaf      // 绿叶罐（植物）
ScaryPotZombie    // 金色罐（僵尸）

// ScaryPotType 枚举
Seed / Zombie / Sun
```

### `Il2CppSource.Controllers.ScaryPotController`（视图层）

```csharp
GridItem                  m_gridItem        // 关联数据模型
PreviewDrawerController   m_previewDrawer   // 内容物预览渲染
GameObject                m_sunsContainer   // 阳光预览容器

// 相关方法
void DrawScaryPot()   // 每帧更新罐子视觉状态
void UpdateScaryPot() // 更新罐子逻辑
```

### `Il2CppSource.Controllers.PreviewDrawerController`

```csharp
void SetPreview(SeedType seedType, bool seedPacket)  // 展示植物预览图
void SetPreview(ZombieType zombieType)               // 展示僵尸预览图
void ClearPreview()                                  // 清除预览
```

---

## 阶段 4：方案实现记录

### 方案 A：PreviewDrawer 直接注入 ✅ 有效但有瑕疵

**思路**：在 `ScaryPotController.Update` 的 Postfix 中，直接调用 `PreviewDrawerController.SetPreview()` 将内容物图标渲染到罐子内部。

**核心代码**：
```csharp
[HarmonyPatch(typeof(ScaryPotController), nameof(ScaryPotController.Update))]
static class RevealPatch
{
    static void Postfix(ScaryPotController __instance)
    {
        var gi = __instance.m_gridItem;
        if (gi?.mGridItemType != GridItemType.ScaryPot || gi.mDead) return;

        switch (gi.mScaryPotType)
        {
            case ScaryPotType.Seed:
                __instance.m_previewDrawer.SetPreview(gi.mSeedType, false);
                break;
            case ScaryPotType.Zombie:
                __instance.m_previewDrawer.SetPreview(gi.mZombieType);
                break;
            case ScaryPotType.Sun:
                __instance.m_sunsContainer?.SetActive(true);
                break;
        }
    }
}
```

**结果**：  
✅ 植物/僵尸/阳光内容**可以看到**  
❌ 内容物图标被罐体遮挡，渲染层级在罐子图层**之下**，视觉效果差  
❌ 阳光罐会一直显示阳光容器（包括非阳光罐）

---

### 方案 B：GridItemState 驱动 ❌ 大部分罐子无内容显示

**思路**：游戏原生支持不同视觉状态的罐子（绿叶/金色），直接通过 Harmony Prefix 在 `GridItem.DrawScaryPot` 调用前强制设置对应状态，复用游戏内置渲染逻辑。

**核心代码**：
```csharp
[HarmonyPatch(typeof(GridItem), nameof(GridItem.DrawScaryPot))]
static class StatePatch
{
    static void Prefix(GridItem __instance)
    {
        if (__instance?.mGridItemType != GridItemType.ScaryPot || __instance.mDead) return;

        __instance.GridItemState = __instance.mScaryPotType switch
        {
            ScaryPotType.Seed    => GridItemState.ScaryPotLeaf,
            ScaryPotType.Zombie  => GridItemState.ScaryPotZombie,
            _                    => GridItemState.ScaryPotQuestion,
        };
    }
}
```

**结果**：  
✅ 少量罐子（约 2–3 个）正确显示为绿叶/金色样式  
❌ 大多数罐子仍显示为问号，效果几乎等于无  
❌ 不显示具体内容（只改变罐体颜色，无法知道具体是哪种植物/僵尸）

**推测原因**：`DrawScaryPot` 调用时机晚于游戏的状态初始化；或者 `GridItem.GridItemState` 属性设置后被游戏逻辑立即覆写；或者属性名/方法签名与实际 IL2CPP 符号存在偏差。

---

## 当前状态

**当前代码为方案 B（有问题版本）**。已知以下内容：

- 方案 A 效果**更可靠**，内容可见，但存在 z-order 遮挡问题
- 方案 B 理论上更优雅，但实现有 bug

---

## 待探索方向

### 方案 C：找到灯笼的原生透视逻辑

游戏内置的「灯笼」道具可以实现美观的透视效果（半透明罐体 + 内容物清晰可见）。  
目标：在 `dump.cs` 中找到 `BoardToolController` 或 `LanternBoardTool` 的相关方法，  
找到灯笼触发透视的具体 flag 或方法调用，直接复用。

**可能的关键 flag**：
- `GridItem.mScaryPotRevealed`（如果存在）
- 某个 `ScaryPotController` 上的 `IsRevealed` 属性

### 方案 D：修复方案 A 的 z-order

在 `SetPreview` 调用后，通过 Unity `Transform.SetAsLastSibling()` 或  
修改 `SpriteRenderer.sortingOrder` 将预览图渲染到罐体之上。

---

## 参考资料

- [MelonLoader Wiki](https://melonwiki.xyz/)
- [HarmonyX Docs](https://github.com/BepInEx/HarmonyX/wiki)
- [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
- [Re-Replanted](https://github.com/Lazy-Rabbit-2001/Re-Replanted)（社区逆向参考）
- 游戏符号文件：`dump.cs`（由 Il2CppDumper 生成，存放于 session files）
