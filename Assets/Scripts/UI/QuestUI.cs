using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using IdleGame.Audio;

/// <summary>
/// 每日任务面板UI控制器
/// 5个任务，任务进度条+文字显示，可领取时高亮按钮
/// </summary>
public class QuestUI : MonoBehaviour
{
    /// <summary>
    /// 共享的 GDD 任务模板，供 DailyQuestManager 等其他系统使用
    /// </summary>
    public static QuestData[] QuestTemplates => _questTemplates;

    [Header("任务列表")]

    public GameObject questItemPrefab;
    public Transform questGrid;

    [Header("任务数量")]
    public TextMeshProUGUI completedCountText;
    public TextMeshProUGUI totalCountText;

    [Header("刷新按钮")]
    public Button refreshButton;
    public TextMeshProUGUI refreshCostText;

    [Header("关闭按钮")]
    public Button closeButton;

    [Header("进度条")]
    public Sprite progressBarFillSprite;
    public Color progressBarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color progressBarFillColor = new Color(0.2f, 0.8f, 0.2f);
    public Color progressBarCompleteColor = new Color(1f, 0.84f, 0f); // 金色

    private List<QuestItemUI> _questItems = new List<QuestItemUI>();
    private List<QuestData> _dailyQuests = new List<QuestData>();

    // 每日任务配置（与 GDD Q1-Q10 对应）
    private readonly QuestData[] _questTemplates = new QuestData[]
    {
        new QuestData
        {
            questId = "q1",
            questName = "怪物猎人",
            description = "击杀10个怪物",
            targetCount = 10,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 1000,
            questType = QuestType.KillMonster
        },
        new QuestData
        {
            questId = "q2",
            questName = "杀戮盛宴",
            description = "击杀50个怪物",
            targetCount = 50,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 5000,
            questType = QuestType.KillMonster
        },
        new QuestData
        {
            questId = "q3",
            questName = "初学者",
            description = "升级角色3次",
            targetCount = 3,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 2000,
            questType = QuestType.UpgradeLevel
        },
        new QuestData
        {
            questId = "q4",
            questName = "突破者",
            description = "升级角色10次",
            targetCount = 10,
            currentProgress = 0,
            rewardType = RewardType.Chest,
            rewardAmount = 1,
            questType = QuestType.UpgradeLevel
        },
        new QuestData
        {
            questId = "q5",
            questName = "广告赞助",
            description = "观看3个广告",
            targetCount = 3,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 500,
            questType = QuestType.WatchAd
        },
        new QuestData
        {
            questId = "q6",
            questName = "VIP赞助",
            description = "观看10个广告",
            targetCount = 10,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 2000,
            questType = QuestType.WatchAd
        },
        new QuestData
        {
            questId = "q7",
            questName = "征服者",
            description = "到达波次50",
            targetCount = 50,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 10000,
            questType = QuestType.ReachWave
        },
        new QuestData
        {
            questId = "q8",
            questName = "无尽挑战",
            description = "到达波次100",
            targetCount = 100,
            currentProgress = 0,
            rewardType = RewardType.Chest,
            rewardAmount = 1,
            questType = QuestType.ReachWave
        },
        new QuestData
        {
            questId = "q9",
            questName = "敛金者",
            description = "累计获得50000金币",
            targetCount = 50000,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 5000,
            questType = QuestType.EarnGold
        },
        new QuestData
        {
            questId = "q10",
            questName = "财富自由",
            description = "累计获得200000金币",
            targetCount = 200000,
            currentProgress = 0,
            rewardType = RewardType.Gold,
            rewardAmount = 20000,
            questType = QuestType.EarnGold
        }
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
        LoadQuestData();
    }

