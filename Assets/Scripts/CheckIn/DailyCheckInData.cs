using System;
using System.Collections.Generic;

/// <summary>
/// 签到系统数据
/// </summary>
[System.Serializable]
public class CheckInDay
{
    public int day;                    // 第几天 (1-7)
    public bool claimed;               // 是否已领取
    public RewardType rewardType;      // 奖励类型
    public long rewardAmount;          // 奖励数量

    public static CheckInDay Create(int day, RewardType type, long amount)
    {
        return new CheckInDay
        {
            day = day,
            claimed = false,
            rewardType = type,
            rewardAmount = amount
        };
    }
}

/// <summary>
/// 奖励类型
/// </summary>
public enum RewardType 
{ 
    Gold,       // 金币
    Gem,        // 钻石
    Item,       // 物品
    Chest       // 宝箱
}

/// <summary>
/// 签到数据
/// </summary>
[System.Serializable]
public class DailyCheckInData
{
    public int currentStreak;          // 连续签到天数
    public string lastCheckInDate;     // 最后签到日期 (yyyy-MM-dd)
    public List<CheckInDay> weekRewards; // 本周奖励列表
    public bool hasCheckedInToday;     // 今日是否已签到

    /// <summary>
    /// 初始化本周奖励
    /// </summary>
    public void InitializeWeekRewards()
    {
        if (weekRewards == null)
            weekRewards = new List<CheckInDay>();

        weekRewards.Clear();

        // 7天签到奖励配置
        weekRewards.Add(CheckInDay.Create(1, RewardType.Gold, 1000));
        weekRewards.Add(CheckInDay.Create(2, RewardType.Gold, 2000));
        weekRewards.Add(CheckInDay.Create(3, RewardType.Gold, 3000));
        weekRewards.Add(CheckInDay.Create(4, RewardType.Chest, 1));      // 装备宝箱
        weekRewards.Add(CheckInDay.Create(5, RewardType.Gold, 5000));
        weekRewards.Add(CheckInDay.Create(6, RewardType.Gold, 8000));
        weekRewards.Add(CheckInDay.Create(7, RewardType.Chest, 2));      // 传说宝箱
    }

    /// <summary>
    /// 获取今日奖励
    /// </summary>
    public CheckInDay GetTodayReward()
    {
        if (weekRewards == null || weekRewards.Count == 0)
            return null;

        int dayIndex = (currentStreak - 1) % 7;
        if (dayIndex < 0 || dayIndex >= weekRewards.Count)
            return null;

        return weekRewards[dayIndex];
    }
}
