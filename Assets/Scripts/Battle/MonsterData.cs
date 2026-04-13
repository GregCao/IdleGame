using System;

/// <summary>
/// 怪物数据结构
/// </summary>
[System.Serializable]
public class MonsterData
{
    public int monsterId;           // 怪物ID
    public string monsterName;     // 怪物名称
    public float maxHealth;        // 最大生命值
    public float currentHealth;    // 当前生命值
    public float attack;           // 攻击力
    public long rewardGold;        // 击杀奖励金币
    public int wave;               // 所属波次

    /// <summary>
    /// 根据玩家等级生成怪物
    /// </summary>
    public static MonsterData CreateByPlayerLevel(int playerLevel, int wave)
    {
        float healthMultiplier = 1f + (wave * 0.3f);
        
        return new MonsterData
        {
            monsterId = wave,
            monsterName = GetMonsterName(wave),
            maxHealth = playerLevel * 50 * healthMultiplier,
            currentHealth = playerLevel * 50 * healthMultiplier,
            attack = playerLevel * 2 * (1f + wave * 0.1f),
            rewardGold = playerLevel * 20 * (1 + wave / 10),
            wave = wave
        };
    }

    /// <summary>
    /// 获取怪物名称
    /// </summary>
    private static string GetMonsterName(int wave)
    {
        string[] names = { "哥布林", "骷髅兵", "狼人", "食人魔", "巨魔", "巨龙", "恶魔", "天使", "泰坦", "神王" };
        int index = Mathf.Clamp(wave / 10, 0, names.Length - 1);
        return $"{names[index]} Lv.{wave}";
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
    }

    /// <summary>
    /// 是否死亡
    /// </summary>
    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    /// <summary>
    /// 获取生命百分比
    /// </summary>
    public float GetHealthPercent()
    {
        if (maxHealth <= 0) return 0;
        return currentHealth / maxHealth;
    }
}
