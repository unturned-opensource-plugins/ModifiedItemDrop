# ModifiedItemDrop

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Unturned](https://img.shields.io/badge/Unturned-RocketMod-green.svg)](https://rocketmod.net)

**ModifiedItemDrop** v2 是一个面向 Unturned RocketMod 服务器的死亡物品结果控制插件，使用声明式 Outcome Rules、Durable Claim 持久化、safe/degraded mode、调试诊断以及热重载功能。

**作者**: FF,Emqo

## ✨ 核心功能

### 🎯 智能掉落控制
- **声明式 Outcome Rules**：通过 v2 XML 明确配置 `Drop` / `Keep` / `Delete` / `Grant`
- **Player Asset Outcome Model**：死亡处理先规划每个 Player Asset 的结果，再执行掉落、恢复或删除
- **规则解释**：`/mid rules preview` 和 `/mid rules explain` 直接展示 v2 Outcome Rules 决策

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
- **热重载**：`/mid config reload` 在不停机的情况下重新加载配置
- **自动热重载**：修改 XML 文件会被自动检测并重新加载
- **运行期调试**：`/mid rules preview` / `/mid rules explain` 查看 Outcome Rules 决策，`/mid inventory dump` 导出完整库存

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

#### 从 GitHub Release 安装
1. 下载 `ModifiedItemDrop-2.0.0.zip`。
2. 将 `ModifiedItemDrop.dll` 复制到服务器 `Rocket/Plugins/`。
3. 将 `Libraries/ModifiedItemDrop.Domain.dll` 复制到服务器 `Rocket/Libraries/`。
4. 将 `ModifiedItemDrop.configuration.xml` 复制到 `Rocket/Plugins/ModifiedItemDrop/`。
5. 启动服务器，或替换配置后在游戏中执行 `/mid config reload`。

#### 从源码构建后安装
1. 执行 `dotnet build -c Release`。
2. 将 `bin/Release/net48/ModifiedItemDrop.dll` 复制到服务器 `Rocket/Plugins/`。
3. 将 `bin/Release/net48/ModifiedItemDrop.Domain.dll` 复制到服务器 `Rocket/Libraries/`。
4. 将 `ModifiedItemDrop.configuration.xml` 复制到 `Rocket/Plugins/ModifiedItemDrop/`。
5. 启动服务器，或替换配置后在游戏中执行 `/mid config reload`。

> `ModifiedItemDrop.Domain.dll` 是 v2 的必需依赖，但它不是 Rocket 插件；不要放在 `Rocket/Plugins/`，否则 Rocket 会把它当插件扫描并输出 `Invalid or outdated plugin assembly`。

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

  <OutcomeRulesXml><![CDATA[
<OutcomeRules>
  <Rule name="Primary weapons drop" priority="100">
    <Target kind="Slot" slot="PrimaryWeapon" />
    <Outcome kind="Drop" chance="1.0" />
  </Rule>
  <Rule name="Default keep" priority="0">
    <Target kind="Any" />
    <Outcome kind="Keep" />
  </Rule>
</OutcomeRules>
  ]]></OutcomeRulesXml>
</ModifiedItemDropConfiguration>
```

> v2 不自动迁移 v1 `RuleSet`。升级前请阅读仓库中的 [`docs/migration/v1-to-v2-configuration.md`](docs/migration/v1-to-v2-configuration.md) 并手动改写为 Outcome Rules。GitHub Release 插件 zip 不包含 `docs/` 目录。

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

### OutcomeRulesXml 配置项

`OutcomeRulesXml` 是 v2 死亡处理的唯一规则入口。每条规则包含：

| 元素 | 说明 |
|------|------|
| `Rule name` | 规则名，会出现在 `/mid rules explain` 输出中 |
| `priority` | 优先级，高优先级先匹配；同优先级多条命中视为配置错误 |
| `Target` | `Any`、`Slot`、`Item`、`ClothingContent` |
| `Outcome` | `Drop`、`Keep`、`Delete`、`Grant` |
| `chance` | 仅适用于概率型 `Drop` / `Keep` |

v2 配置必须包含显式 catch-all 规则，例如 `Target kind="Any"`。

## 🎮 命令参考

| 命令 | 权限 | 说明 |
|------|------|------|
| `/mid config reload` | `modifieditemdrop.config.reload` | 热重载配置文件 |
| `/mid rules preview [player]` | `modifieditemdrop.rules.preview` | 预览玩家当前 Player Assets 的 v2 Outcome Rule 决策 |
| `/mid rules explain slot <PlayerAssetSlot>` | `modifieditemdrop.rules.explain` | 解释某个槽位目标会命中的规则、概率和最终 Outcome |
| `/mid rules explain item <itemId>` | `modifieditemdrop.rules.explain` | 解释某个 ItemID 目标会命中的规则、概率和最终 Outcome |
| `/mid inventory dump [player]` | `modifieditemdrop.inventory.dump` | 导出玩家完整库存信息 |
| `/mid claims list [player]` | `modifieditemdrop.claims.list` | 查看玩家待恢复的 v2 Durable Claims 摘要 |
| `/mid claims recover oldest` | `modifieditemdrop.claims.recover` | 领取最旧一条 v2 Durable Claim |
| `/mid claims recover all` | `modifieditemdrop.claims.recover` | 领取全部 v2 Durable Claims |
| `/mid diagnostics status` | `modifieditemdrop.diagnostics.status` | 查看 safe/degraded mode 与 Claim Recovery 状态 |
| `/mid diagnostics export` | `modifieditemdrop.diagnostics.export` | 输出非破坏性诊断信息和 v2 Claim storage 路径 |

> v1 flat commands `/mid reload`、`/mid preview`、`/mid dump`、`/mid claim` 在 v2 中不会成功执行，只会返回迁移提示。

## 🏗️ 项目架构

### 目录结构
```
ModifiedItemDrop/
├── Claim/                    # 持久化存储服务
│   ├── ClaimService.cs      # 业务逻辑
│   └── ClaimStorage.cs      # JSON 数据存储
├── Configuration/            # 配置系统
│   ├── ClaimSettings.cs     # 配置模型
│   ├── ConfigurationLoader.cs # v2 Outcome Rules 加载与 safe mode
│   └── ModifiedItemDropConfiguration.cs # 主配置
├── Drop/                    # 掉落逻辑
│   ├── DropService.cs       # v2 死亡处理编排
│   ├── V2DeathProcessingAdapter.cs
│   ├── V2QuickSlotExecutionAdapter.cs
│   ├── V2ClothingExecutionAdapter.cs
│   └── RestoreManager.cs    # 恢复 / Claim fallback 管理
├── Extensions/              # 扩展方法
│   └── PlayerExtensions.cs  # 玩家扩展
├── ModifiedItemDrop.Domain/ # 纯领域模型：Player Asset、Outcome Rules、Durable Claim
├── Models/                  # 运行时快照模型
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
├── docs/migration/v1-to-v2-configuration.md
├── ModifiedItemDrop.configuration.xml
└── README.md
```

### 核心工作流程

1. **初始化阶段** (`ModifiedItemDropPlugin`)
   - 初始化 DropService、v2 Durable Claim Store 和 Claim Recovery
   - 加载 Outcome Rules；无效配置进入 safe mode
   - 设置 FileSystemWatcher 监听配置变化

2. **玩家死亡** (`PlayerDeathHandler` → `DropService`)
   - 捕获玩家库存和衣物快照
   - 通过 v2 `DeathOutcomePlanner` 规划每个 Player Asset 的 `Drop` / `Keep` / `Delete` 结果
   - 根据 `DeathOutcomeExecutionPlan` 执行掉落、恢复或删除

3. **玩家复活** (`PlayerDeathHandler` → `DropService` → `ClaimService`)
   - 将 `Keep` Outcome 的物品恢复到玩家背包
   - 溢出或断线/卸载场景转入 v2 Durable Claim
   - v2 Claim 数据保存到 `claims/v2/claims.json`，并维护 backup/corrupt 诊断路径

4. **Claim 管理** (`ClaimService`)
   - 玩家上线时检查待领取物品并提示
   - 执行过期检查（删除或掉落到死亡位置）
   - 强制执行玩家 Claim 数量上限

### Outcome Rule 优先级系统

1. v2 按 `priority` 从高到低匹配规则。
2. 同一 Player Asset 在同一优先级命中多条规则会进入配置错误/safe mode。
3. 概率型 `Drop` / `Keep` 会记录 sampled roll，供 `/mid rules explain` 诊断。
4. 配置必须包含显式 catch-all 规则，避免隐式默认行为。

## 📊 数据存储

| 数据类型 | 存储位置 | 文件格式 |
|----------|----------|----------|
| v2 Durable Claim 数据 | `Rocket/Plugins/ModifiedItemDrop/claims/v2/claims.json` | JSON |
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
5. 使用 `/mid rules preview` 或 `/mid rules explain` 验证 Outcome Rules 决策

### 常见问题排查
- **规则不生效**：检查 `OutcomeRulesXml`、catch-all 规则和 `/mid diagnostics status`
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

Release 构建后，运行时需要同时部署：

```text
Rocket/Plugins/ModifiedItemDrop.dll
Rocket/Libraries/ModifiedItemDrop.Domain.dll
Rocket/Plugins/ModifiedItemDrop/ModifiedItemDrop.configuration.xml
```

## 📄 许可证

本项目采用 [MIT License](LICENSE) - 详见许可证文件。

## 🙏 致谢

- Unturned 社区
- RocketMod 团队
