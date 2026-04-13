# 🎮 放置江湖 - Idle Game

放置类挂机游戏，支持微信小程序平台。

## 📁 项目结构

```
IdleGame/
├── Assets/
│   └── Scripts/
│       ├── Core/               # 核心框架
│       ├── Player/             # 玩家系统
│       ├── Battle/             # 战斗系统
│       ├── Economy/            # 经济系统
│       ├── Ads/                # 广告系统
│       ├── Platform/           # 平台适配
│       ├── Equipment/          # 装备系统 🆕
│       ├── CheckIn/            # 签到系统 🆕
│       ├── Quest/              # 每日任务 🆕
│       ├── UI/                 # 界面系统
│       └── Save/               # 存档系统
│
├── numerical_tables/           # 数值平衡表 🆕
│   ├── Player_Level_Stats.csv
│   ├── Monster_Wave_Stats.csv
│   ├── Equipment_Stats.csv
│   └── Quest_Rewards.csv
│
├── Assets/Scripts/IdleGame.asmdef
├── README.md
├── CHANGELOG.md
├── GAME_DESIGN_DOC.md
├── NUMERICAL_BALANCE.md       # 数值设计文档 🆕
├── SETUP_GUIDE.md
└── .gitignore
```

## ✅ 已完成功能

| 功能 | 状态 |
|------|------|
| 自动战斗 | ✅ |
| 玩家升级 | ✅ |
| 金币经济 | ✅ |
| 离线收益 | ✅ |
| 激励视频 × 3 | ✅ |
| 微信支付 | ✅ |
| 自动存档 | ✅ |

## 🚀 快速开始

### 1. 安装Unity
下载 [Unity 2022 LTS](https://unity.com/download)

### 2. 创建项目
```
New Project → 2D Template → 项目名: IdleGame
```

### 3. 导入代码
复制 `Assets/Scripts/` 到你的Unity项目

### 4. 配置场景
按 [SETUP_GUIDE.md](SETUP_GUIDE.md) 创建UI场景

### 5. 运行测试
```
Unity Play按钮 → 测试核心玩法
```

## 📖 核心脚本说明

### GameManager.cs
游戏总控制器，管理所有子系统。

### BattleManager.cs
自动战斗逻辑，每秒执行一次伤害结算。

### EconomyManager.cs
金币管理，离线收益计算。

### AdManager.cs
激励视频广告控制（离线双倍/加速/额外金币）

### IAPManager.cs
应用内购买，商品管理。

## 💰 变现设计

- **激励视频**: 离线双倍、加速、额外金币
- **IAP**: 新手礼包、月卡、金币包、去除广告

## 📱 发布平台

- [x] 微信小程序 (WeChat MiniGame)
- [ ] 抖音小游戏
- [ ] 海外Web版

## 📝 文档

- [SETUP_GUIDE.md](SETUP_GUIDE.md) - Unity配置详细指南
- [GAME_DESIGN_DOC.md](GAME_DESIGN_DOC.md) - 游戏设计文档
- [CHANGELOG.md](CHANGELOG.md) - 更新日志

## 🔧 开发环境

- Unity: 2022 LTS+
- C#: 10.0
- 目标平台: WebGL / iOS / Android / 微信小程序

---

**License**: MIT
