using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 玩家数据管理器
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public PlayerData Data { get; private set; }
    
    // 事件
    public UnityEvent<PlayerData> OnDataChanged;
    public UnityEvent<int> OnLevelUp;
    public UnityEvent<long> OnGoldChanged;

    private void Awake()
    {
        Data = new PlayerData();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void Initialize(PlayerData data)
    {
        Data = data;
        OnDataChanged?.Invoke(Data);
    }

    /// <summary>
    /// 升级角色
    /// </summary>
    public bool TryUpgrade()
    {
        long cost = Data.GetUpgradeCost();
        if (Data.gold >= cost)
        {
            Data.gold -= cost;
            Data.level++;
            
            Debug.Log($"[PlayerManager] Upgraded to Level {Data.level}!");
            
            OnGoldChanged?.Invoke(Data.gold);
            OnLevelUp?.Invoke(Data.level);
            OnDataChanged?.Invoke(Data);
            
            // 触发每日任务进度 - 升级角色
            if (DailyQuestManager.Instance != null)
            {
                DailyQuestManager.Instance.UpdateQuestProgress(QuestType.UpgradeLevel, 1);
            }
            
            return true;
        }
        
        Debug.Log($"[PlayerManager] Not enough gold to upgrade. Cost: {cost}, Have: {Data.gold}");
        return false;
    }

    /// <summary>
    /// 添加金币
    /// </summary>
    public void AddGold(long amount)
    {
        Data.gold += amount;
        Data.totalGoldEarned += amount;
        OnGoldChanged?.Invoke(Data.gold);
        OnDataChanged?.Invoke(Data);
    }

    /// <summary>
    /// 消耗金币
    /// </summary>
    public bool TrySpendGold(long amount)
    {
        if (Data.gold >= amount)
        {
            Data.gold -= amount;
            OnGoldChanged?.Invoke(Data.gold);
            OnDataChanged?.Invoke(Data);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取每秒金币收益（用于离线计算）
    /// </summary>
    public float GetGoldPerSecond()
    {
        return Data.GetCurrentAttack() * 0.5f;
    }

    /// <summary>
    /// 获取暴击伤害
    /// </summary>
    public float CalculateDamage(float baseDamage)
    {
        float damage = baseDamage;
        if (Random.value < Data.critRate)
        {
            damage *= Data.critDamage;
            Debug.Log("[PlayerManager] CRITICAL HIT!");
        }
        return damage;
    }

    /// <summary>
    /// 从基础伤害计算最终伤害（已判定暴击）
    /// 注意：PlayerAttack 中 baseDamage 已包含暴击加成，这里直接返回
    /// </summary>
    public float CalculateDamageFromBase(float baseDamage, bool isCrit)
    {
        return baseDamage;
    }

    /// <summary>
    /// 获取装备提供的总属性加成
    /// </summary>
    public EquipmentBonus GetEquipmentBonuses()
    {
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance.GetEquipmentBonuses();
        }
        return new EquipmentBonus();
    }

    /// <summary>
    /// 获取装备提供的攻击加成
    /// </summary>
    public float GetEquipmentAttackBonus()
    {
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance.TotalAttackBonus;
        }
        return 0f;
    }

    /// <summary>
    /// 获取装备提供的生命加成
    /// </summary>
    public float GetEquipmentHealthBonus()
    {
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance.TotalHealthBonus;
        }
        return 0f;
    }

    /// <summary>
    /// 获取装备提供的暴击率加成
    /// </summary>
    public float GetEquipmentCritRateBonus()
    {
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance.TotalCritRateBonus;
        }
        return 0f;
    }

    /// <summary>
    /// 获取装备提供的暴击伤害加成
    /// </summary>
    public float GetEquipmentCritDamageBonus()
    {
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance.TotalCritDamageBonus;
        }
        return 0f;
    }

    /// <summary>
    /// 获取当前波次奖励加成
    /// </summary>
    public float GetWaveBonus()
    {
        return 1f + (Data.highestWave * 0.05f);
    }

    /// <summary>
    /// 添加宝箱到背包
    /// </summary>
    public void AddChest(string chestId)
    {
        if (Data.chestInventory == null)
        {
            Data.chestInventory = new System.Collections.Generic.List<string>();
        }
        Data.chestInventory.Add(chestId);
        Debug.Log($"[PlayerManager] Added chest to inventory: {chestId}. Total chests: {Data.chestInventory.Count}");
        OnDataChanged?.Invoke(Data);
    }

    /// <summary>
    /// 获取宝箱背包列表
    /// </summary>
    public List<string> GetChestInventory()
    {
        return Data.chestInventory ?? new List<string>();
    }

    /// <summary>
    /// 打开一个宝箱（移除第一个）
    /// </summary>
    public string OpenChest()
    {
        if (Data.chestInventory != null && Data.chestInventory.Count > 0)
        {
            string chestId = Data.chestInventory[0];
            Data.chestInventory.RemoveAt(0);
            OnDataChanged?.Invoke(Data);
            return chestId;
        }
        return null;
    }
}
