# Phase 3 UI/UX 开发计划

## 一、Unity场景结构

### 1.1 Canvas层级规划
```
MainCanvas (Screen Space - Overlay)
├── TopStatusBar          # 顶部状态栏（常驻）
│   ├── GoldDisplay        # 金币 + 图标
│   ├── LevelDisplay       # 等级
│   ├── WaveDisplay        # 当前波次
│   └── DPSDisplay         # DPS数值
│
├── MonsterArea           # 怪物区域（中央偏上）
│   ├── MonsterNameText
│   ├── MonsterHealthBar   # 血条（Slider）
│   ├── MonsterLevelBadge
│   └── MonsterHPText
│
├── DamagePopupPool       # 伤害数字对象池（10个预制体）
├── RewardNotify          # 击杀/升级提示（动画弹出）
│
├── BottomActionBar       # 底部功能栏（常驻）
│   ├── UpgradeButton      # 升级按钮
│   ├── AdButtonGroup      # 激励视频组
│   │   ├── OfflineDoubleBtn
│   │   ├── SpeedUpBtn
│   │   └── ExtraGoldBtn
│   └── MoreMenuBtn        # 更多菜单
│
├── MainMenuBar           # 底部导航（图标菜单）
│   ├── EquipBtn           # 装备
│   ├── CheckInBtn         # 签到
│   ├── QuestBtn           # 任务
│   └── ShopBtn            # 商店
│
├── EquipmentPanel        # 装备面板（隐藏）
├── CheckInPanel          # 签到面板（隐藏）
├── QuestPanel            # 任务面板（隐藏）
├── ShopPanel             # 商店面板（隐藏）
├── SettingsPopup         # 设置弹窗（隐藏）
└── RewardPopup           # 奖励详情弹窗（隐藏）
```

### 1.2 Canvas配置
```
Canvas:
  Render Mode: Screen Space - Overlay
  Canvas Scaler:
    - Reference Resolution: 1080 x 1920
    - Match: 0.5 (width match height)
  UI Scale Mode: Scale With Screen Size
```

---

## 二、UI组件绑定方案

### 2.1 TopStatusBar（顶部状态栏）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| GoldIcon | Image | GameUI | 静态图标 |
| GoldText | TextMeshProUGUI | GameUI.UpdateGold() | 格式化显示(K/M/B) |
| LevelText | TextMeshProUGUI | GameUI.UpdateLevel() | "等级 N" |
| WaveText | TextMeshProUGUI | GameUI.UpdateWave() | "波次 N" |
| DPSText | TextMeshProUGUI | GameUI.UpdateDPS() | "DPS: X" |
| OfflineTimeText | TextMeshProUGUI | GameUI.UpdateOfflineTime() | "离线X小时" |

### 2.2 MonsterArea（怪物区域）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| MonsterPortrait | Image | GameUI.UpdateMonster() | 怪物头像Sprite |
| MonsterNameText | TextMeshProUGUI | GameUI.UpdateMonster() | 怪物名称 |
| MonsterHealthBar | Slider | GameUI.UpdateMonsterHP() | 0~1 fill |
| MonsterHPText | TextMeshProUGUI | GameUI.UpdateMonsterHP() | "X / Y" |
| MonsterLevelBadge | TextMeshProUGUI | GameUI.UpdateMonster() | "Lv.X" |

### 2.3 BottomActionBar（底部功能栏）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| UpgradeButton | Button | GameUI / UIManager | OnClick → PlayerManager.TryUpgrade() |
| UpgradeCostText | TextMeshProUGUI | GameUI.UpdateUpgradeButton() | "升级\nN金币" |
| UpgradeEffectIcon | Image | GameUI | 可升级时闪烁动画 |
| OfflineDoubleBtn | Button | GameUI | OnClick → AdManager.ShowRewardedVideo(OfflineDouble) |
| OfflineDoubleCostText | TextMeshProUGUI | GameUI | "看广告双倍" |
| SpeedUpBtn | Button | GameUI | OnClick → AdManager.ShowRewardedVideo(SpeedUp) |
| SpeedUpCostText | TextMeshProUGUI | GameUI | "看广告加速" |
| ExtraGoldBtn | Button | GameUI | OnClick → AdManager.ShowRewardedVideo(ExtraGold) |
| ExtraGoldCostText | TextMeshProUGUI | GameUI | "看广告领金币" |
| MoreMenuBtn | Button | GameUI | OnClick → OpenSettingsPopup() |

