using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using IdleGame;

/// <summary>
/// UI管理器 - 负责所有界面更新
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("主界面组件")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _waveText;
    [SerializeField] private TextMeshProUGUI _damagePerSecondText;
    
    [Header("怪物信息")]
    [SerializeField] private TextMeshProUGUI _monsterNameText;
    [SerializeField] private Image _monsterHealthBar;
    [SerializeField] private TextMeshProUGUI _monsterHealthText;
    
    [Header("按钮")]
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TextMeshProUGUI _upgradeCostText;
    
    [Header("激励视频按钮")]
    [SerializeField] private Button _offlineDoubleButton;
    [SerializeField] private Button _speedUpButton;
    [SerializeField] private Button _extraGoldButton;
    
    [Header("弹窗")]
    [SerializeField] private GameObject _rewardPopup;
    [SerializeField] private TextMeshProUGUI _rewardText;

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        SubscribeToEvents();
        RefreshAllUI();
    }

    private void CacheComponents()
    {
        // 主界面
        if (_goldText == null) _goldText = GameObject.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
        if (_levelText == null) _levelText = GameObject.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        if (_waveText == null) _waveText = GameObject.Find("WaveText")?.GetComponent<TextMeshProUGUI>();
        if (_damagePerSecondText == null) _damagePerSecondText = GameObject.Find("DPSText")?.GetComponent<TextMeshProUGUI>();
        
        // 怪物
        if (_monsterNameText == null) _monsterNameText = GameObject.Find("MonsterNameText")?.GetComponent<TextMeshProUGUI>();
        if (_monsterHealthBar == null) _monsterHealthBar = GameObject.Find("MonsterHealthBar")?.GetComponent<Image>();
        if (_monsterHealthText == null) _monsterHealthText = GameObject.Find("MonsterHealthText")?.GetComponent<TextMeshProUGUI>();
        
        // 按钮
        if (_upgradeButton == null) _upgradeButton = GameObject.Find("UpgradeButton")?.GetComponent<Button>();
        if (_upgradeCostText == null) _upgradeCostText = GameObject.Find("UpgradeCostText")?.GetComponent<TextMeshProUGUI>();
        
        // 激励视频
        if (_offlineDoubleButton == null) _offlineDoubleButton = GameObject.Find("OfflineDoubleButton")?.GetComponent<Button>();
        if (_speedUpButton == null) _speedUpButton = GameObject.Find("SpeedUpButton")?.GetComponent<Button>();
        if (_extraGoldButton == null) _extraGoldButton = GameObject.Find("ExtraGoldButton")?.GetComponent<Button>();
        
        // 弹窗
        if (_rewardPopup == null) _rewardPopup = GameObject.Find("RewardPopup");
        if (_rewardText == null) _rewardText = GameObject.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
        
        Debug.Log("[UIManager] Components cached.");
    }

    private void SubscribeToEvents()
    {
        // 按钮事件
        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        
        if (_offlineDoubleButton != null)
            _offlineDoubleButton.onClick.AddListener(() => OnAdButtonClicked(AdManager.RewardedScene.OfflineDouble));
        
        if (_speedUpButton != null)
            _speedUpButton.onClick.AddListener(() => OnAdButtonClicked(AdManager.RewardedScene.SpeedUp));
        
        if (_extraGoldButton != null)
            _extraGoldButton.onClick.AddListener(() => OnAdButtonClicked(AdManager.RewardedScene.ExtraGold));
        
        // 订阅游戏事件
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.PlayerManager.OnGoldChanged += OnGoldChanged;
            gm.PlayerManager.OnLevelUp += OnLevelUp;
            gm.BattleManager.OnMonsterSpawned += OnMonsterSpawned;
            gm.BattleManager.OnMonsterDamaged += OnMonsterDamaged;
            gm.BattleManager.OnMonsterKilled += OnMonsterKilled;
            gm.BattleManager.OnWaveChanged += OnWaveChanged;
            gm.EconomyManager.OnOfflineEarningsCalculated += OnOfflineEarnings;
        }
        
        // 隐藏弹窗
        if (_rewardPopup != null)
            _rewardPopup.SetActive(false);
    }

    /// <summary>
    /// 刷新所有UI
    /// </summary>
    public void RefreshAllUI()
    {
        if (GameManager.Instance == null) return;
        
        PlayerData data = GameManager.Instance.PlayerManager.Data;
        
        // 金币
        UpdateGoldText(data.gold);
        
        // 等级
        UpdateLevelText(data.level);
        
        // 波次
        UpdateWaveText(GameManager.Instance.BattleManager.CurrentWave);
        
        // DPS
        UpdateDPSText(data.GetCurrentAttack());
        
        // 升级消耗
        UpdateUpgradeCost(data.GetUpgradeCost(), data.gold >= data.GetUpgradeCost());
    }

    private void UpdateGoldText(long gold)
    {
        if (_goldText != null)
            _goldText.text = FormatGold(gold);
    }

    private void UpdateLevelText(int level)
    {
        if (_levelText != null)
            _levelText.text = $"等级 {level}";
    }

    private void UpdateWaveText(int wave)
    {
        if (_waveText != null)
            _waveText.text = $"波次 {wave}";
    }

    private void UpdateDPSText(float attack)
    {
        if (_damagePerSecondText != null)
        {
            // DPS ≈ 每秒攻击次数(1次) × 攻击力
            float dps = attack;
            _damagePerSecondText.text = $"DPS: {dps:F0}";
        }
    }

    private void UpdateUpgradeCost(long cost, bool canAfford)
    {
        if (_upgradeCostText != null)
        {
            _upgradeCostText.text = $"升级\n{cost}金币";
            _upgradeCostText.color = canAfford ? Color.green : Color.gray;
        }
        
        if (_upgradeButton != null)
            _upgradeButton.interactable = canAfford;
    }

    private void UpdateMonsterUI(MonsterData monster)
    {
        if (_monsterNameText != null)
            _monsterNameText.text = monster.monsterName;
        
        if (_monsterHealthBar != null)
            _monsterHealthBar.fillAmount = monster.GetHealthPercent();
        
        if (_monsterHealthText != null)
            _monsterHealthText.text = $"{monster.currentHealth:F0} / {monster.maxHealth:F0}";
    }

    private string FormatGold(long gold)
    {
        if (gold >= 1000000000)
            return $"{gold / 1000000000.0:F1}B";
        if (gold >= 1000000)
            return $"{gold / 1000000.0:F1}M";
        if (gold >= 1000)
            return $"{gold / 1000.0:F1}K";
        return gold.ToString();
    }

    private void ShowRewardPopup(string message)
    {
        if (_rewardPopup != null && _rewardText != null)
        {
            _rewardText.text = message;
            _rewardPopup.SetActive(true);
            
            // 2秒后自动隐藏
            CancelInvoke(nameof(HideRewardPopup));
            Invoke(nameof(HideRewardPopup), 2f);
        }
    }

    private void HideRewardPopup()
    {
        if (_rewardPopup != null)
            _rewardPopup.SetActive(false);
    }

    // ==================== 事件处理 ====================

    private void OnGoldChanged(long gold)
    {
        UpdateGoldText(gold);
        UpdateUpgradeCost(GameManager.Instance.PlayerManager.Data.GetUpgradeCost(), 
                         gold >= GameManager.Instance.PlayerManager.Data.GetUpgradeCost());
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateLevelText(newLevel);
        ShowRewardPopup($"🎉 升级到 {newLevel} 级！");
    }

    private void OnMonsterSpawned(MonsterData monster)
    {
        UpdateMonsterUI(monster);
    }

    private void OnMonsterDamaged(MonsterData monster, float damage)
    {
        UpdateMonsterUI(monster);
    }

    private void OnMonsterKilled(MonsterData monster, long reward)
    {
        ShowRewardPopup($"+{reward} 金币");
    }

    private void OnWaveChanged(int newWave)
    {
        UpdateWaveText(newWave);
    }

    private void OnOfflineEarnings(long earnings)
    {
        ShowRewardPopup($"离线收益\n+{FormatGold(earnings)} 金币");
    }

    private void OnUpgradeButtonClicked()
    {
        GameManager.Instance.PlayerManager.TryUpgrade();
    }

    private void OnAdButtonClicked(AdManager.RewardedScene scene)
    {
        GameManager.Instance.AdManager.ShowRewardedVideo(scene);
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (GameManager.Instance != null && GameManager.Instance.PlayerManager != null)
        {
            GameManager.Instance.PlayerManager.OnGoldChanged -= OnGoldChanged;
            GameManager.Instance.PlayerManager.OnLevelUp -= OnLevelUp;
        }
        
        if (GameManager.Instance != null && GameManager.Instance.BattleManager != null)
        {
            GameManager.Instance.BattleManager.OnMonsterSpawned -= OnMonsterSpawned;
            GameManager.Instance.BattleManager.OnMonsterDamaged -= OnMonsterDamaged;
            GameManager.Instance.BattleManager.OnMonsterKilled -= OnMonsterKilled;
            GameManager.Instance.BattleManager.OnWaveChanged -= OnWaveChanged;
        }
    }

    // ==================== 装备系统接口 ====================

    /// <summary>
    /// 更新装备面板显示
    /// </summary>
    public void UpdateEquipmentPanel()
    {
        if (EquipmentManager.Instance == null)
        {
            Debug.LogWarning("[UIManager] EquipmentManager instance not found.");
            return;
        }

        // 获取当前穿戴的装备
        EquipmentData weapon = EquipmentManager.Instance.GetEquippedItem(EquipmentSlot.Weapon);
        EquipmentData armor = EquipmentManager.Instance.GetEquippedItem(EquipmentSlot.Armor);
        EquipmentData accessory = EquipmentManager.Instance.GetEquippedItem(EquipmentSlot.Accessory);

        // 获取装备加成
        EquipmentBonus bonus = EquipmentManager.Instance.GetEquipmentBonuses();

        Debug.Log($"[UIManager] UpdateEquipmentPanel - Weapon: {(weapon != null ? weapon.name : "None")}, " +
                  $"Armor: {(armor != null ? armor.name : "None")}, " +
                  $"Accessory: {(accessory != null ? accessory.name : "None")}");
        Debug.Log($"[UIManager] Equipment Bonuses - ATK: {bonus.attackBonus}, HP: {bonus.healthBonus}, " +
                  $"CritRate: {bonus.critRateBonus}, CritDmg: {bonus.critDamageBonus}");

        // TODO: 根据具体UI实现更新面板
        // 例如：更新装备栏图标、属性显示、强化按钮状态等
    }

    /// <summary>
    /// 装备强化成功反馈
    /// </summary>
    public void ShowEquipmentUpgradeEffect(EquipmentData equip)
    {
        if (equip == null) return;

        Debug.Log($"[UIManager] ShowEquipmentUpgradeEffect - {equip.name} upgraded to Level {equip.level}");

        // TODO: 根据具体UI实现强化特效
        // 例如：
        // - 显示强化成功弹窗
        // - 播放强化特效动画
        // - 显示属性变化提示
        // - 播放音效

        // 示例：显示强化成功提示
        ShowRewardPopup($"强化成功！\n{equip.name} 等级 +1");
    }
}
