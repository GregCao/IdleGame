using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame.Task
{
    /// <summary>
    /// 任务类型枚举
    /// </summary>
    public enum TaskType
    {
        /// <summary>击杀怪物</summary>
        KillMonster = 1,

        /// <summary>通关副本</summary>
        ClearDungeon = 2,

        /// <summary>收集金币</summary>
        CollectGold = 3,

        /// <summary>升级装备</summary>
        UpgradeEquipment = 4,

        /// <summary>每日登录</summary>
        DailyLogin = 5,

        /// <summary>消耗体力</summary>
        ConsumeStamina = 6,

        /// <summary>参与竞技场</summary>
        ParticipateArena = 7,

        /// <summary>完成每日目标</summary>
        CompleteDailyGoal = 8,
    }

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskState
    {
        /// <summary>未解锁</summary>
        Locked = 0,

        /// <summary>可领取</summary>
        Claimable = 1,

        /// <summary>已完成并领取</summary>
        Claimed = 2,
    }

    /// <summary>
    /// 任务难度枚举
    /// </summary>
    public enum TaskDifficulty
    {
        /// <summary>简单</summary>
        Easy = 1,

        /// <summary>普通</summary>
        Normal = 2,

        /// <summary>困难</summary>
        Hard = 3,

        /// <summary>噩梦</summary>
        Nightmare = 4,
    }

    /// <summary>
    /// 奖励配置数据结构
    /// </summary>
    [Serializable]
    public class AwardConfig
    {
        /// <summary>奖励类型</summary>
        public string awardType;

        /// <summary>奖励物品ID</summary>
        public int itemId;

        /// <summary>奖励数量</summary>
        public int count;

        /// <summary>权重（用于随机奖励）</summary>
        public int weight = 1;
    }

    /// <summary>
    /// 任务配置数据结构（静态配置）
    /// </summary>
    [Serializable]
    public class TaskConfig
    {
        /// <summary>任务ID</summary>
        public int taskId;

        /// <summary>任务名称</summary>
        public string taskName;

        /// <summary>任务类型</summary>
        public TaskType taskType;

        /// <summary>任务难度</summary>
        public TaskDifficulty difficulty;

        /// <summary>目标数量</summary>
        public int targetCount;

        /// <summary>任务描述</summary>
        public string description;

        /// <summary>任务图标</summary>
        public string icon;

        /// <summary>刷新周期（天/周/永久）</summary>
        public string refreshCycle;

        /// <summary>解锁等级</summary>
        public int unlockLevel;

        /// <summary>奖励列表</summary>
        public List<AwardConfig> awards;

        /// <summary>是否启用</summary>
        public bool isEnabled = true;
    }

    /// <summary>
    /// 玩家任务进度数据结构（运行时数据）
    /// </summary>
    [Serializable]
    public class PlayerTask
    {
        /// <summary>任务ID</summary>
        public int taskId;

        /// <summary>当前进度</summary>
        public int currentProgress;

        /// <summary>任务状态</summary>
        public TaskState state;

        /// <summary>任务配置引用（不序列化）</summary>
        [NonSerialized]
        public TaskConfig config;

        /// <summary>领取时间</summary>
        public DateTime claimTime;

        /// <summary>是否满足领取条件</summary>
        public bool CanClaim => config != null && currentProgress >= config.targetCount && state == TaskState.Claimable;

        /// <summary>
        /// 增加进度
        /// </summary>
        public void AddProgress(int amount)
        {
            if (state == TaskState.Claimed || state == TaskState.Locked)
                return;

            currentProgress += amount;

            if (config != null && currentProgress >= config.targetCount)
            {
                state = TaskState.Claimable;
            }
        }

        /// <summary>
        /// 重置任务进度
        /// </summary>
        public void Reset()
        {
            currentProgress = 0;
            state = TaskState.Claimable;
            claimTime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// 任务刷新周期常量
    /// </summary>
    public static class TaskRefreshCycle
    {
        public const string Daily = "Daily";
        public const string Weekly = "Weekly";
        public const string Permanent = "Permanent";
    }
}
