using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 签到管理器 - 单例
/// </summary>
public class DailyCheckInManager : Singleton<DailyCheckInManager>
{
    [Header("签到配置")]
    [SerializeField] private int _maxClaimDays = 7;  // 7天循环

    private DailyCheckInData _data;
    private bool _isInitialized = false;

    // 事件
    public UnityEvent<int, CheckInDay> OnCheckInClaimed;   // 领取成功 (day, reward)
    public UnityEvent<int> OnStreakUpdated;                  // 连续天数更新
    public UnityEvent OnDateChanged;                         // 日期变更（跨天）

    public DailyCheckInData Data => _data;
    public bool CanCheckInToday => !_data.hasCheckedInToday;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    private void Start()
    {
        CheckAndReset();
    }

    /// <summary>
    /// 加载签到数据
    /// </summary>
    private void LoadData()
    {
        if (_isInitialized) return;

        string key = "DailyCheckInData";
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            _data = JsonUtility.FromJson<DailyCheckInData>(json);
        }

        if (_data == null)
        {
            _data = new DailyCheckInData();
            _data.InitializeWeekRewards();
        }

        _isInitialized = true;
        Debug.Log($"[CheckIn] Loaded. Streak: {_data.currentStreak}, CheckedIn: {_data.hasCheckedInToday}");
    }

    /// <summary>
    /// 保存签到数据
    /// </summary>
    public void SaveData()
    {
        string json = JsonUtility.ToJson(_data);
        PlayerPrefs.SetString("DailyCheckInData", json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 检查是否需要重置（跨天）
    /// </summary>
    private void CheckAndReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (_data.lastCheckInDate != today)
        {
            // 跨天了
            if (_data.hasCheckedInToday && !string.IsNullOrEmpty(_data.lastCheckInDate))
            {
                // 昨日已签到，连续天数+1
                _data.currentStreak++;
                OnStreakUpdated?.Invoke(_data.currentStreak);
            }
            else if (!string.IsNullOrEmpty(_data.lastCheckInDate))
            {
                // 昨日未签到，连续天数重置
                _data.currentStreak = 0;
                OnStreakUpdated?.Invoke(_data.currentStreak);
            }

            // 重置今日签到状态
            _data.hasCheckedInToday = false;

            // 重置周奖励（如果满7天）
            if (_data.currentStreak > 0 && (_data.currentStreak % 7) == 0)
            {
                _data.InitializeWeekRewards();
            }

            _data.lastCheckInDate = today;
            SaveData();
            OnDateChanged?.Invoke();
        }
    }

    /// <summary>
    /// 签到（免费）
    /// </summary>
    public void CheckIn()
    {
        if (_data.hasCheckedInToday)
        {
            Debug.Log("[CheckIn] Already checked in today!");
            return;
        }

        PerformCheckIn();
    }

    /// <summary>
    /// 看广告补签
    /// </summary>
    public void CheckInWithAd(Action<bool> callback)
    {
        // 调用广告系统
        if (GameManager.Instance?.AdManager != null)
        {
            GameManager.Instance.AdManager.ShowRewardedVideo(AdManager.RewardedScene.TrySummon, (success) =>
            {
                if (success)
                {
                    // 补签前一天（如果有漏签）
                    if (!_data.hasCheckedInToday)
                    {
                        PerformCheckIn();
                        callback?.Invoke(true);
                    }
                    else
                    {
                        callback?.Invoke(false);
                    }
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }
        else
        {
            // 编辑器模式模拟
            PerformCheckIn();
            callback?.Invoke(true);
        }
    }

    /// <summary>
    /// 执行签到
    /// </summary>
    private void PerformCheckIn()
    {
        CheckInDay reward = _data.GetTodayReward();
        if (reward == null)
        {
            Debug.LogError("[CheckIn] No reward available!");
            return;
        }

        // 标记已签到
        reward.claimed = true;
        _data.hasCheckedInToday = true;
        _data.lastCheckInDate = DateTime.Now.ToString("yyyy-MM-dd");

        // 连续天数+1
        _data.currentStreak++;

        // 发放奖励
        GrantReward(reward);

        SaveData();
        OnStreakUpdated?.Invoke(_data.currentStreak);
        OnCheckInClaimed?.Invoke(reward.day, reward);

        Debug.Log($"[CheckIn] Checked in! Day {_data.currentStreak}, Reward: {reward.rewardAmount} {reward.rewardType}");
    }

    /// <summary>
    /// 发放奖励
    /// </summary>
    private void GrantReward(CheckInDay reward)
    {
        switch (reward.rewardType)
        {
            case RewardType.Gold:
                GameManager.Instance.PlayerManager.AddGold(reward.rewardAmount);
                GameManager.Instance.UIManager.ShowReward($"签到奖励 +{reward.rewardAmount} 金币");
                break;

            case RewardType.Gem:
                // 钻石系统（待添加）
                Debug.Log($"[CheckIn] Gem reward: {reward.rewardAmount}");
                break;

            case RewardType.Chest:
                // 宝箱系统（待添加）
                Debug.Log($"[CheckIn] Chest reward: x{reward.rewardAmount}");
                break;

            case RewardType.Item:
                Debug.Log($"[CheckIn] Item reward: x{reward.rewardAmount}");
                break;
        }
    }

    /// <summary>
    /// 获取本周签到状态
    /// </summary>
    public bool[] GetWeekClaimStatus()
    {
        bool[] status = new bool[7];
        if (_data.weekRewards != null)
        {
            for (int i = 0; i < Mathf.Min(7, _data.weekRewards.Count); i++)
            {
                status[i] = _data.weekRewards[i].claimed;
            }
        }
        return status;
    }
}
