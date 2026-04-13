using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 每日任务管理器 - 单例
/// </summary>
public class DailyQuestManager : Singleton<DailyQuestManager>
{
    [Header("任务配置")]
    [SerializeField] private int _maxDailyQuests = 5;  // 每日任务数量

    private DailyQuestData _questData;
    private List<DailyQuest> _dailyQuests;
    private bool _isInitialized = false;

    // 事件
    public UnityEvent<DailyQuest> OnQuestUpdated;       // 任务进度更新
    public UnityEvent<DailyQuest> OnQuestCompleted;     // 任务完成
    public UnityEvent<DailyQuest, long> OnQuestClaimed; // 任务奖励领取

    public List<DailyQuest> DailyQuests => _dailyQuests;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    private void Start()
    {
        CheckAndResetQuests();
    }

    /// <summary>
    /// 加载任务数据
    /// </summary>
    private void LoadData()
    {
        if (_isInitialized) return;

        string key = "DailyQuestData";
        if (PlayerPrefs.HasKey(key))
        {
            try
            {
                string json = PlayerPrefs.GetString(key);
                _questData = JsonUtility.FromJson<DailyQuestData>(json);
            }
            catch
            {
                _questData = new DailyQuestData();
            }
        }

        if (_questData == null)
        {
            _questData = new DailyQuestData
            {
                lastResetDate = "",
                totalQuestsCount = 0,
                lastResetTime = DateTime.MinValue
            };
        }

        // 加载今日任务
        LoadTodayQuests();

        _isInitialized = true;
        Debug.Log($"[Quest] Loaded {_dailyQuests.Count} quests");
    }

    /// <summary>
    /// 加载今日任务
    /// </summary>
    private void LoadTodayQuests()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string questsKey = $"DailyQuests_{today}";

        _dailyQuests = new List<DailyQuest>();

        if (PlayerPrefs.HasKey(questsKey))
        {
            try
            {
                string json = PlayerPrefs.GetString(questsKey);
                var wrapper = JsonUtility.FromJson<QuestListWrapper>(json);
                if (wrapper != null && wrapper.quests != null)
                {
                    _dailyQuests = wrapper.quests;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Quest] Failed to load quests: {e.Message}");
                GenerateNewQuests();
            }
        }
        else
        {
            GenerateNewQuests();
        }
    }

    /// <summary>
    /// 生成新任务
    /// </summary>
    private void GenerateNewQuests()
    {
        _dailyQuests = new List<DailyQuest>();
        _questData.totalQuestsCount++;

        // 使用 QuestUI 的固定 GDD 模板，保持两套系统数据一致
        // 避免 DailyQuestManager 随机生成与 QuestUI 固定任务脱节
        var templates = IdleGame.UI.QuestUI.GetQuestTemplates();
        int idx = 0;
        foreach (var t in templates)
        {
            if (idx >= _maxDailyQuests) break;
            _dailyQuests.Add(DailyQuest.Create(
                $"quest_{_questData.totalQuestsCount}_{idx}",
                t.questName,
                t.description,
                t.questType,
                t.targetCount,
                t.rewardType,
                t.rewardAmount
            ));
            idx++;
        }

        SaveTodayQuests();
    }

    /// <summary>
    /// 保存今日任务
    /// </summary>
    public void SaveTodayQuests()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string questsKey = $"DailyQuests_{today}";

        var wrapper = new QuestListWrapper { quests = _dailyQuests };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(questsKey, json);
        PlayerPrefs.SetString("DailyQuestData", JsonUtility.ToJson(_questData));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 检查并重置任务（跨天）
    /// </summary>
    private void CheckAndResetQuests()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (_questData.lastResetDate != today)
        {
            _questData.lastResetDate = today;
            _questData.lastResetTime = DateTime.Now;

            GenerateNewQuests();
            SaveTodayQuests();

            Debug.Log("[Quest] Quests reset for new day");
        }
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public void UpdateQuestProgress(QuestType type, int amount = 1)
    {
        if (_dailyQuests == null) return;

        foreach (var quest in _dailyQuests)
        {
            if (quest.type == type && !quest.claimed && quest.progress < quest.target)
            {
                quest.progress += amount;
                quest.isNew = false;

                if (quest.progress >= quest.target)
                {
                    quest.completed = true;
                    OnQuestCompleted?.Invoke(quest);
                    Debug.Log($"[Quest] Quest completed: {quest.title}");
                }
                else
                {
                    OnQuestUpdated?.Invoke(quest);
                }

                SaveTodayQuests();
            }
        }
    }

    /// <summary>
    /// 领取任务奖励
    /// </summary>
    public bool ClaimQuestReward(string questId)
    {
        var quest = _dailyQuests.Find(q => q.questId == questId);
        if (quest == null || !quest.completed || quest.claimed)
        {
            Debug.Log($"[Quest] Cannot claim quest: {questId}");
            return false;
        }

        quest.claimed = true;

        // 发放奖励
        GrantReward(quest);

        OnQuestClaimed?.Invoke(quest, quest.rewardAmount);
        SaveTodayQuests();

        Debug.Log($"[Quest] Reward claimed: {quest.title} (+{quest.rewardAmount} {quest.rewardType})");
        return true;
    }

    /// <summary>
    /// 领取奖励
    /// </summary>
    private void GrantReward(DailyQuest quest)
    {
        switch (quest.rewardType)
        {
            case RewardType.Gold:
                GameManager.Instance.PlayerManager.AddGold(quest.rewardAmount);
                GameManager.Instance.UIManager.ShowReward($"任务奖励 +{quest.rewardAmount} 金币");
                break;

            case RewardType.Gem:
                Debug.Log($"[Quest] Gem reward: {quest.rewardAmount}");
                break;

            case RewardType.Chest:
                Debug.Log($"[Quest] Chest reward: x{quest.rewardAmount}");
                break;

            case RewardType.Item:
                Debug.Log($"[Quest] Item reward: x{quest.rewardAmount}");
                break;
        }
    }

    /// <summary>
    /// 获取可领取的任务数量
    /// </summary>
    public int GetClaimableCount()
    {
        int count = 0;
        foreach (var quest in _dailyQuests)
        {
            if (quest.completed && !quest.claimed)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 获取新任务数量
    /// </summary>
    public int GetNewQuestCount()
    {
        int count = 0;
        foreach (var quest in _dailyQuests)
        {
            if (quest.isNew)
                count++;
        }
        return count;
    }
}

/// <summary>
/// 任务列表包装器（用于JSON序列化）
/// </summary>
[System.Serializable]
public class QuestListWrapper
{
    public List<DailyQuest> quests;
}
