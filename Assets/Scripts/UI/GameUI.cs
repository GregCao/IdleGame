using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using IdleGame;

/// <summary>
/// 游戏UI总控制器 - 统一管理所有UI组件和面板
/// 挂载在 Canvas 根节点上
/// 
/// Phase 3.1 改进：
/// - 完善所有 Update 方法
/// - 集成 UIPanelController 面板管理
/// - 集成 DOTweenAnimations 动画系统
/// - 集成 DamagePopupManager 伤害数字系统
/// </summary>
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    #region === SerializeField 组件引用 ===

    [Header("=== 状态栏 ===")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _waveText;
    [SerializeField] private TextMeshProUGUI _dpsText;
    [SerializeField] private TextMeshProUGUI _offlineTimeText;

    [Header("=== 怪物区域 ===")]
    [SerializeField] private TextMeshProUGUI _monsterNameText;
    [SerializeField] private Slider _monsterHealthBar;
    [SerializeField] private TextMeshProUGUI _monsterHealthText;
    [SerializeField] private TextMeshProUGUI _monsterLevelBadge;

    [Header("=== 升级按钮 ===")]
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TextMeshProUGUI _upgradeCostText;
    [SerializeField] private GameObject _upgradeEffectGlow;  // 可升级时发光

    [Header("=== 激励视频按钮 ===")]
    [SerializeField] private Button _offlineDoubleButton;
    [SerializeField] private TextMeshProUGUI _offlineDoubleLabel;
    [SerializeField] private Image _offlineDoubleCooldown;

    [SerializeField] private Button _speedUpButton;
    [SerializeField] private TextMeshProUGUI _speedUpLabel;
    [SerializeField] private Image _speedUpCooldown;

    [SerializeField] private Button _extraGoldButton;
    [SerializeField] private TextMeshProUGUI _extraGoldLabel;
    [SerializeField] private Image _extraGoldCooldown;

    [Header("=== 底部导航 ===")]
    [SerializeField] private Button _equipButton;
    [SerializeField] private Image _equipBadge;
    [SerializeField] private Button _checkInButton;
    [SerializeField] private Image _checkInBadge;
    [SerializeField] private Button _questButton;
    [SerializeField] private Image _questBadge;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _settingsButton;

    [Header("=== 面板引用 ===")]
    [SerializeField] private GameObject _equipmentPanel;
    [SerializeField] private GameObject _checkInPanel;
    [SerializeField] private GameObject _questPanel;
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private GameObject _settingsPopup;

    [Header("=== 弹窗 ===")]
    [SerializeField] private GameObject _rewardPopup;
    [SerializeField] private TextMeshProUGUI _rewardText;

    [Header("=== 颜色配置 ===")]
    [SerializeField] private Color _canAffordColor = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color _cannotAffordColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color _goldColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color _critColor = new Color(1f, 0.3f, 0.3f);

    #endregion

    #region === 私有变量 ===

    // 面板控制
    private UIPanelController _panelController;
    private string _currentOpenPanel = null;

    // 动画状态
    private bool _isUpgradeGlowActive = false;
    private float _lastGold = 0;

    // 冷却状态
    private float _offlineDoubleCooldownEnd = 0;
    private float _speedUpCooldownEnd = 0;
    private float _extraGoldCooldownEnd = 0;

    // 离线奖励回调
    private Action _offlineDoubleCallback;

    // 待显示的伤害队列（防止伤害数字太多重叠）
    private readonly Queue<Action> _pendingDamageQueue = new Queue<Action>();
    private const float DamageQueueInterval = 0.05f;
    private float _lastDamageShowTime = 0;

    #endregion

    #region === Unity 生命周期 ===

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _panelController = FindObjectOfType<UIPanelController>();
        CacheComponents();
    }

    private void Start()
    {
        Initialize();
        SubscribeEvents();
        RefreshAllUI();
    }

    private void Update()
    {
        UpdateCooldownUI();
        UpdateUpgradeGlow();
        UpdateDamageQueue();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region === 初始化 ===

    private void CacheComponents()
    {
        Transform canvas = transform;

        // 状态栏
        _goldText = FindComponent<TextMeshProUGUI>(canvas, "TopStatusBar/GoldText");
        _levelText = FindComponent<TextMeshProUGUI>(canvas, "TopStatusBar/LevelText");
        _waveText = FindComponent<TextMeshProUGUI>(canvas, "TopStatusBar/WaveText");
        _dpsText = FindComponent<TextMeshProUGUI>(canvas, "TopStatusBar/DPSInfo");
        _offlineTimeText = FindComponent<TextMeshProUGUI>(canvas, "TopStatusBar/OfflineTimeText");

        // 怪物区域
        _monsterNameText = FindComponent<TextMeshProUGUI>(canvas, "MonsterArea/MonsterNameText");
        _monsterHealthBar = FindComponent<Slider>(canvas, "MonsterArea/MonsterHealthBar");
        _monsterHealthText = FindComponent<TextMeshProUGUI>(canvas, "MonsterArea/MonsterHealthText");
        _monsterLevelBadge = FindComponent<TextMeshProUGUI>(canvas, "MonsterArea/MonsterLevelBadge");

        // 升级按钮
        _upgradeButton = FindComponent<Button>(canvas, "BottomActionBar/UpgradeButton");
        _upgradeCostText = FindComponent<TextMeshProUGUI>(canvas, "BottomActionBar/UpgradeButton/Text");
        _upgradeEffectGlow = FindGameObject(canvas, "BottomActionBar/UpgradeButton/EffectGlow");

        // 激励视频按钮
        _offlineDoubleButton = FindComponent<Button>(canvas, "BottomActionBar/AdButtonsPanel/OfflineDoubleButton");
        _offlineDoubleLabel = FindComponent<TextMeshProUGUI>(canvas, "BottomActionBar/AdButtonsPanel/OfflineDoubleButton/Label");
        _offlineDoubleCooldown = FindComponent<Image>(canvas, "BottomActionBar/AdButtonsPanel/OfflineDoubleButton/Cooldown");

        _speedUpButton = FindComponent<Button>(canvas, "BottomActionBar/AdButtonsPanel/SpeedUpButton");
        _speedUpLabel = FindComponent<TextMeshProUGUI>(canvas, "BottomActionBar/AdButtonsPanel/SpeedUpButton/Label");
        _speedUpCooldown = FindComponent<Image>(canvas, "BottomActionBar/AdButtonsPanel/SpeedUpButton/Cooldown");

        _extraGoldButton = FindComponent<Button>(canvas, "BottomActionBar/AdButtonsPanel/ExtraGoldButton");
        _extraGoldLabel = FindComponent<TextMeshProUGUI>(canvas, "BottomActionBar/AdButtonsPanel/ExtraGoldButton/Label");
        _extraGoldCooldown = FindComponent<Image>(canvas, "BottomActionBar/AdButtonsPanel/ExtraGoldButton/Cooldown");

        // 底部导航
        _equipButton = FindComponent<Button>(canvas, "MainMenuBar/EquipBtn");
        _checkInButton = FindComponent<Button>(canvas, "MainMenuBar/CheckInBtn");
        _questButton = FindComponent<Button>(canvas, "MainMenuBar/QuestBtn");
        _shopButton = FindComponent<Button>(canvas, "MainMenuBar/ShopBtn");
        _settingsButton = FindComponent<Button>(canvas, "BottomActionBar/MoreMenuBtn");

        _equipBadge = FindComponent<Image>(canvas, "MainMenuBar/EquipBtn/Badge");
        _checkInBadge = FindComponent<Image>(canvas, "MainMenuBar/CheckInBtn/Badge");
        _questBadge = FindComponent<Image>(canvas, "MainMenuBar/QuestBtn/Badge");

        // 面板
        _equipmentPanel = FindGameObject(canvas, "EquipmentPanel");
        _checkInPanel = FindGameObject(canvas, "CheckInPanel");
        _questPanel = FindGameObject(canvas, "QuestPanel");
        _shopPanel = FindGameObject(canvas, "ShopPanel");
        _settingsPopup = FindGameObject(canvas, "SettingsPopup");

        // 弹窗
        _rewardPopup = FindGameObject(canvas, "RewardPopup");
        _rewardText = FindComponent<TextMeshProUGUI>(canvas, "RewardPopup/RewardText");

        Debug.Log("[GameUI] Components cached.");
    }

    private void Initialize()
    {
        // 按钮事件绑定
        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

        if (_offlineDoubleButton != null)
            _offlineDoubleButton.onClick.AddListener(OnOfflineDoubleClicked);
        if (_speedUpButton != null)
            _speedUpButton.onClick.AddListener(OnSpeedUpClicked);
        if (_extraGoldButton != null)
            _extraGoldButton.onClick.AddListener(OnExtraGoldClicked);

        if (_equipButton != null)
            _equipButton.onClick.AddListener(OnEquipButtonClicked);
        if (_checkInButton != null)
            _checkInButton.onClick.AddListener(OnCheckInButtonClicked);
        if (_questButton != null)
            _questButton.onClick.AddListener(OnQuestButtonClicked);
        if (_shopButton != null)
            _shopButton.onClick.AddListener(OnShopButtonClicked);
        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        // 关闭初始状态
        if (_upgradeEffectGlow != null)
            _upgradeEffectGlow.SetActive(false);

        if (_rewardPopup != null)
            _rewardPopup.SetActive(false);

        Debug.Log("[GameUI] Initialized.");
    }

    private void SubscribeEvents()
    {
        if (GameManager.Instance == null) return;

        var pm = GameManager.Instance.PlayerManager;
        var bm = GameManager.Instance.BattleManager;
        var em = GameManager.Instance.EconomyManager;

        if (pm != null)
        {
            pm.OnGoldChanged += OnGoldChanged;
            pm.OnLevelUp += OnLevelUp;
        }

        if (bm != null)
        {
            bm.OnMonsterSpawned += OnMonsterSpawned;
            bm.OnMonsterDamaged += OnMonsterDamaged;
            bm.OnMonsterKilled += OnMonsterKilled;
            bm.OnWaveChanged += OnWaveChanged;
            bm.OnPlayerDamaged += OnPlayerDamaged;
        }

        if (em != null)
        {
            em.OnOfflineEarningsCalculated += OnOfflineEarningsCalculated;
        }

        // 装备系统事件
        if (EquipmentManager.Instance != null)
        {
            // EquipmentManager 有事件的话订阅
        }

        // 签到系统事件
        if (DailyCheckInManager.Instance != null)
        {
            DailyCheckInManager.Instance.OnCheckInClaimed += OnCheckInClaimed;
            DailyCheckInManager.Instance.OnStreakUpdated += OnStreakUpdated;
        }

        // 任务系统事件
        if (DailyQuestManager.Instance != null)
        {
            DailyQuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            DailyQuestManager.Instance.OnQuestClaimed += OnQuestClaimed;
        }
    }

    private void UnsubscribeEvents()
    {
        if (GameManager.Instance != null)
        {
            var pm = GameManager.Instance.PlayerManager;
            var bm = GameManager.Instance.BattleManager;
            var em = GameManager.Instance.EconomyManager;

            if (pm != null)
            {
                pm.OnGoldChanged -= OnGoldChanged;
                pm.OnLevelUp -= OnLevelUp;
            }

            if (bm != null)
            {
                bm.OnMonsterSpawned -= OnMonsterSpawned;
                bm.OnMonsterDamaged -= OnMonsterDamaged;
                bm.OnMonsterKilled -= OnMonsterKilled;
                bm.OnWaveChanged -= OnWaveChanged;
                bm.OnPlayerDamaged -= OnPlayerDamaged;
            }

            if (em != null)
            {
                em.OnOfflineEarningsCalculated -= OnOfflineEarningsCalculated;
            }
        }

        if (DailyCheckInManager.Instance != null)
        {
            DailyCheckInManager.Instance.OnCheckInClaimed -= OnCheckInClaimed;
            DailyCheckInManager.Instance.OnStreakUpdated -= OnStreakUpdated;
        }

        if (DailyQuestManager.Instance != null)
        {
            DailyQuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            DailyQuestManager.Instance.OnQuestClaimed -= OnQuestClaimed;
        }
    }

    #endregion

    #region === Update 方法（核心） ===

    /// <summary>
    /// 刷新所有UI
    /// </summary>
    public void RefreshAllUI()
    {
        if (GameManager.Instance == null) return;

        var data = GameManager.Instance.PlayerManager.Data;

        UpdateGold(data.gold);
        UpdateLevel(data.level);
        UpdateWave(GameManager.Instance.BattleManager.CurrentWave);
        UpdateDPS(data.GetCurrentAttack());
        UpdateUpgradeButton(data.GetUpgradeCost(), data.gold >= data.GetUpgradeCost());
        UpdateAdButtons();
        UpdateAllBadges();

        Debug.Log("[GameUI] All UI refreshed.");
    }

    /// <summary>
    /// 更新金币显示
    /// </summary>
    public void UpdateGold(long gold)
    {
        if (_goldText != null)
            _goldText.text = FormatNumber(gold);

        _lastGold = gold;
    }

    /// <summary>
    /// 更新等级显示
    /// </summary>
    public void UpdateLevel(int level)
    {
        if (_levelText != null)
            _levelText.text = $"等级 {level}";
    }

    /// <summary>
    /// 更新波次显示
    /// </summary>
    public void UpdateWave(int wave)
    {
        if (_waveText != null)
            _waveText.text = $"波次 {wave}";
    }

    /// <summary>
    /// 更新DPS显示
    /// </summary>
    public void UpdateDPS(float attack)
    {
        if (_dpsText != null)
        {
            float dps = attack; // 简化计算
            _dpsText.text = $"DPS: {FormatNumber((long)dps)}";
        }
    }

    /// <summary>
    /// 更新离线时间显示
    /// </summary>
    public void UpdateOfflineTime(float hours)
    {
        if (_offlineTimeText != null)
        {
            _offlineTimeText.gameObject.SetActive(hours > 0);
            _offlineTimeText.text = $"离线 {hours:F1}h";
        }
    }

    /// <summary>
    /// 更新升级按钮状态
    /// </summary>
    public void UpdateUpgradeButton(long cost, bool canAfford)
    {
        if (_upgradeCostText != null)
        {
            _upgradeCostText.text = $"升级\n{FormatNumber(cost)}";
            _upgradeCostText.color = canAfford ? _canAffordColor : _cannotAffordColor;
        }

        if (_upgradeButton != null)
            _upgradeButton.interactable = canAfford;
    }

    /// <summary>
    /// 更新怪物信息显示
    /// </summary>
    public void UpdateMonster(string name, float currentHP, float maxHP, int level = 1)
    {
        if (_monsterNameText != null)
            _monsterNameText.text = name;

        if (_monsterHealthBar != null)
        {
            _monsterHealthBar.maxValue = maxHP;
            _monsterHealthBar.value = currentHP;
        }

        if (_monsterHealthText != null)
            _monsterHealthText.text = $"{FormatNumber((long)currentHP)} / {FormatNumber((long)maxHP)}";

        if (_monsterLevelBadge != null)
            _monsterLevelBadge.text = $"Lv.{level}";
    }

    /// <summary>
    /// 更新激励视频按钮状态
    /// </summary>
    public void UpdateAdButtons()
    {
        float now = Time.time;

        // 离线双倍
        if (_offlineDoubleButton != null)
        {
            bool canUse = now >= _offlineDoubleCooldownEnd;
            _offlineDoubleButton.interactable = canUse;
            if (_offlineDoubleLabel != null)
                _offlineDoubleLabel.text = canUse ? "离线双倍" : "冷却中";
        }

        // 加速
        if (_speedUpButton != null)
        {
            bool canUse = now >= _speedUpCooldownEnd;
            _speedUpButton.interactable = canUse;
            if (_speedUpLabel != null)
                _speedUpLabel.text = canUse ? "加速" : "冷却中";
        }

        // 额外金币
        if (_extraGoldButton != null)
        {
            bool canUse = now >= _extraGoldCooldownEnd;
            _extraGoldButton.interactable = canUse;
            if (_extraGoldLabel != null)
                _extraGoldLabel.text = canUse ? "领金币" : "冷却中";
        }
    }

    /// <summary>
    /// 更新所有红点Badge
    /// </summary>
    public void UpdateAllBadges()
    {
        // 装备红点（背包有新装备）
        if (_equipBadge != null && EquipmentManager.Instance != null)
        {
            bool hasNew = EquipmentManager.Instance.GetOwnedEquipment().Count > 0;
            _equipBadge.gameObject.SetActive(hasNew);
        }

        // 签到红点（今日未签到）
        if (_checkInBadge != null && DailyCheckInManager.Instance != null)
        {
            _checkInBadge.gameObject.SetActive(DailyCheckInManager.Instance.CanCheckInToday);
        }

        // 任务红点（有可领取）
        if (_questBadge != null && DailyQuestManager.Instance != null)
        {
            _questBadge.gameObject.SetActive(DailyQuestManager.Instance.GetClaimableCount() > 0);
        }
    }

    #endregion

    #region === Update 循环方法 ===

    /// <summary>
    /// 更新冷却UI（每帧）
    /// </summary>
    private void UpdateCooldownUI()
    {
        float now = Time.time;

        // 离线双倍冷却
        if (_offlineDoubleCooldown != null)
        {
            bool onCooldown = now < _offlineDoubleCooldownEnd;
            _offlineDoubleCooldown.gameObject.SetActive(onCooldown);
            if (onCooldown)
            {
                float remaining = _offlineDoubleCooldownEnd - now;
                _offlineDoubleCooldown.fillAmount = Mathf.Clamp01(remaining / 60f);
            }
        }

        // 加速冷却
        if (_speedUpCooldown != null)
        {
            bool onCooldown = now < _speedUpCooldownEnd;
            _speedUpCooldown.gameObject.SetActive(onCooldown);
            if (onCooldown)
            {
                float remaining = _speedUpCooldownEnd - now;
                _speedUpCooldown.fillAmount = Mathf.Clamp01(remaining / 30f);
            }
        }

        // 额外金币冷却
        if (_extraGoldCooldown != null)
        {
            bool onCooldown = now < _extraGoldCooldownEnd;
            _extraGoldCooldown.gameObject.SetActive(onCooldown);
            if (onCooldown)
            {
                float remaining = _extraGoldCooldownEnd - now;
                _extraGoldCooldown.fillAmount = Mathf.Clamp01(remaining / 60f);
            }
        }
    }

    /// <summary>
    /// 更新升级按钮发光效果
    /// </summary>
    private void UpdateUpgradeGlow()
    {
        if (GameManager.Instance == null || _upgradeEffectGlow == null) return;

        var pm = GameManager.Instance.PlayerManager;
        if (pm == null) return;

        bool shouldGlow = pm.Data.gold >= pm.Data.GetUpgradeCost();

        if (shouldGlow != _isUpgradeGlowActive)
        {
            _isUpgradeGlowActive = shouldGlow;

            _upgradeEffectGlow.SetActive(shouldGlow);

            if (shouldGlow)
            {
                DOTweenAnimations.Pulse(_upgradeEffectGlow, 0.15f, -1);
            }
        }
    }

    /// <summary>
    /// 更新伤害数字队列（防止太多重叠）
    /// </summary>
    private void UpdateDamageQueue()
    {
        if (_pendingDamageQueue.Count == 0) return;

        if (Time.time - _lastDamageShowTime >= DamageQueueInterval)
        {
            _lastDamageShowTime = Time.time;
            var action = _pendingDamageQueue.Dequeue();
            action?.Invoke();
        }
    }

    /// <summary>
    /// 添加伤害数字到队列
    /// </summary>
    public void QueueDamage(float damage, Vector3 worldPos, bool isCrit = false)
    {
        _pendingDamageQueue.Enqueue(() =>
        {
            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.ShowDamage((int)damage, isCrit, worldPos);
            }
        });
    }

    #endregion

    #region === 按钮事件处理 ===

    private void OnUpgradeButtonClicked()
    {
        DOTweenAnimations.ButtonClickScale(_upgradeButton);

        if (GameManager.Instance?.PlayerManager != null)
        {
            bool success = GameManager.Instance.PlayerManager.TryUpgrade();
            if (!success)
            {
                // 金币不足反馈
                var rect = _upgradeButton?.GetComponent<RectTransform>();
                if (rect != null)
                    DOTweenAnimations.ShakeScale(rect);
            }
        }
    }

    private void OnOfflineDoubleClicked()
    {
        DOTweenAnimations.ButtonClickScale(_offlineDoubleButton);

        if (Time.time < _offlineDoubleCooldownEnd)
        {
            ShowToast("冷却中，请稍后");
            return;
        }

        if (GameManager.Instance?.AdManager != null)
        {
            GameManager.Instance.AdManager.ShowRewardedVideo(AdManager.RewardedScene.OfflineDouble, (success) =>
            {
                if (success)
                {
                    _offlineDoubleCooldownEnd = Time.time + 60f;
                    ShowReward("离线双倍奖励已领取！");
                }
            });
        }
    }

    private void OnSpeedUpClicked()
    {
        DOTweenAnimations.ButtonClickScale(_speedUpButton);

        if (Time.time < _speedUpCooldownEnd)
        {
            ShowToast("冷却中，请稍后");
            return;
        }

        if (GameManager.Instance?.AdManager != null)
        {
            GameManager.Instance.AdManager.ShowRewardedVideo(AdManager.RewardedScene.SpeedUp, (success) =>
            {
                if (success)
                {
                    _speedUpCooldownEnd = Time.time + 30f;
                    ShowReward("战斗加速中！");
                }
            });
        }
    }

    private void OnExtraGoldClicked()
    {
        DOTweenAnimations.ButtonClickScale(_extraGoldButton);

        if (Time.time < _extraGoldCooldownEnd)
        {
            ShowToast("冷却中，请稍后");
            return;
        }

        if (GameManager.Instance?.AdManager != null)
        {
            GameManager.Instance.AdManager.ShowRewardedVideo(AdManager.RewardedScene.ExtraGold, (success) =>
            {
                if (success)
                {
                    _extraGoldCooldownEnd = Time.time + 60f;
                    ShowReward("额外金币已领取！");
                }
            });
        }
    }

    // === 面板按钮 ===

    private void OnEquipButtonClicked()
    {
        DOTweenAnimations.ButtonClickScale(_equipButton);

        if (_panelController != null)
            _panelController.TogglePanel("Equipment");
        else
            TogglePanelDirect(_equipmentPanel);
    }

    private void OnCheckInButtonClicked()
    {
        DOTweenAnimations.ButtonClickScale(_checkInButton);

        if (_panelController != null)
            _panelController.TogglePanel("CheckIn");
        else
            TogglePanelDirect(_checkInPanel);
    }

    private void OnQuestButtonClicked()
    {
        DOTweenAnimations.ButtonClickScale(_questButton);

        if (_panelController != null)
            _panelController.TogglePanel("Quest");
        else
            TogglePanelDirect(_questPanel);
    }

    private void OnShopButtonClicked()
    {
        DOTweenAnimations.ButtonClickScale(_shopButton);

        if (_panelController != null)
            _panelController.TogglePanel("Shop");
        else
            TogglePanelDirect(_shopPanel);
    }

    private void OnSettingsButtonClicked()
    {
        DOTweenAnimations.ButtonClickScale(_settingsButton);

        if (_panelController != null)
            _panelController.TogglePanel("Settings");
        else
            TogglePanelDirect(_settingsPopup);
    }

    private void TogglePanelDirect(GameObject panel)
    {
        if (panel == null) return;

        if (panel.activeSelf)
        {
            var rect = panel.GetComponent<RectTransform>();
            if (rect != null)
                DOTweenAnimations.SlideOutUp(rect, 0.3f).OnComplete(() => panel.SetActive(false));
            else
                panel.SetActive(false);
        }
        else
        {
            panel.SetActive(true);
            var rect = panel.GetComponent<RectTransform>();
            if (rect != null)
                DOTweenAnimations.SlideInDown(rect, 0.3f);
        }
    }

    #endregion

    #region === 游戏事件回调 ===

    private void OnGoldChanged(long gold)
    {
        UpdateGold(gold);

        if (GameManager.Instance?.PlayerManager != null)
        {
            var data = GameManager.Instance.PlayerManager.Data;
            UpdateUpgradeButton(data.GetUpgradeCost(), gold >= data.GetUpgradeCost());
        }
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateLevel(newLevel);

        // 升级特效
        if (DamagePopupManager.Instance != null)
        {
            Vector3 monsterPos = Vector3.zero;
            if (GameManager.Instance?.BattleManager?.CurrentMonster != null)
                monsterPos = GameManager.Instance.BattleManager.CurrentMonster.transform.position;

            DamagePopupManager.Instance.ShowLevelUp(newLevel, monsterPos);
        }

        ShowLevelUpEffect(newLevel);
    }

    private void OnMonsterSpawned(MonsterData monster)
    {
        if (monster == null) return;
        UpdateMonster(monster.monsterName, monster.currentHealth, monster.maxHealth, monster.level);
    }

    private void OnMonsterDamaged(MonsterData monster, float damage)
    {
        if (monster == null) return;
        UpdateMonster(monster.monsterName, monster.currentHealth, monster.maxHealth, monster.level);

        // 显示伤害数字（使用队列避免重叠）
        Vector3 worldPos = monster.transform != null ? monster.transform.position : Vector3.zero;
        bool isCrit = damage > monster.maxHealth * 0.1f; // 简单暴击判断
        QueueDamage(damage, worldPos, isCrit);

        // 血条抖动
        if (_monsterHealthBar != null)
        {
            var rect = _monsterHealthBar.GetComponent<RectTransform>();
            if (rect != null)
                DOTweenAnimations.HealthBarShake(rect);
        }
    }

    private void OnMonsterKilled(MonsterData monster, long reward)
    {
        ShowReward($"+{FormatNumber(reward)} 金币");

        // 显示金币特效
        if (DamagePopupManager.Instance != null && monster != null)
        {
            DamagePopupManager.Instance.ShowGold(reward, monster.transform.position);
        }
    }

    private void OnWaveChanged(int newWave)
    {
        UpdateWave(newWave);

        // 波次大字动画
        if (_waveText != null)
        {
            DOTweenAnimations.WaveAnnounce(_waveText, newWave, 1.5f);
        }
    }

    private void OnPlayerDamaged()
    {
        // 屏幕震动
        if (Camera.main != null)
        {
            DOTweenAnimations.ScreenShake(Camera.main, 0.15f, 0.3f);
        }
    }

    private void OnOfflineEarningsCalculated(long earnings)
    {
        ShowOfflineRewardPopup(
            $"离线收益\n+{FormatNumber(earnings)} 金币\n点击看广告领取双倍",
            () =>
            {
                if (GameManager.Instance?.AdManager != null)
                {
                    GameManager.Instance.AdManager.ShowRewardedVideo(AdManager.RewardedScene.OfflineDouble, (success) =>
                    {
                        if (success)
                        {
                            GameManager.Instance.PlayerManager.AddGold(earnings);
                            ShowReward($"+{FormatNumber(earnings)} 双倍离线金币！");
                        }
                    });
                }
            }
        );
    }

    private void OnCheckInClaimed(int day, CheckInDay reward)
    {
        ShowReward($"签到奖励\n+{reward.rewardAmount} {(reward.rewardType == RewardType.Gold ? "金币" : "物品")}");
        UpdateAllBadges();
    }

    private void OnStreakUpdated(int streak)
    {
        UpdateAllBadges();
    }

    private void OnQuestCompleted(DailyQuest quest)
    {
        ShowReward($"任务完成！\n{quest.title}");
        UpdateAllBadges();
    }

    private void OnQuestClaimed(DailyQuest quest, long reward)
    {
        ShowReward($"任务奖励\n+{FormatNumber(reward)} 金币");
        UpdateAllBadges();
    }

    #endregion

    #region === 特效与提示 ===

    private void ShowLevelUpEffect(int level)
    {
        ShowReward($"🎉 升级到 {level} 级！");
    }

    /// <summary>
    /// 显示奖励弹窗
    /// </summary>
    public void ShowReward(string message)
    {
        if (_rewardPopup != null && _rewardText != null)
        {
            _rewardText.text = message;
            _rewardPopup.SetActive(true);

            var rect = _rewardPopup.GetComponent<RectTransform>();
            rect.PopIn(0.3f);

            CancelInvoke(nameof(HideReward));
            Invoke(nameof(HideReward), 2f);
        }
    }

    /// <summary>
    /// 显示离线奖励弹窗
    /// </summary>
    public void ShowOfflineRewardPopup(string message, Action onDoubleClick)
    {
        _offlineDoubleCallback = onDoubleClick;

        if (_panelController != null)
        {
            _panelController.ShowOfflineRewardPopup(message, onDoubleClick);
        }
        else if (_rewardPopup != null && _rewardText != null)
        {
            _rewardText.text = message;
            _rewardPopup.SetActive(true);

            var rect = _rewardPopup.GetComponent<RectTransform>();
            rect.PopIn(0.3f);

            CancelInvoke(nameof(HideOfflineRewardAuto));
            Invoke(nameof(HideOfflineRewardAuto), 5f);
        }
    }

    private void HideReward()
    {
        if (_rewardPopup != null)
        {
            var rect = _rewardPopup.GetComponent<RectTransform>();
            rect.PopOut(0.2f, () => _rewardPopup.SetActive(false));
        }
    }

    private void HideOfflineRewardAuto()
    {
        HideReward();
        _offlineDoubleCallback?.Invoke();
        _offlineDoubleCallback = null;
    }

    /// <summary>
    /// 显示 Toast 提示
    /// </summary>
    public void ShowToast(string message)
    {
        Debug.Log($"[Toast] {message}");
        // TODO: 实现实际的 Toast UI
    }

    #endregion

    #region === 辅助方法 ===

    /// <summary>
    /// 格式化数字显示（K/M/B）
    /// </summary>
    private string FormatNumber(long num)
    {
        if (num >= 1000000000)
            return $"{num / 1000000000.0:F1}B";
        if (num >= 1000000)
            return $"{num / 1000000.0:F1}M";
        if (num >= 1000)
            return $"{num / 1000.0:F1}K";
        return num.ToString();
    }

    private T FindComponent<T>(Transform parent, string path) where T : Component
    {
        Transform child = parent.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

    private GameObject FindGameObject(Transform parent, string path)
    {
        Transform child = parent.Find(path);
        return child != null ? child.gameObject : null;
    }

    #endregion

    #region === 公共接口 ===

    /// <summary>
    /// 关闭所有面板
    /// </summary>
    public void CloseAllPanels()
    {
        _panelController?.CloseAllPanels();
    }

    /// <summary>
    /// 检查面板是否打开
    /// </summary>
    public bool IsPanelOpen(string panelName)
    {
        return _panelController != null && _panelController.IsPanelOpen(panelName);
    }

    #endregion
}
