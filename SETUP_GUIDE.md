# 放置游戏 - Unity项目配置指南

## 📋 Unity项目配置步骤

### 第一步：创建Unity项目

1. 下载安装 [Unity Hub](https://unity.com/download)
2. 安装 Unity 2022 LTS（或更高版本）
3. 创建新项目：
   - 点击 **New Project**
   - 选择 **2D** 模板
   - 输入项目名称：`IdleGame`
   - 点击 **Create Project**

### 第二步：导入脚本

1. 复制 `Assets/Scripts/` 文件夹到你的Unity项目
2. 或直接在Unity中创建对应脚本文件，粘贴代码

### 第三步：导入必要插件

在 Unity Package Manager 中安装：

1. **TextMeshPro**（已内置）
   - Window → TextMeshPro → Import TMP Essential Resources

2. **微信小游戏SDK**（发布时需要）
   - 从微信官方下载
   - 导入：Assets → Import Package → Custom Package

### 第四步：创建场景

#### 1. 创建空场景
```
File → New Scene → Basic (Built-in)
保存为 MainScene
```

#### 2. 创建GameManager空物体
```
Hierarchy → 右键 → Create Empty
命名：GameManager
```

### 第五步：挂载脚本

在 GameManager 对象上，按顺序添加组件：

```
GameManager (空物体)
├── GameManager.cs          ← 拖入
├── PlayerManager.cs        ← 拖入
├── BattleManager.cs        ← 拖入
├── EconomyManager.cs       ← 拖入
├── AdManager.cs           ← 拖入
├── IAPManager.cs          ← 拖入
├── UIManager.cs           ← 拖入
└── SaveManager.cs         ← 拖入
```

**注意：Unity会自动添加同名脚本，如已有则跳过。**

### 第六步：创建UI

#### 1. 创建Canvas
```
Hierarchy → 右键 → UI → Canvas
设置：Render Mode = Screen Space - Overlay
```

#### 2. 创建UI元素层级

```
Canvas
├── TopPanel (Panel)
│   ├── GoldText          (TextMeshPro) - "金币: 0"
│   ├── LevelText         (TextMeshPro) - "等级 1"
│   ├── WaveText          (TextMeshPro) - "波次 1"
│   └── DPSPanel (Panel)
│       └── DPSInfo       (TextMeshPro) - "DPS: 0"
│
├── MonsterPanel (Panel)
│   ├── MonsterNameText   (TextMeshPro) - 怪物名称
│   ├── MonsterHealthBar  (Image/Slider) - 血条
│   └── MonsterHealthText (TextMeshPro) - "100 / 100"
│
├── CenterPanel (Panel) - 战斗/奖励信息
│   ├── BattleLogText     (TextMeshPro)
│   └── RewardPopup       (Panel)
│       ├── RewardText    (TextMeshPro)
│       └── RewardIcon    (Image)
│
├── BottomPanel (Panel)
│   ├── UpgradeButton     (Button)
│   │   └── Text         (TextMeshPro) - "升级\n100金币"
│   │
│   ├── AdButtonsPanel    (Panel)
│   │   ├── OfflineDoubleBtn (Button)
│   │   ├── SpeedUpBtn      (Button)
│   │   └── ExtraGoldBtn     (Button)
│   │
│   └── ShopButton       (Button)
│
└── ShopPanel (Panel) - 商店弹窗（初始隐藏）
    ├── ShopTitle        (TextMeshPro)
    ├── ProductList      (Scroll View)
    │   └── ProductItem  (重复模板)
    └── CloseButton      (Button)
```

### 第七步：绑定UI到UIManager

1. 选中 GameManager 对象
2. 找到 UIManager 组件
3. 将Hierarchy中的UI元素拖入对应字段

或在 GameUI.cs 中已自动查找：
```
GameObject.Find("GoldText") 等
```

### 第八步：配置微信发布

#### 1. 下载微信开发者工具
https://developers.weixin.qq.com/miniprogram/dev/devtools/download.html

#### 2. Unity 导出微信小游戏
```
File → Build Settings
选择 WeChat MiniGame 平台
点击 Switch Platform
点击 Build
```

#### 3. 使用微信开发者工具打开
- 导出后用微信工具打开
- 填入 AppID
- 配置广告位ID（在 WeChatSDK.cs 中替换）

### 第九步：测试运行

#### 编辑器测试
1. 点击 Unity Play 按钮
2. 观察 Console 输出
3. 检查UI是否正常更新

#### 常见问题

**Q: 报错 "MissingReferenceException"**
```
A: 检查所有UI组件是否正确绑定
```

**Q: 激励视频无法播放**
```
A: 微信SDK未正确初始化，需真机测试
```

**Q: 金币不增加**
```
A: 检查 BattleManager 是否正确调用
```

---

## 🎮 游戏玩法

1. **自动战斗**：玩家自动攻击怪物
2. **升级**：点击升级按钮消耗金币
3. **离线收益**：关闭游戏后重新打开可领取离线金币
4. **激励视频**：
   - 离线双倍领取
   - 加速冷却
   - 额外金币

---

## 💰 商品配置

在 IAPManager.cs 中配置商品：

```csharp
_products.Add(new IAPProduct
{
    id = "gold_pack_small",
    name = "金币小礼包",
    description = "获得10000金币",
    price = 0.99m,
    priceString = "$0.99",
    type = IAPProductType.Consumable
});
```

---

## 📱 发布检查清单

- [ ] Unity项目无报错
- [ ] 所有UI正确显示
- [ ] 存档功能正常
- [ ] 微信SDK已接入
- [ ] 广告位ID已配置
- [ ] 支付ID已配置
- [ ] 隐私政策已添加
- [ ] 游戏图标已设置
- [ ] 版本号已更新

---

## 🔧 调试技巧

### 编辑器日志
```
Debug.Log("[BattleManager] Player attacked!");
```

### 真机调试
1. 微信开发者工具开启调试
2. 使用 vconsole 查看日志

### 性能优化
- 减少UI更新频率（每秒最多更新1次）
- 使用对象池管理怪物
- 禁用不需要的Unity组件

---

## 📞 微信SDK接入帮助

官方文档：https://developers.weixin.qq.com/miniprogram/dev/game/

### 广告接入示例
```csharp
// 显示激励视频
WeChatSDK.Instance.ShowRewardedVideo((success) => {
    if (success) {
        // 发放奖励
    }
});
```

### 支付接入示例
```csharp
// 购买金币
IAPManager.Instance.Purchase("gold_pack_small", (success) => {
    if (success) {
        Debug.Log("购买成功！");
    }
});
```