### 2.4 MainMenuBar（底部导航）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| EquipBtn | Button | GameUI.OpenEquipmentPanel() | OnClick |
| EquipBadge | Image | GameUI.UpdateEquipBadge() | 新装备时红点 |
| CheckInBtn | Button | GameUI.OpenCheckInPanel() | OnClick |
| CheckInBadge | Image | GameUI.UpdateCheckInBadge() | 未签到时红点 |
| QuestBtn | Button | GameUI.OpenQuestPanel() | OnClick |
| QuestBadge | Image | GameUI.UpdateQuestBadge() | 可领取时红点 |
| ShopBtn | Button | GameUI.OpenShopPanel() | OnClick |

### 2.5 EquipmentPanel（装备面板）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| WeaponSlot | Button/Image | EquipmentUI | 点击穿戴/卸下 |
| ArmorSlot | Button/Image | EquipmentUI | 点击穿戴/卸下 |
| AccessorySlot | Button/Image | EquipmentUI | 点击穿戴/卸下 |
| WeaponDetail | Panel | EquipmentUI | 显示详情+强化按钮 |
| InventoryGrid | GridLayoutGroup | EquipmentUI | 背包内所有装备 |
| EquipmentItem | Button/Image | EquipmentUI | 点击选中 |
| UpgradeBtn | Button | EquipmentUI.UpgradeEquipment() | OnClick |
| SellBtn | Button | EquipmentUI.SellEquipment() | OnClick |
| ATKBonusText | TextMeshProUGUI | EquipmentUI.UpdateStats() | 攻击加成 |
| HPBonusText | TextMeshProUGUI | EquipmentUI.UpdateStats() | 生命加成 |
| CritRateText | TextMeshProUGUI | EquipmentUI.UpdateStats() | 暴击率 |
| CritDamageText | TextMeshProUGUI | EquipmentUI.UpdateStats() | 暴击伤害 |

### 2.6 CheckInPanel（签到面板）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| Day1~Day7 | Button/Toggle | CheckInUI | OnClick → Claim() |
| Day1Icon, etc. | Image | CheckInUI | 已领取变灰/金色 |
| StreakText | TextMeshProUGUI | CheckInUI.UpdateStreak() | "连续签到 X 天" |
| ClaimFreeBtn | Button | CheckInUI.CheckIn() | 免费签到 |
| ClaimAdBtn | Button | CheckInUI.CheckInWithAd() | 看广告补签 |
| RewardPreview | TextMeshProUGUI | CheckInUI.ShowRewardPreview() | 当日奖励预览 |

### 2.7 QuestPanel（任务面板）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| QuestList | ScrollView | QuestUI | 显示5个任务 |
| Quest1~5 | Button/Panel | QuestUI | 任务项 |
| QuestTitleText | TextMeshProUGUI | QuestUI.UpdateQuest() | 任务名称 |
| QuestProgressBar | Slider | QuestUI.UpdateProgress() | 进度条 |
| QuestProgressText | TextMeshProUGUI | QuestUI.UpdateProgress() | "X / Y" |
| QuestRewardIcon | Image | QuestUI | 奖励图标 |
| QuestRewardAmount | TextMeshProUGUI | QuestUI | 奖励数量 |
| ClaimRewardBtn | Button | QuestUI.ClaimReward() | 领取按钮 |

### 2.8 ShopPanel（商店面板）

| UI组件 | 类型 | 绑定脚本 | 事件/属性 |
|--------|------|----------|-----------|
| ProductList | ScrollView | ShopUI | IAP商品列表 |
| ProductItem | Button/Panel | ShopUI | 商品项 |
| ProductIcon | Image | ShopUI.UpdateProduct() | 商品图标 |
| ProductNameText | TextMeshProUGUI | ShopUI.UpdateProduct() | 商品名称 |
| ProductPriceText | TextMeshProUGUI | ShopUI.UpdateProduct() | 价格 |
| ProductDescText | TextMeshProUGUI | ShopUI.UpdateProduct() | 描述 |
| BuyBtn | Button | ShopUI.Purchase() | 购买按钮 |

---

## 三、动画/特效方案

### 3.1 推荐工具
- **DOTween**（推荐）：轻量、链式API、序列动画
- **Unity Animator**：适合复杂状态机（如角色动画）
- **Particle System**：伤害特效、金币雨、强化光效
- **LeanTween**：备选（更轻量）

### 3.2 动画清单

