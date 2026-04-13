using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using IdleGame;

/// <summary>
/// 装备界面UI控制器
/// 挂载在 EquipmentPanel 上
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    [Header("装备槽位")]
    public Button weaponSlot;
    public Image weaponIcon;
    public TextMeshProUGUI weaponLevelText;
    public GameObject weaponEmptyIcon;

    public Button armorSlot;
    public Image armorIcon;
    public TextMeshProUGUI armorLevelText;
    public GameObject armorEmptyIcon;

    public Button accessorySlot;
    public Image accessoryIcon;
    public TextMeshProUGUI accessoryLevelText;
    public GameObject accessoryEmptyIcon;

    [Header("详情面板")]
    public GameObject detailPanel;
    public TextMeshProUGUI equipNameText;
    public TextMeshProUGUI equipTypeText;
    public TextMeshProUGUI equipLevelText;
    public TextMeshProUGUI equipATKText;
    public TextMeshProUGUI equipHPText;
    public TextMeshProUGUI equipCritRateText;
    public TextMeshProUGUI equipCritDmgText;
    public Button upgradeButton;
    public TextMeshProUGUI upgradeCostText;
    public Button sellButton;
    public TextMeshProUGUI sellPriceText;
    public Button unequipButton;

    [Header("背包")]
    public Transform inventoryGrid;
    public GameObject inventoryItemPrefab;

    [Header("总属性面板")]
    public TextMeshProUGUI totalATKText;
    public TextMeshProUGUI totalHPText;
    public TextMeshProUGUI totalCritRateText;
    public TextMeshProUGUI totalCritDmgText;

    [Header("关闭按钮")]
    public Button closeButton;

    private EquipmentData _selectedEquipment;
    private List<GameObject> _inventoryItems = new List<GameObject>();

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        Initialize();
        RefreshAll();
    }

    private void CacheComponents()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (weaponSlot != null)
            weaponSlot.onClick.AddListener(() => OnSlotClicked(EquipmentSlot.Weapon));
        if (armorSlot != null)
            armorSlot.onClick.AddListener(() => OnSlotClicked(EquipmentSlot.Armor));
        if (accessorySlot != null)
            accessorySlot.onClick.AddListener(() => OnSlotClicked(EquipmentSlot.Accessory));

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipClicked);
    }

    private void Initialize()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        RefreshAll();
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    /// <summary>
    /// 刷新所有装备UI
    /// </summary>
    public void RefreshAll()
    {
        RefreshEquipmentSlots();
        RefreshInventory();
        RefreshTotalStats();
        HideDetail();
    }

    /// <summary>
    /// 刷新装备槽位显示
    /// </summary>
    private void RefreshEquipmentSlots()
    {
        if (EquipmentManager.Instance == null) return;

        // 武器
        var weapon = EquipmentManager.Instance.GetEquippedItem(EquipmentSlot.Weapon);
        RefreshSlot(weaponSlot, weaponIcon, weaponLevelText, weaponEmptyIcon, weapon);

        // 防具
        var armor = EquipmentManager.Instance.GetEquippedItem(EquipmentSlot.Armor);
        RefreshSlot(armorSlot, armorIcon, armorLevelText, armorEmptyIcon, armor);

        // 饰品
        var accessory = EquipmentManager.Instance.GetEquippedItem(EquipmentSlot.Accessory);
        RefreshSlot(accessorySlot, accessoryIcon, accessoryLevelText, accessoryEmptyIcon, accessory);
    }

    private void RefreshSlot(Button slot, Image icon, TextMeshProUGUI levelText, GameObject emptyIcon, EquipmentData data)
    {
        if (data != null)
        {
            if (icon != null)
            {
                icon.gameObject.SetActive(true);
                // TODO: 设置图标Sprite（根据品质着色）
                icon.color = GetQualityColor(data.quality);
            }
            if (levelText != null)
            {
                levelText.gameObject.SetActive(true);
                levelText.text = $"+{data.level}";
            }
            if (emptyIcon != null)
                emptyIcon.SetActive(false);
        }
        else
        {
            if (icon != null) icon.gameObject.SetActive(false);
            if (levelText != null) levelText.gameObject.SetActive(false);
            if (emptyIcon != null) emptyIcon.SetActive(true);
        }
    }

    private Color GetQualityColor(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common: return Color.gray;
            case EquipmentQuality.Uncommon: return Color.green;
            case EquipmentQuality.Rare: return Color.blue;
            case EquipmentQuality.Epic: return new Color(0.6f, 0f, 1f); // 紫色
            case EquipmentQuality.Legendary: return new Color(1f, 0.5f, 0f); // 橙色
            default: return Color.white;
        }
    }

    /// <summary>
    /// 刷新背包
    /// </summary>
    private void RefreshInventory()
    {
        if (EquipmentManager.Instance == null || inventoryGrid == null) return;

        // 清除旧物品
        foreach (var item in _inventoryItems)
        {
            Destroy(item);
        }
        _inventoryItems.Clear();

        // 生成新物品
        var owned = EquipmentManager.Instance.GetOwnedEquipment();
        foreach (var equip in owned)
        {
            // 跳过已穿戴的
            if (IsEquipped(equip)) continue;

            GameObject item = Instantiate(inventoryItemPrefab, inventoryGrid);
            _inventoryItems.Add(item);

            // 绑定数据
            var itemUI = item.GetComponent<InventoryItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(equip, this);
            }
            else
            {
                // 简单绑定
                var icon = item.transform.Find("Icon")?.GetComponent<Image>();
                var level = item.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();

                if (icon != null)
                {
                    icon.color = GetQualityColor(equip.quality);
                }
                if (level != null)
                {
                    level.text = $"+{equip.level}";
                }

                var btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnInventoryItemClicked(equip));
                }
            }
        }

        Debug.Log($"[EquipmentUI] Inventory refreshed. Items: {_inventoryItems.Count}");
    }

    private bool IsEquipped(EquipmentData equip)
    {
        if (EquipmentManager.Instance == null) return false;
        var slots = new[] {
            EquipmentSlot.Weapon,
            EquipmentSlot.Armor,
            EquipmentSlot.Accessory
        };
        foreach (var slot in slots)
        {
            var equipped = EquipmentManager.Instance.GetEquippedItem(slot);
            if (equipped != null && equipped.equipmentId == equip.equipmentId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 刷新总属性
    /// </summary>
    private void RefreshTotalStats()
    {
        if (EquipmentManager.Instance == null) return;

        var bonus = EquipmentManager.Instance.GetEquipmentBonuses();

        if (totalATKText != null)
            totalATKText.text = $"+{bonus.attackBonus:F0}";
        if (totalHPText != null)
            totalHPText.text = $"+{bonus.healthBonus:F0}";
        if (totalCritRateText != null)
            totalCritRateText.text = $"+{bonus.critRateBonus:P0}";
        if (totalCritDmgText != null)
            totalCritDmgText.text = $"+{bonus.critDamageBonus:P0}";
    }

    // ==================== 点击事件 ====================

    private void OnSlotClicked(EquipmentSlot slot)
    {
        var equipped = EquipmentManager.Instance.GetEquippedItem(slot);
        if (equipped != null)
        {
            ShowDetail(equipped);
        }
    }

    private void OnInventoryItemClicked(EquipmentData equip)
    {
        ShowDetail(equip);
    }

    /// <summary>
    /// 显示装备详情
    /// </summary>
    public void ShowDetail(EquipmentData equip)
    {
        _selectedEquipment = equip;

        if (detailPanel != null)
            detailPanel.SetActive(true);

        if (equipNameText != null)
            equipNameText.text = equip.name;
        if (equipTypeText != null)
            equipTypeText.text = equip.slot.ToString();
        if (equipLevelText != null)
            equipLevelText.text = $"等级 +{equip.level}";

        if (equipATKText != null)
            equipATKText.text = $"攻击 +{equip.attackBonus:F0}";
        if (equipHPText != null)
            equipHPText.text = $"生命 +{equip.healthBonus:F0}";
        if (equipCritRateText != null)
            equipCritRateText.text = $"暴击率 +{equip.critRateBonus:P0}";
        if (equipCritDmgText != null)
            equipCritDmgText.text = $"暴击伤害 +{equip.critDamageBonus:P0}";

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"强化\n{FormatNumber(equip.Cost)}金币";
            bool canAfford = GameManager.Instance?.PlayerManager?.Data?.gold >= equip.Cost;
            upgradeCostText.color = canAfford ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
            if (upgradeButton != null)
                upgradeButton.interactable = canAfford;
        }

        if (sellPriceText != null)
            sellPriceText.text = $"出售 +{FormatNumber(equip.Cost / 2)}金币";

        // 装备按钮显示
        bool isEquipped = IsEquipped(equip);
        if (unequipButton != null)
            unequipButton.gameObject.SetActive(isEquipped);
    }

    private void HideDetail()
    {
        _selectedEquipment = null;
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    private void OnUpgradeClicked()
    {
        if (_selectedEquipment == null) return;

        DOTweenAnimations.ButtonClickScale(upgradeButton);
        SoundManager.Instance?.PlaySFX(IdleGame.Audio.SoundType.SFX_Click);

        if (EquipmentManager.Instance != null)
        {
            bool success = EquipmentManager.Instance.UpgradeEquipment(_selectedEquipment.equipmentId);
            if (success)
            {
                DOTweenAnimations.Pulse(upgradeButton.gameObject, 0.3f);
                RefreshAll();
            }
            else
            {
                DOTweenAnimations.ShakeScale(upgradeButton.GetComponent<RectTransform>());
            }
        }
    }

    private void OnSellClicked()
    {
        if (_selectedEquipment == null) return;

        DOTweenAnimations.ButtonClickScale(sellButton);
        SoundManager.Instance?.PlaySFX(IdleGame.Audio.SoundType.SFX_Click);

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.SellEquipment(_selectedEquipment.equipmentId);
            HideDetail();
            RefreshAll();
        }
    }

    private void OnUnequipClicked()
    {
        if (_selectedEquipment == null) return;

        DOTweenAnimations.ButtonClickScale(unequipButton);
        SoundManager.Instance?.PlaySFX(IdleGame.Audio.SoundType.SFX_Click);

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.UnequipItem(_selectedEquipment.slot);
            HideDetail();
            RefreshAll();
        }
    }

    public void OnEquipButtonClicked()
    {
        if (_selectedEquipment == null) return;

        if (EquipmentManager.Instance != null)
        {
            // 获取当前槽位以播放发光特效
            Transform slotTransform = GetSlotTransform(_selectedEquipment.slot);

            EquipmentManager.Instance.EquipItem(_selectedEquipment);

            // 播放穿戴特效
            if (slotTransform != null)
                UIManager.Instance?.ShowEquipGlowEffect(slotTransform);
            else
                UIManager.Instance?.ShowEquipGlowEffect(null);

            HideDetail();
            RefreshAll();
        }
    }

    private Transform GetSlotTransform(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => weaponSlot?.transform,
            EquipmentSlot.Armor => armorSlot?.transform,
            EquipmentSlot.Accessory => accessorySlot?.transform,
            _ => null
        };
    }

    private void Hide()
    {
        if (UIPanelController.Instance != null)
            UIPanelController.Instance.ClosePanel("Equipment");
        else
            gameObject.SetActive(false);
    }

    // ==================== 辅助方法 ====================

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
}

