using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using IdleGame;
using IdleGame.Audio;

/// <summary>
/// 签到面板UI控制器
/// 7天签到循环，显示已签到天数、奖励预览
/// </summary>
public class CheckInUI : MonoBehaviour
{
    [Header("签到天数显示")]
    public TextMeshProUGUI currentDayText;
    public TextMeshProUGUI totalDaysText;

    [Header("签到物品预制体")]
    public GameObject checkInItemPrefab;
    public Transform checkInGrid;

    [Header("签到按钮")]
    public Button freeCheckInButton;
    public TextMeshProUGUI freeCheckInCostText;
    public Button adCheckInButton;
    public TextMeshProUGUI adCheckInCostText;

    [Header("广告补签")]
    public TextMeshProUGUI adRemainText;

    [Header("关闭按钮")]
    public Button closeButton;

    [Header("已领取遮罩")]
    public Sprite claimedOverlaySprite;
    public Color claimedOverlayColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("金色标记")]
    public Sprite goldenBadgeSprite;

    private List<CheckInItemUI> _checkInItems = new List<CheckInItemUI>();
    private int _currentMonth;
    private int _currentYear;

    // 7天签到奖励配置（示例）
    private readonly CheckInReward[] _weeklyRewards = new CheckInReward[]
    {
        new CheckInReward { day = 1, rewardType = RewardType.Gold, amount = 100, itemName = "100金币" },
        new CheckInReward { day = 2, rewardType = RewardType.Gem, amount = 10, itemName = "10钻石" },
        new CheckInReward { day = 3, rewardType = RewardType.Equipment, amount = 1, itemName = "精良装备箱" },
        new CheckInReward { day = 4, rewardType = RewardType.Gold, amount = 200, itemName = "200金币" },
        new CheckInReward { day = 5, rewardType = RewardType.Gem, amount = 20, itemName = "20钻石" },
        new CheckInReward { day = 6, rewardType = RewardType.Equipment, amount = 1, itemName = "稀有装备箱" },
        new CheckInReward { day = 7, rewardType = RewardType.Gold, amount = 500, itemName = "500金币" },
    };

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        DOTweenAnimations.ScaleIn(rectTransform, 0.3f);
        RefreshAll();
    }

    private void CacheComponents()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (freeCheckInButton != null)
            freeCheckInButton.onClick.AddListener(OnFreeCheckInClicked);

        if (adCheckInButton != null)
            adCheckInButton.onClick.AddListener(OnAdCheckInClicked);
    }

    private void Initialize()
    {
        // 初始化日期
        var now = System.DateTime.Now;
        _currentYear = now.Year;
        _currentMonth = now.Month;

        GenerateCheckInItems();
        RefreshAll();
    }

    /// <summary>
    /// 生成7天签到物品
    /// </summary>
    private void GenerateCheckInItems()
    {
        if (checkInGrid == null || checkInItemPrefab == null) return;

        // 清除旧物品
        foreach (var item in _checkInItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        _checkInItems.Clear();

        // 生成新物品
        for (int i = 0; i < _weeklyRewards.Length; i++)
        {
            GameObject obj = Instantiate(checkInItemPrefab, checkInGrid);
            CheckInItemUI itemUI = obj.GetComponent<CheckInItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(_weeklyRewards[i], i + 1);
                _checkInItems.Add(itemUI);
            }
        }

        Debug.Log($"[CheckInUI] Generated {_weeklyRewards.Length} check-in items");
    }

    /// <summary>
    /// 刷新所有UI
    /// </summary>
    public void RefreshAll()
    {
        RefreshDayDisplay();
        RefreshCheckInItems();
        RefreshButtons();
    }

    /// <summary>
    /// 刷新天数显示
    /// </summary>
    private void RefreshDayDisplay()
    {
        int signedDays = GetSignedDaysThisCycle();

        if (currentDayText != null)
            currentDayText.text = $"{signedDays}";

        if (totalDaysText != null)
            totalDaysText.text = $"/ 7天";

        Debug.Log($"[CheckInUI] Current signed days: {signedDays}/7");
    }

    /// <summary>
    /// 刷新签到物品状态
    /// </summary>
    private void RefreshCheckInItems()
    {
        int signedDays = GetSignedDaysThisCycle();
        int currentDay = GetCurrentDay();

        for (int i = 0; i < _checkInItems.Count; i++)
        {
            if (_checkInItems[i] == null) continue;

            int day = i + 1;
            bool isClaimed = day <= signedDays;
            bool isToday = day == currentDay;
            bool canClaim = day == currentDay && !isClaimed;

            _checkInItems[i].RefreshState(isClaimed, isToday, canClaim);
        }
    }

    /// <summary>
    /// 刷新按钮状态
    /// </summary>
    private void RefreshButtons()
    {
        int signedDays = GetSignedDaysThisCycle();
        int currentDay = GetCurrentDay();
        bool isTodayClaimed = currentDay <= signedDays;
        bool canAdCheckIn = CanAdCheckIn();

        // 免费签到按钮
        if (freeCheckInButton != null)
        {
            freeCheckInButton.interactable = !isTodayClaimed;
        }

        if (freeCheckInCostText != null)
        {
            freeCheckInCostText.text = isTodayClaimed ? "已签到" : "免费签到";
            freeCheckInCostText.color = isTodayClaimed ? Color.gray : new Color(0.2f, 0.8f, 0.2f);
        }

        // 广告补签按钮
        if (adCheckInButton != null)
        {
            adCheckInButton.interactable = canAdCheckIn && !isTodayClaimed;
        }

        if (adRemainText != null)
        {
            int adRemain = GetAdCheckInRemain();
            adRemainText.text = $"广告补签 ({adRemain}次)";
        }
    }

    // ==================== 点击事件 ====================

    private void OnFreeCheckInClicked()
    {
        DOTweenAnimations.ButtonClickScale(freeCheckInButton.transform as RectTransform);

        int currentDay = GetCurrentDay();
        int signedDays = GetSignedDaysThisCycle();

        // 检查今天是否已经签到
        if (currentDay <= signedDays)
        {
            Debug.Log("[CheckInUI] Already checked in today!");
            return;
        }

        SoundManager.Instance?.PlaySFX(SoundType.SFX_Reward);

        // 发放奖励
        ClaimReward(currentDay);

        // 更新签到数据
        SaveCheckInData(currentDay, false);

        // 刷新UI
        RefreshAll();

        Debug.Log($"[CheckInUI] Free check-in successful for day {currentDay}");
    }

    private void OnAdCheckInClicked()
    {
        DOTweenAnimations.ButtonClickScale(adCheckInButton.transform as RectTransform);

        int currentDay = GetCurrentDay();
        int signedDays = GetSignedDaysThisCycle();

        // 检查今天是否已经签到
        if (currentDay <= signedDays)
        {
            Debug.Log("[CheckInUI] Already checked in today!");
            return;
        }

        // 检查广告补签次数
        if (!CanAdCheckIn())
        {
            Debug.Log("[CheckInUI] No ad check-in remaining!");
            return;
        }

        SoundManager.Instance?.PlaySFX(SoundType.SFX_Click);

        // 播放广告
        ShowAdReward(() =>
        {
            // 广告观看完成后发放奖励
            ClaimReward(currentDay);
            SaveCheckInData(currentDay, true);
            RefreshAll();

            Debug.Log($"[CheckInUI] Ad check-in successful for day {currentDay}");
        });
    }

    /// <summary>
    /// 领取奖励
    /// </summary>
    private void ClaimReward(int day)
    {
        if (day < 1 || day > _weeklyRewards.Length) return;

        DOTweenAnimations.Pulse(_checkInItems[day - 1].gameObject.transform as RectTransform);

        var reward = _weeklyRewards[day - 1];

        switch (reward.rewardType)
        {
            case RewardType.Gold:
                GameManager.Instance?.PlayerManager?.AddGold(reward.amount);
                ShowRewardPopup(reward.itemName, $"+{reward.amount} 金币");
                break;

            case RewardType.Gem:
                GameManager.Instance?.PlayerManager?.AddGem(reward.amount);
                ShowRewardPopup(reward.itemName, $"+{reward.amount} 钻石");
                break;

            case RewardType.Equipment:
                // 发放装备
                // EquipmentManager.Instance?.GenerateEquipment(...)
                ShowRewardPopup(reward.itemName, "已发放到背包");
                break;
        }
    }

    private void ShowRewardPopup(string title, string description)
    {
        // TODO: 显示奖励弹窗
        Debug.Log($"[CheckInUI] Reward: {title} - {description}");
    }

    /// <summary>
    /// 显示广告
    /// </summary>
    private void ShowAdReward(System.Action onComplete)
    {
        // TODO: 调用广告SDK
        Debug.Log("[CheckInUI] Showing ad...");

        // 模拟广告完成
        // 实际项目中需要接入广告SDK的回调
        // AdvertisingManager.Instance.ShowRewardedAd((success) => { ... });
    }

    private void Hide()
    {
        DOTweenAnimations.ScaleOut(rectTransform, 0.2f);

        if (UIPanelController.Instance != null)
            UIPanelController.Instance.ClosePanel("CheckIn");
        else
            gameObject.SetActive(false);
    }

    // ==================== 数据管理 ====================

    private int GetCurrentDay()
    {
        // 获取当前是本周期第几天（1-7循环）
        int dayOfYear = System.DateTime.Now.DayOfYear;
        return ((dayOfYear - 1) % 7) + 1;
    }

    private int GetSignedDaysThisCycle()
    {
        // 从PlayerData获取已签到天数
        // 实际项目中需要从持久化数据读取
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return 0;

        // 检查是否是同一天
        if (playerData.lastCheckInDate != System.DateTime.Now.ToString("yyyy-MM-dd"))
        {
            return playerData.signedDaysThisCycle;
        }

        return playerData.signedDaysThisCycle;
    }

    private bool CanAdCheckIn()
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return false;

        return playerData.adCheckInRemaining > 0;
    }

    private int GetAdCheckInRemain()
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return 0;

        return playerData.adCheckInRemaining;
    }

    private void SaveCheckInData(int day, bool usedAd)
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return;

        playerData.signedDaysThisCycle = day;
        playerData.lastCheckInDate = System.DateTime.Now.ToString("yyyy-MM-dd");

        if (usedAd)
        {
            playerData.adCheckInRemaining--;
        }

        Debug.Log($"[CheckInUI] Saved check-in data: day={day}, usedAd={usedAd}");
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
/// 签到物品UI组件
/// 挂载在 CheckInItemPrefab 上
/// </summary>
public class CheckInItemUI : MonoBehaviour
{
    [Header("日期显示")]
    public TextMeshProUGUI dayText;
    public Image dayCircle;