| 动画名称 | 触发时机 | 效果 | 工具 |
|----------|----------|------|------|
| DamagePopup | 每次造成伤害 | 数字飘起+消失，向上飘动 | DOTween / 对象池 |
| CritPopup | 暴击时 | 红色数字 + 屏幕轻微震动 | DOTween |
| GoldEarned | 获得金币 | 金币图标飞向顶部状态栏 | DOTween Path |
| LevelUpEffect | 升级时 | 升级弹窗 + 光效 + 粒子 | DOTween + ParticleSystem |
| WaveClearEffect | 波次切换 | 屏幕闪光 + "波次N"大字 | DOTween |
| ButtonClickScale | 按钮点击 | 缩放0.9→1.0动画 | DOTween |
| HealthBarShake | 受到攻击 | 血条抖动 | DOTween |
| MonsterDeath | 怪物死亡 | 爆炸粒子 + 渐隐 | ParticleSystem |
| UpgradeGlow | 升级按钮可点 | 按钮边框发光脉冲 | DOTween |
| PanelSlideIn | 打开面板 | 从下往上滑入 | DOTween |
| PanelSlideOut | 关闭面板 | 向上滑出 | DOTween |
| BadgePulse | 红点提示 | 小圆点脉冲动画 | DOTween |
| RewardPop | 弹窗出现 | 缩放0→1.1→1弹性 | DOTween |
| FloatingIdle | 界面元素 | 轻微上下浮动（UI装饰） | DOTween循环 |

### 3.3 粒子特效

| 特效名称 | 位置 | 参数 |
|----------|------|------|
| GoldCoinBurst | 击杀怪物时 | 5-10个金币粒子，向四周散开 |
| UpgradeSparkle | 升级时 | 中心爆发，多彩粒子 |
| HealSparkle | 生命恢复时 | 绿色上升粒子 |
| EquipGlow | 穿戴装备时 | 装备槽位发光 |
|强化光柱 | 强化成功时 | 从下往上光柱 |

---

## 四、需补充的C#脚本

### 4.1 现有脚本改进
- `UIManager.cs`：增加面板开关方法，改进事件绑定
- `GameUI.cs`：完善所有Update方法，添加面板切换逻辑

### 4.2 新增脚本

| 脚本 | 职责 | 优先级 |
|------|------|--------|
| `UIPanelController.cs` | 管理所有Panel的显示/隐藏、动画切换 | P0 |
| `DamagePopupManager.cs` | 伤害数字对象池管理 | P0 |
| `DOTweenAnimations.cs` | 封装常用DOTween动画 | P0 |
| `SoundManager.cs` | 音效/BGM管理 | P1 |
| `ParticleEffectManager.cs` | 粒子特效播放管理 | P1 |
| `EquipmentUI.cs` | 装备界面逻辑 | P1 |
| `CheckInUI.cs` | 签到界面逻辑 | P1 |
| `QuestUI.cs` | 任务界面逻辑 | P1 |
| `ShopUI.cs` | 商店界面逻辑 | P1 |
| `NotificationBadge.cs` | 红点badge管理 | P2 |

---

## 五、开发顺序

### Phase 3.1（P0 - 基础框架）
1. 创建Unity场景和Canvas结构（按1.1节）
2. 实现`UIPanelController.cs` - 面板开关管理
3. 实现`DOTweenAnimations.cs` - 动画封装
4. 实现`DamagePopupManager.cs` - 伤害数字对象池
5. 完善`UIManager.cs`事件绑定
6. 完善`GameUI.cs`所有Update方法

### Phase 3.2（P1 - 核心UI）
7. 装备面板 `EquipmentUI.cs`
8. 签到面板 `CheckInUI.cs`
9. 任务面板 `QuestUI.cs`
10. 商店面板 `ShopUI.cs`

### Phase 3.3（P1 - 动画/音效）
11. 所有动画效果实现
12. 音效系统 `SoundManager.cs`
13. 粒子特效 `ParticleEffectManager.cs`

### Phase 3.4（P2 - 打磨）
14. 红点通知系统
15. 界面过渡动画
16. Loading屏幕

---

## 六、关键实现细节

### 6.1 面板切换动画示例（DOTween）
```csharp
// 面板滑入
RectTransform.DOAnchorPosY(0, 0.3f).SetEase(Ease.OutBack);

// 面板滑出
RectTransform.DOAnchorPosY(1500, 0.3f).SetEase(Ease.InBack);
```

### 6.2 伤害数字对象池
```csharp
// 预制10个DamagePopup
// 每次显示从池中取，动画结束后回收
// 位置：怪物头顶 + 随机X偏移
```

### 6.3 按钮状态管理
```csharp
// 升级按钮：金币足够时绿色，可点击；不足时灰色，禁用
// 激励视频按钮：冷却中显示倒计时，可用时高亮
```

### 6.4 红点Badge规则
```
EquipBadge:    背包有新装备时显示
CheckInBadge:  今日未签到时显示
QuestBadge:    有可领取奖励时显示
```
