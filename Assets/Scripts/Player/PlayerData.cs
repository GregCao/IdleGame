using System;
using System.Collections.Generic;

/// <summary>
/// 玩家数据结构 - 可序列化存档
/// </summary>
[System.Serializable]
public class PlayerData
{
    // 基础属性
    public int level = 1;                    // 角色等级
    public long gold = 0;                    // 当前金币
    public float baseAttack = 10f;           // 基础攻击力
    public float baseHealth = 100f;          // 基础生命值
    public float critRate = 0.1f;             // 暴击率 (10%)
    public float critDamage = 1.5f;           // 暴击伤害 (150%)
    
    // 成长属性
    public float attackPerLevel = 5f;        // 每级攻击成长
    public float healthPerLevel = 20f;        // 每级生命成长
    
    // 离线收益相关
    public long totalGoldEarned = 0;         // 累计获得金币
    public DateTime lastLoginTime;            // 最后登录时间
    
    // 元数据
    public int highestWave = 0;              // 最高波次
    public long totalDamageDealt = 0;         // 总伤害
    public int monstersKilled = 0;            // 击杀怪物数

    // 宝箱相关
    public List<string> chestInventory = new List<string>();  // 宝箱背包

    // 任务系统相关
    public string lastQuestRefreshDate = "";     // 上次任务刷新日期 (yyyy-MM-dd)
    public int questRefreshCount = 0;             // 任务刷新次数（用于费用递增）

    /// <summary>
    /// 计算当前攻击力
    /// </summary>
    public float GetCurrentAttack()
    {
        return baseAttack + (level - 1) * attackPerLevel;
    }

    /// <summary>
    /// 计算当前生命值
    /// </summary>
    public float GetCurrentHealth()
    {
        return baseHealth + (level - 1) * healthPerLevel;
    }

    /// <summary>
    /// 计算升级消耗金币
    /// </summary>
    public long GetUpgradeCost()
    {
        return level * 100;
    }

    /// <summary>
    /// 重置为初始状态
    /// </summary>
    public void Reset()
    {
        level = 1;
        gold = 0;
        baseAttack = 10f;
        baseHealth = 100f;
        critRate = 0.1f;
        critDamage = 1.5f;
        totalGoldEarned = 0;
        highestWave = 0;
        totalDamageDealt = 0;
        monstersKilled = 0;
        lastLoginTime = DateTime.Now;
    }
}