    private void CacheComponents()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);
    }

    private void Initialize()
    {
        GenerateQuestItems();
        RefreshAll();
    }

    /// <summary>
    /// 生成任务物品
    /// </summary>
    private void GenerateQuestItems()
    {
        if (questGrid == null || questItemPrefab == null) return;

        // 清除旧物品
        foreach (var item in _questItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        _questItems.Clear();

        // 生成新物品
        for (int i = 0; i < _questTemplates.Length; i++)
        {
            GameObject obj = Instantiate(questItemPrefab, questGrid);
            QuestItemUI itemUI = obj.GetComponent<QuestItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(_questTemplates[i], this);
                _questItems.Add(itemUI);
            }
        }

        Debug.Log($"[QuestUI] Generated {_questTemplates.Length} quest items");
    }

    /// <summary>
    /// 加载任务数据
    /// </summary>
    private void LoadQuestData()
    {
        // 检查是否需要重置（跨天）
        if (IsNewDay())
        {
            ResetDailyQuests();
        }

        // 从PlayerData加载进度
        var questProgress = GetQuestProgress();

        for (int i = 0; i < _questItems.Count; i++)
        {
            if (_questItems[i] == null) continue;

            string questId = _questTemplates[i].questId;
            if (questProgress.ContainsKey(questId))
            {
                _questItems[i].UpdateProgress(questProgress[questId]);
            }
        }

        RefreshAll();
    }

    /// <summary>
    /// 刷新所有UI
    /// </summary>
    public void RefreshAll()
    {
        RefreshCompletedCount();
        RefreshQuestItems();
        RefreshButtons();
    }

    /// <summary>
    /// 刷新完成计数
    /// </summary>
    private void RefreshCompletedCount()
    {
        int completed = 0;
        int total = _questTemplates.Length;

        foreach (var item in _questItems)
        {
            if (item != null && item.IsCompleted && item.IsRewardClaimed)
            {
                completed++;
            }
        }

        if (completedCountText != null)
            completedCountText.text = completed.ToString();

        if (totalCountText != null)
            totalCountText.text = $"/ {total}";
    }

    /// <summary>
    /// 刷新任务物品
    /// </summary>
    private void RefreshQuestItems()
    {
        foreach (var item in _questItems)
        {
            if (item != null)
            {
                item.RefreshUI();
            }
        }
    }

    /// <summary>
    /// 刷新按钮状态
    /// </summary>
    private void RefreshButtons()
    {
        if (refreshButton != null)
        {
            // 检查刷新费用是否足够
            long refreshCost = GetRefreshCost();
            long playerGold = GameManager.Instance?.PlayerManager?.Data?.gold ?? 0;

            refreshButton.interactable = playerGold >= refreshCost;
        }

        if (refreshCostText != null)
        {
            refreshCostText.text = $"{FormatNumber(GetRefreshCost())}金币";
        }
    }

    // ==================== 点击事件 ====================

    /// <summary>
    /// 领取任务奖励（由 QuestItemUI 调用）
    /// </summary>
    public void OnClaimReward(string questId)
    {
        // 查找对应任务
        QuestItemUI targetItem = null;
        foreach (var item in _questItems)
        {
            if (item != null && item.QuestId == questId)
            {
                targetItem = item;
                break;
            }
        }

        if (targetItem == null) return;

        // 播放按钮动画
        if (targetItem.ClaimButton != null)
            DOTweenAnimations.ButtonClickScale(targetItem.ClaimButton.transform as RectTransform);

        // 检查是否可以领取
        if (!targetItem.CanClaimReward)
        {
            Debug.Log($"[QuestUI] Cannot claim reward for quest {questId}");
            return;
        }

        SoundManager.Instance?.PlaySFX(SoundType.SFX_Reward);

        // 发放奖励
        ClaimReward(targetItem.QuestData);

        // 播放领取动画
        DOTweenAnimations.Pulse(targetItem.gameObject.transform as RectTransform);

        // 标记为已领取
        MarkRewardClaimed(questId);

        // 刷新UI
        RefreshAll();

        Debug.Log($"[QuestUI] Claimed reward for quest {questId}");
    }

    /// <summary>
    /// 刷新任务按钮点击
    /// </summary>
    private void OnRefreshClicked()
    {
        DOTweenAnimations.ButtonClickScale(refreshButton.transform as RectTransform);
        SoundManager.Instance?.PlaySFX(SoundType.SFX_Click);

        // 检查费用
        long refreshCost = GetRefreshCost();
        long playerGold = GameManager.Instance?.PlayerManager?.Data?.gold ?? 0;

        if (playerGold < refreshCost)
        {
            Debug.Log($"[QuestUI] Not enough gold to refresh. Need {refreshCost}, have {playerGold}");
            DOTweenAnimations.ShakeScale(refreshButton.GetComponent<RectTransform>());
            return;
        }

        // 扣除金币
        GameManager.Instance?.PlayerManager?.SpendGold(refreshCost);

        // 重置所有任务进度
        ResetDailyQuests();

        // 刷新UI
        RefreshAll();

        Debug.Log($"[QuestUI] Quests refreshed. Cost: {refreshCost}");
    }

    /// <summary>
    /// 领取奖励
    /// </summary>
    private void ClaimReward(QuestData quest)
    {
        switch (quest.rewardType)
        {
            case RewardType.Gold:
                GameManager.Instance?.PlayerManager?.AddGold(quest.rewardAmount);
                ShowRewardPopup(quest.questName, $"+{quest.rewardAmount} 金币");
                break;

            case RewardType.Gem:
                // 钻石系统暂未实现，改为发放金币
                GameManager.Instance?.PlayerManager?.AddGold(quest.rewardAmount * 10);
                ShowRewardPopup(quest.questName, $"+{quest.rewardAmount * 10} 金币(钻石未实现)");
                break;

            case RewardType.Equipment:
                // 装备系统奖励，暂不发
                ShowRewardPopup(quest.questName, "装备奖励(暂未开放)");
                break;

            case RewardType.Item:
                ShowRewardPopup(quest.questName, $"+{quest.rewardAmount} 道具");
                break;

            case RewardType.Chest:
                ShowRewardPopup(quest.questName, "宝箱×1");
                break;
        }
    }

    private void ShowRewardPopup(string title, string description)
    {
        // TODO: 显示奖励弹窗
        Debug.Log($"[QuestUI] Reward: {title} - {description}");
    }

    private void Hide()
    {
        DOTweenAnimations.ScaleOut(rectTransform, 0.2f);

        if (UIPanelController.Instance != null)
            UIPanelController.Instance.ClosePanel("Quest");
        else
            gameObject.SetActive(false);
    }

    // ==================== 数据管理 ====================

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public void UpdateQuestProgress(QuestType type, int amount)
    {
        foreach (var item in _questItems)
        {
            if (item != null && item.QuestData.questType == type)
            {
                int newProgress = Mathf.Min(item.QuestData.currentProgress + amount, item.QuestData.targetCount);
                item.QuestData.currentProgress = newProgress;

                // 保存进度
                SaveQuestProgress(item.QuestId, newProgress);

                // 刷新UI
                RefreshAll();

                Debug.Log($"[QuestUI] Quest {item.QuestId} progress: {newProgress}/{item.QuestData.targetCount}");
            }
        }
    }

    /// <summary>
    /// 检查是否新的一天
    /// </summary>
    private bool IsNewDay()
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return true;

        string lastDate = playerData.lastQuestRefreshDate;
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");

        return lastDate != today;
    }

    /// <summary>
    /// 重置每日任务
    /// </summary>
    private void ResetDailyQuests()
    {
        foreach (var item in _questItems)
        {
            if (item != null)
            {
                item.QuestData.currentProgress = 0;
                item.QuestData.isRewardClaimed = false;
            }
        }

        ClearQuestProgress();
        SaveLastRefreshDate();

        Debug.Log("[QuestUI] Daily quests reset");
    }

    private Dictionary<string, int> GetQuestProgress()
    {
        // TODO: 从PlayerData获取任务进度
        // 实际项目中需要从持久化数据读取
        return new Dictionary<string, int>();
    }

    private void SaveQuestProgress(string questId, int progress)
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return;

        // TODO: 保存到PlayerData
        Debug.Log($"[QuestUI] Saved progress for {questId}: {progress}");
    }

    private void ClearQuestProgress()
    {
        // TODO: 清除任务进度
    }

    private void MarkRewardClaimed(string questId)
    {
        foreach (var item in _questItems)
        {
            if (item != null && item.QuestId == questId)
            {
                item.QuestData.isRewardClaimed = true;
                break;
            }
        }

        // TODO: 保存到PlayerData
    }

    private void SaveLastRefreshDate()
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return;

        playerData.lastQuestRefreshDate = System.DateTime.Now.ToString("yyyy-MM-dd");
    }

    private long GetRefreshCost()
    {
        // 刷新费用递增
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return 100;

        return 100 + (playerData.questRefreshCount * 50);
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
/// 任务物品UI组件
/// 挂载在 QuestItemPrefab 上
/// </summary>
public class QuestItemUI : MonoBehaviour
{
    [Header("任务信息")]
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI descriptionText;
    public Image questIcon;

    [Header("进度显示")]
    public TextMeshProUGUI progressText;
    public Slider progressBar;
    public Image progressFill;

    [Header("奖励显示")]
    public Image rewardIcon;
    public TextMeshProUGUI rewardAmountText;

    [Header("按钮")]
    public Button claimButton;
    public GameObject claimableHighlight;

    [Header("状态")]
    public GameObject completedBadge;
    public TextMeshProUGUI statusText;

    private QuestData _questData;
    private QuestUI _parentUI;

    public string QuestId => _questData?.questId ?? "";
    public QuestData QuestData => _questData;
    public Button ClaimButton => claimButton;
    public bool IsCompleted => _questData != null && _questData.currentProgress >= _questData.targetCount;
    public bool IsRewardClaimed => _questData != null && _questData.isRewardClaimed;
    public bool CanClaimReward => IsCompleted && !IsRewardClaimed;

    public void Initialize(QuestData questData, QuestUI parentUI)
    {
        _questData = questData;
        _parentUI = parentUI;

        // 绑定按钮事件
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        RefreshUI();
    }

    public void UpdateProgress(int progress)
    {
        if (_questData == null) return;

        _questData.currentProgress = Mathf.Min(progress, _questData.targetCount);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_questData == null) return;

        // 任务名称
        if (questNameText != null)
            questNameText.text = _questData.questName;

        // 描述
        if (descriptionText != null)
            descriptionText.text = _questData.description;

        // 进度文字
        if (progressText != null)
        {
            progressText.text = $"{_questData.currentProgress}/{_questData.targetCount}";
        }

        // 进度条
        if (progressBar != null)
        {
            float fillAmount = (float)_questData.currentProgress / _questData.targetCount;
            progressBar.value = fillAmount;
        }

        if (progressFill != null)
        {
            if (IsCompleted)
            {
                progressFill.color = new Color(1f, 0.84f, 0f); // 金色
            }
            else
            {
                progressFill.color = new Color(0.2f, 0.8f, 0.2f); // 绿色
            }
        }

        // 奖励
        if (rewardAmountText != null)
        {
            rewardAmountText.text = $"+{_questData.rewardAmount}";
        }

        if (rewardIcon != null)
        {
            rewardIcon.sprite = GetRewardIcon(_questData.rewardType);
        }

        // 按钮状态
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (claimButton == null) return;

        if (IsRewardClaimed)
        {
            // 已领取
            claimButton.interactable = false;
            if (statusText != null)
            {
                statusText.text = "已领取";
                statusText.color = Color.gray;
            }
        }
        else if (IsCompleted)
        {
            // 可领取
            claimButton.interactable = true;
            if (statusText != null)
            {
                statusText.text = "可领取";
                statusText.color = new Color(0.2f, 0.8f, 0.2f);
            }
        }
        else
        {
            // 进行中
            claimButton.interactable = false;
            if (statusText != null)
            {
                statusText.text = "进行中";
                statusText.color = Color.gray;
            }
        }

        // 高亮显示
        if (claimableHighlight != null)
        {
            claimableHighlight.SetActive(CanClaimReward);
        }

        // 完成标记
        if (completedBadge != null)
        {
            completedBadge.SetActive(IsRewardClaimed);
        }
    }

    private void OnClaimClicked()
    {
        if (_parentUI != null && _questData != null)
        {
            _parentUI.OnClaimReward(_questData.questId);
        }
    }

    private Sprite GetRewardIcon(RewardType type)
    {
        // TODO: 返回对应的图标资源
        return null;
    }
}

/// <summary>
/// 任务数据
/// </summary>
public class QuestData
{
    public string questId;
    public string questName;
    public string description;
    public int targetCount;
    public int currentProgress;
    public bool isRewardClaimed;
    public RewardType rewardType;
    public int rewardAmount;
    public QuestType questType;
}

/// <summary>
/// 任务类型（与 DailyQuestManager.QuestType 保持一致）
/// </summary>
public enum QuestType
{
    KillMonster = 0,     // 击杀怪物
    UpgradeLevel = 1,    // 升级角色
    WatchAd = 2,         // 观看广告
    SpinGacha = 3,       // 抽卡
    EarnGold = 4,       // 赚取金币
    ReachWave = 5        // 到达波次
}