/// <summary>
/// 背包物品UI组件
/// 挂载在 InventoryItemPrefab 上
/// </summary>
public class InventoryItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI levelText;
    public GameObject equippedBadge;

    private EquipmentData _data;
    private EquipmentUI _parentUI;

    public void Initialize(EquipmentData data, EquipmentUI parentUI)
    {
        _data = data;
        _parentUI = parentUI;

        // 设置图标（根据品质）
        if (icon != null)
        {
            // TODO: 根据装备类型设置不同图标
            icon.color = GetQualityColor(data.quality);
        }

        // 设置等级
        if (levelText != null)
        {
            levelText.text = $"+{data.level}";
        }

        // 已穿戴标记
        if (equippedBadge != null)
        {
            equippedBadge.SetActive(false); //背包物品默认未穿戴
        }

        // 点击事件
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        if (_parentUI != null)
        {
            _parentUI.ShowDetail(_data);
        }
    }

    private Color GetQualityColor(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common: return Color.gray;
            case EquipmentQuality.Uncommon: return Color.green;
            case EquipmentQuality.Rare: return Color.blue;
            case EquipmentQuality.Epic: return new Color(0.6f, 0f, 1f);
            case EquipmentQuality.Legendary: return new Color(1f, 0.5f, 0f);
            default: return Color.white;
        }
    }
}
