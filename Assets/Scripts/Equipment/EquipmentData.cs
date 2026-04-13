using System;

/// <summary>
/// 装备位置枚举
/// </summary>
public enum EquipmentSlot
{
    Weapon,    // 武器
    Armor,     // 防具
    Accessory  // 饰品
}

/// <summary>
/// 装备品质枚举
/// </summary>
public enum EquipmentQuality
{
    Common,     // 普通
    Rare,       // 稀有
    Epic,       // 史诗
    Legendary   // 传说
}

/// <summary>
/// 装备数据
/// </summary>
[System.Serializable]
public class EquipmentData
{
    public string equipmentId;        // 唯一ID
    public string name;               // 装备名称
    public EquipmentSlot slot;        // 装备位置
    public EquipmentQuality quality;   // 品质
    public int level;                 // 强化等级
    public float attackBonus;         // 攻击加成
    public float healthBonus;         // 生命加成
    public float critRateBonus;       // 暴击率加成
    public float critDamageBonus;     // 暴击伤害加成
    public long baseCost;             // 基础强化消耗金币
    
    /// <summary>
    /// 当前强化消耗金币
    /// 公式：baseCost * (1.5 ^ level)
    /// </summary>
    public long Cost => (long)(baseCost * Math.Pow(1.5, level));
    
    /// <summary>
    /// 根据等级计算属性加成
    /// 公式：baseValue * (1 + level * 0.1)
    /// </summary>
    public float GetAttackBonus(float baseValue) => baseValue * (1 + level * 0.1f);
    public float GetHealthBonus(float baseValue) => baseValue * (1 + level * 0.1f);
    public float GetCritRateBonus(float baseValue) => baseValue * (1 + level * 0.1f);
    public float GetCritDamageBonus(float baseValue) => baseValue * (1 + level * 0.1f);
    
    /// <summary>
    /// 创建装备副本
    /// </summary>
    public EquipmentData Clone()
    {
        return new EquipmentData
        {
            equipmentId = this.equipmentId,
            name = this.name,
            slot = this.slot,
            quality = this.quality,
            level = this.level,
            attackBonus = this.attackBonus,
            healthBonus = this.healthBonus,
            critRateBonus = this.critRateBonus,
            critDamageBonus = this.critDamageBonus,
            baseCost = this.baseCost
        };
    }
}