    [Header("奖励显示")]
    public Image rewardIcon;
    public TextMeshProUGUI rewardAmountText;
    public TextMeshProUGUI rewardNameText;

    [Header("状态遮罩")]
    public GameObject claimedOverlay;
    public GameObject goldenBadge;
    public GameObject todayHighlight;

    [Header("状态文字")]
    public TextMeshProUGUI statusText;

    private CheckInReward _rewardData;
    private int _dayIndex;
    private bool _isClaimed;
    private bool _isToday;
    private bool _canClaim;

    public void Initialize(CheckInReward reward, int dayIndex)
    {
        _rewardData = reward;
        _dayIndex = dayIndex;

        RefreshUI();
    }

    public void RefreshState(bool isClaimed, bool isToday, bool canClaim)
    {
        _isClaimed = isClaimed;
        _isToday = isToday;
        _canClaim = canClaim;

        RefreshUI();
    }

    private void RefreshUI()
    {
        // 日期
        if (dayText != null)
            dayText.text = $"第{_dayIndex}天";

        // 奖励
        if (rewardAmountText != null)
        {
            rewardAmountText.text = _rewardData.amount.ToString();
        }

        if (rewardNameText != null)
        {
            rewardNameText.text = _rewardData.itemName;
        }

        // 奖励图标
        if (rewardIcon != null)
        {
            rewardIcon.sprite = GetRewardIcon(_rewardData.rewardType);
            rewardIcon.color = Color.white;
        }

        // 状态遮罩
        if (claimedOverlay != null)
        {
            claimedOverlay.SetActive(_isClaimed);
        }

        // 金色标记
        if (goldenBadge != null)
        {
            goldenBadge.SetActive(_isClaimed);
        }

        // 今日高亮
        if (todayHighlight != null)
        {
            todayHighlight.SetActive(_isToday && !_isClaimed);
        }

        // 状态文字
        if (statusText != null)
        {
            if (_isClaimed)
            {
                statusText.text = "已领取";
                statusText.color = new Color(1f, 0.84f, 0f); // 金色
            }
            else if (_isToday)
            {
                statusText.text = "可领取";
                statusText.color = new Color(0.2f, 0.8f, 0.2f); // 绿色
            }
            else
            {
                statusText.text = "未签到";
                statusText.color = Color.gray;
            }
        }

        // 日期圆圈颜色
        if (dayCircle != null)
        {
            if (_isClaimed)
            {
                dayCircle.color = new Color(1f, 0.84f, 0f); // 金色
            }
            else if (_isToday)
            {
                dayCircle.color = new Color(0.2f, 0.8f, 0.2f); // 绿色
            }
            else
            {
                dayCircle.color = Color.gray;
            }
        }
    }

    private Sprite GetRewardIcon(RewardType type)
    {
        // TODO: 返回对应的图标资源
        // 可以根据type返回不同的Sprite
        return null;
    }
}

/// <summary>
/// 签到奖励配置
/// </summary>
public class CheckInReward
{
    public int day;
    public RewardType rewardType;
    public int amount;
    public string itemName;
}

/// <summary>
/// 奖励类型
/// </summary>
public enum RewardType
{
    Gold,
    Gem,
    Equipment,
    Item
}
