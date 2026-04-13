using System;

/// <summary>
/// 每日任务数据
/// </summary>
[System.Serializable]
public class DailyQuest
{
    public string questId;           // 任务ID
    public string title;             // 任务标题
    public string description;       // 任务描述
    public QuestType type;           // 任务类型
    public int target;               // 目标数量
    public int progress;             // 当前进度
    public RewardType rewardType;   // 奖励类型
    public long rewardAmount;        // 奖励数量
    public bool claimed;             // 是否已领取
    public bool completed;           // 是否完成（进度达标）
    public bool isNew;               // 是否新任务（未读）

    /// <summary>
    /// 获取完成百分比
    /// </summary>
    public float GetProgressPercent()
    {
        if (target <= 0) return 0;
        return Mathf.Clamp01((float)progress / target);
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    public static DailyQuest Create(string id, string title, string desc, QuestType type, 
        int target, RewardType reward, long amount)
    {
        return new DailyQuest
        {
            questId = id,
            title = title,
            description = desc,
            type = type,
            target = target,
            progress = 0,
            rewardType = reward,
            rewardAmount = amount,
            claimed = false,
            completed = false,
            isNew = true
        };
    }
}

/// <summary>
/// 任务类型
/// </summary>
[Serializable]
public enum QuestType 
{ 
    KillMonster,     // 击杀怪物
    UpgradeLevel,    // 升级角色
    WatchAd,         // 观看广告
    SpinGacha,       // 抽卡
    EarnGold,       // 赚取金币
    ReachWave       // 到达波次
}

/// <summary>
/// 每日任务数据容器
/// </summary>
[System.Serializable]
public class DailyQuestData
{
    public string lastResetDate;          // 上次重置日期
    public int totalQuestsCount;         // 已完成任务总数（用于ID生成）
    public System.DateTime lastResetTime; // 上次重置时间
}

/// <summary>
/// 每日任务配置
/// </summary>
[System.Serializable]
public class QuestConfig
{
    public QuestType type;
    public string title;
    public string description;
    public int target;
    public RewardType rewardType;
    public long rewardAmount;

    public static QuestConfig[] DefaultQuests = new QuestConfig[]
    {
        new QuestConfig { type = QuestType.KillMonster, title = "怪物猎人", description = "击杀10个怪物", target = 10, rewardType = RewardType.Gold, rewardAmount = 1000 },
        new QuestConfig { type = QuestType.KillMonster, title = "杀戮盛宴", description = "击杀50个怪物", target = 50, rewardType = RewardType.Gold, rewardAmount = 5000 },
        new QuestConfig { type = QuestType.UpgradeLevel, title = "升阶", description = "升级角色3次", target = 3, rewardType = RewardType.Gold, rewardAmount = 2000 },
        new QuestConfig { type = QuestType.UpgradeLevel, title = "突破", description = "升级角色10次", target = 10, rewardType = RewardType.Chest, rewardAmount = 1 },
        new QuestConfig { type = QuestType.WatchAd, title = "广告赞助", description = "观看3个广告", target = 3, rewardType = RewardType.Gold, rewardAmount = 500 },
        new QuestConfig { type = QuestType.WatchAd, title = "VIP赞助", description = "观看10个广告", target = 10, rewardType = RewardType.Gold, rewardAmount = 2000 },
        new QuestConfig { type = QuestType.ReachWave, title = "征服者", description = "到达波次50", target = 50, rewardType = RewardType.Gold, rewardAmount = 10000 },
        new QuestConfig { type = QuestType.ReachWave, title = "无尽挑战", description = "到达波次100", target = 100, rewardType = RewardType.Chest, rewardAmount = 1 },
        new QuestConfig { type = QuestType.EarnGold, title = "敛金者", description = "累计获得50000金币", target = 50000, rewardType = RewardType.Gold, rewardAmount = 5000 },
        new QuestConfig { type = QuestType.EarnGold, title = "财富自由", description = "累计获得200000金币", target = 200000, rewardType = RewardType.Gold, rewardAmount = 20000 },
    };
}
