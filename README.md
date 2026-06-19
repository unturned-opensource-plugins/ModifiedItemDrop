# ModifiedItemDrop

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Unturned](https://img.shields.io/badge/Unturned-RocketMod-green.svg)](https://rocketmod.net)

**ModifiedItemDrop** 是一个面向 Unturned RocketMod 服务器的高级掉落控制插件，支持分区掉落概率、衣物容器规则、持久化存储、调试日志以及热重载功能。

**作者**: FF,Emqo

## ✨ 核心功能

### 🎯 智能掉落控制
- **分区概率系统**：主武器、副武器、手持物品可分别设置掉落概率
- **衣物容器规则**：背包、马甲、衬衫、裤子、帽子、面罩、眼镜等槽位独立配置
- **自定义物品覆盖**：针对特定 ItemID 设置最高优先级的掉落概率

### 📦 持久化存储系统
- **服务器重启保留**：未领取的物品保存到 `claims.json`，重启后仍可领取
- **断线保护**：玩家断线时自动保存待发放物品
- **可配置过期时间**：默认 24 小时，支持设置永不过期
- **数量上限控制**：可限制每玩家最大 Claim 数量，超限时可选择删除最旧或拒绝新物品
- **上线通知**：玩家上线时自动提示待领取物品数量

### 🖐️ 手部槽位扩展
- **基于权限的手部槽位大小**：根据玩家权限动态调整手部槽位大小
- **玩家加入时应用**：玩家连接服务器时自动应用对应权限的槽位大小
- **复活时应用**：玩家死亡复活后重新应用槽位大小
- **灵活配置**：支持自定义多个权限等级，每个等级可设置不同的宽度和高度

### 🔧 调试与管理工具
- **详细调试日志**：输出 `[ModifiedItemDrop::Debug]`，包含概率来源、随机数与判定结果
- **热重载**：`/mid reload` 在不停机的情况下重新加载配置
- **自动热重载**：修改 XML 文件会被自动检测并重新加载
- **运行期调试**：`/mid preview` 查看玩家当前装备概率，`/mid dump` 导出完整库存

## 📋 环境要求

### 服务器要求
- Unturned Dedicated Server + RocketMod

### 开发要求
- .NET Framework 4.8 SDK
- C# 7.3 或更高版本

## 🚀 快速开始

### 1. 构建插件
```bash
dotnet build -c Release
```

### 2. 安装部署
1. 复制 `bin/Release/net48/ModifiedItemDrop.dll` 到服务器 `Rocket/Plugins/`
2. 复制 `ModifiedItemDrop.configuration.xml` 到 `Rocket/Plugins/ModifiedItemDrop/`
3. 启动服务器或在游戏中执行 `/mid reload`

### 3. 基础配置示例
```xml
<ModifiedItemDropConfiguration>
  <EnableDebugLogging>false</EnableDebugLogging>

  <ClaimSettings>
    <EnableClaim>true</EnableClaim>
    <MaxClaimsPerPlayer>10</MaxClaimsPerPlayer>
    <ExpirationMinutes>1440</ExpirationMinutes>
    <AutoClaimOnJoin>true</AutoClaimOnJoin>
  </ClaimSettings>

  <RuleSet>
    <GlobalDefaultChance>0.5</GlobalDefaultChance>
    <RegionChances>
      <RegionChanceEntry><Region>PrimaryWeapon</Region><Chance>0.3</Chance></RegionChanceEntry>
      <RegionChanceEntry><Region>SecondaryWeapon</Region><Chance>0.4</Chance></RegionChanceEntry>
      <RegionChanceEntry><Region>Hands</Region><Chance>0.0</Chance></RegionChanceEntry>
    </RegionChances>
    <ClothingRules>
      <ClothingSlot>
        <Slot>Backpack</Slot>
        <SlotDropChance>0.5</SlotDropChance>
        <ContentsDropChance>0.5</ContentsDropChance>
      </ClothingSlot>
    </ClothingRules>
  </RuleSet>
</ModifiedItemDropConfiguration>
```

## ⚙️ 配置详解

### ClaimSettings 配置项

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `EnableClaim` | `true` | 是否启用持久化功能 |
| `MaxClaimsPerPlayer` | `10` | 每玩家最大 Claim 数量，`0`=无限 |
| `ExpirationMinutes` | `1440` | 过期时间(分钟)，`0`=永不过期，`1440`=24小时 |
| `AutoClaimOnJoin` | `true` | 玩家上线时是否自动领取待发放物品 |
| `OverLimitBehavior` | `DeleteOldest` | 达到上限时：`DeleteOldest` / `DropToGround` |
| `ExpirationBehavior` | `Delete` | 过期时：`Delete` / `DropAtDeathPosition` |

### DropRuleSet 配置项

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `GlobalDefaultChance` | `double` | 全局默认掉落概率 (0.0-1.0) |
| `RegionChances` | `List<RegionChanceEntry>` | 分区掉落概率配置 |
| `CustomItemChances` | `List<ItemChanceEntry>` | 自定义物品掉落概率 |
| `ClothingRules` | `List<ClothingSlotRule>` | 衣物槽位规则配置 |

### RegionChanceEntry 配置项

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `Region` | `string` | 槽位类型：`PrimaryWeapon`, `SecondaryWeapon`, `Hands`, `Inventory` |
| `Chance` | `double` | 掉落概率 (0.0-1.0) |

### ClothingSlotRule 配置项

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `Slot` | `string` | 衣物槽位：`Backpack`, `Vest`, `Shirt`, `Pants`, `Hat`, `Mask`, `Glasses` |
| `SlotDropChance` | `double` | 衣物本身的掉落概率 (0.0-1.0) |
| `ContentsDropChance` | `double` | 衣物内物品的掉落概率 (0.0-1.0) |

## 🎮 命令参考

| 命令 | 权限 | 说明 |
|------|------|------|
| `/mid reload` | `modifieditemdrop.reload` | 热重载配置文件 |
| `/mid preview [player]` | `modifieditemdrop.preview` | 查看玩家物品掉落概率 |
| `/mid dump [player]` | `modifieditemdrop.preview` | 导出玩家完整库存信息 |
| `/mid claim` | `modifieditemdrop.claim` | 领取待发放物品 |

## 🏗️ 项目架构

### 目录结构
```
ModifiedItemDrop/
├── Claim/                    # 持久化存储服务
│   ├── ClaimService.cs      # 业务逻辑
│   └── ClaimStorage.cs      # JSON 数据存储
├── Configuration/            # 配置系统
│   ├── ClaimSettings.cs     # 配置模型
│   ├── ConfigurationLoader.cs # 配置加载器
│   ├── DropRuleSet.cs       # 掉落规则集
│   └── ModifiedItemDropConfiguration.cs # 主配置
├── Drop/                    # 掉落逻辑
│   ├── ChanceResolver.cs    # 概率解析器
│   ├── DropService.cs       # 掉落服务
│   ├── InventoryProcessor.cs # 背包处理
│   ├── ClothingProcessor.cs # 衣物处理
│   └── RestoreManager.cs    # 恢复管理
├── Extensions/              # 扩展方法
│   └── PlayerExtensions.cs  # 玩家扩展
├── Models/                  # 数据模型
│   ├── ClaimRecord.cs       # 持久化记录
│   ├── ClothingItemSnapshot.cs
│   ├── InventoryItemSnapshot.cs
│   └── SlotType.cs
├── Plugin/                  # 插件核心
│   ├── ModifiedItemDropPlugin.cs # 入口
│   ├── PlayerDeathHandler.cs    # 事件处理
│   └── ReloadConfigCommand.cs   # 命令处理
├── Utilities/               # 工具类
│   ├── UtilityHelper.cs
│   ├── LoggingHelper.cs
│   └── ClothingOperationHelper.cs
├── ModifiedItemDrop.configuration.xml
└── README.md
```

### 核心工作流程

1. **初始化阶段** (`ModifiedItemDropPlugin`)
   - 初始化 DropService 和 ClaimService
   - 设置 FileSystemWatcher 监听配置变化

2. **玩家死亡** (`PlayerDeathHandler` → `DropService`)
   - 捕获玩家库存和衣物快照
   - 通过 `ChanceResolver` 查询每个物品的掉落概率
   - 根据概率决定掉落或保留物品

3. **玩家复活** (`PlayerDeathHandler` → `DropService` → `ClaimService`)
   - 将保留物品恢复到玩家背包
   - 溢出物品保存到 ClaimService
   - 生成 ClaimRecord 持久化到 claims.json

4. **Claim 管理** (`ClaimService`)
   - 玩家上线时检查待领取物品并提示
   - 执行过期检查（删除或掉落到死亡位置）
   - 强制执行玩家 Claim 数量上限

### 概率优先级系统

**武器槽（Page 0-2）优先级（从高到低）：**
1. 配置中的 ItemID 特定概率 (`CustomItemChances`)
2. 配置中的分区概率 (`RegionChances`)
3. 全局默认概率 (`GlobalDefaultChance`)

**衣物内容物（Page 3-6）处理方式：**
- 完全由 `ClothingRules` 控制
- 不受 `GlobalDefaultChance` 和 `RegionChances` 影响
- 根据 `ContentsDropChance` 决定掉落概率

## 📊 数据存储

| 数据类型 | 存储位置 | 文件格式 |
|----------|----------|----------|
| Claim 数据 | `Rocket/Plugins/ModifiedItemDrop/claims.json` | JSON |
| 配置文件 | `Rocket/Plugins/ModifiedItemDrop/ModifiedItemDrop.configuration.xml` | XML |

## 🧪 开发与测试

### 调试配置
```xml
<EnableDebugLogging>true</EnableDebugLogging>
<EnableClothingContentsDebugLogging>true</EnableClothingContentsDebugLogging>
```

### 测试流程
1. 启用调试日志
2. 使用 `/give <itemID>` 添加测试物品
3. 使用 `/kill` 触发死亡测试
4. 检查服务器控制台 `[ModifiedItemDrop::Debug]` 日志
5. 使用 `/mid preview` 验证概率计算

### 常见问题排查
- **概率不生效**：检查配置文件路径和 XML 格式
- **Claim 不工作**：检查 `EnableClaim` 设置和文件权限
- **自动重载不工作**：检查文件系统权限

## 📦 构建与发布

### 构建命令
```bash
# Release 构建（推荐）
dotnet build -c Release

# Debug 构建（开发）
dotnet build -c Debug
```

## 📄 许可证

本项目采用 [MIT License](LICENSE) - 详见许可证文件。

## 🙏 致谢

- Unturned 社区
- RocketMod 团队
