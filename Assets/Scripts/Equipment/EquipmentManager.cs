using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 装备管理器 - 单例
/// 管理已拥有的装备列表、穿戴/卸下装备、强化装备、计算装备属性加成
/// </summary>
public class EquipmentManager : Singleton<EquipmentManager>
{
    // 装备列表
    private List<EquipmentData> ownedEquipment = new List<EquipmentData>();
    
    // 当前穿戴的装备（槽位：0=武器, 1=防具, 2=饰品）
    private EquipmentData[] equippedItems = new EquipmentData[3];
    
    // 属性加成缓存
    private float totalAttackBonus;
    private float totalHealthBonus;
    private float totalCritRateBonus;
    private float totalCritDamageBonus;
    
    protected override void Awake()
    {
        base.Awake();
        LoadEquipment();
    }
    
    /// <summary>
    /// 获取总攻击加成
    /// </summary>
    public float TotalAttackBonus => totalAttackBonus;
    
    /// <summary>
    /// 获取总生命加成
    /// </summary>
    public float TotalHealthBonus => totalHealthBonus;
    
    /// <summary>
    /// 获取总暴击率加成
    /// </summary>
    public float TotalCritRateBonus => totalCritRateBonus;
    
    /// <summary>
    /// 获取总暴击伤害加成
    /// </summary>
    public float TotalCritDamageBonus => totalCritDamageBonus;
    
    /// <summary>
    /// 获取已拥有的所有装备
    /// </summary>
    public List<EquipmentData> GetOwnedEquipment()
    {
        return ownedEquipment;
    }
    
    /// <summary>
    /// 获取指定槽位当前穿戴的装备
    /// </summary>
    public EquipmentData GetEquippedItem(EquipmentSlot slot)
    {
        int index = GetSlotIndex(slot);
        return index >= 0 && index < equippedItems.Length ? equippedItems[index] : null;
    }
    
    /// <summary>
    /// 获取指定索引槽位当前穿戴的装备
    /// </summary>
    public EquipmentData GetEquippedItemByIndex(int index)
    {
        if (index < 0 || index >= equippedItems.Length) return null;
        return equippedItems[index];
    }
    
    /// <summary>
    /// 添加装备到背包
    /// </summary>
    public void AddEquipment(EquipmentData equipment)
    {
        if (equipment == null) return;
        
        // 生成唯一ID
        if (string.IsNullOrEmpty(equipment.equipmentId))
        {
            equipment.equipmentId = Guid.NewGuid().ToString();
        }
        
        ownedEquipment.Add(equipment);
        SaveEquipment();
        UpdateEquipmentBonuses();
        
        Debug.Log($"[EquipmentManager] 添加装备: {equipment.name}, 品质: {equipment.quality}");
    }
    
    /// <summary>
    /// 穿戴装备
    /// </summary>
    public bool EquipItem(EquipmentData equipment)
    {
        if (equipment == null) return false;
        
        int slotIndex = GetSlotIndex(equipment.slot);
        if (slotIndex < 0) return false;
        
        // 如果该槽位已有装备，先卸下
        if (equippedItems[slotIndex] != null)
        {
            UnequipItem(equipment.slot);
        }
        
        equippedItems[slotIndex] = equipment;
        UpdateEquipmentBonuses();
        SaveEquipment();
        
        Debug.Log($"[EquipmentManager] 穿戴装备: {equipment.name}");
        return true;
    }
    
    /// <summary>
    /// 卸下装备到背包
    /// </summary>
    public bool UnequipItem(EquipmentSlot slot)
    {
        int slotIndex = GetSlotIndex(slot);
        if (slotIndex < 0 || equippedItems[slotIndex] == null) return false;
        
        Debug.Log($"[EquipmentManager] 卸下装备: {equippedItems[slotIndex].name}");
        equippedItems[slotIndex] = null;
        UpdateEquipmentBonuses();
        SaveEquipment();
        
        return true;
    }
    
    /// <summary>
    /// 强化装备
    /// </summary>
    public bool UpgradeEquipment(string equipmentId)
    {
        EquipmentData equipment = ownedEquipment.Find(e => e.equipmentId == equipmentId);
        if (equipment == null)
        {
            Debug.LogWarning($"[EquipmentManager] 找不到装备: {equipmentId}");
            return false;
        }
        
        // 检查金币是否足够
        if (EconomyManager.Instance != null)
        {
            if (!EconomyManager.Instance.SpendGold(equipment.Cost))
            {
                Debug.Log($"[EquipmentManager] 金币不足，无法强化 {equipment.name}");
                return false;
            }
        }
        
        equipment.level++;
        UpdateEquipmentBonuses();
        SaveEquipment();
        
        Debug.Log($"[EquipmentManager] 强化成功: {equipment.name}, 等级: {equipment.level}, 消耗: {equipment.Cost}");
        
        // 通知UI显示强化特效
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEquipmentUpgradeEffect(equipment);
        }
        
        return true;
    }
    
    /// <summary>
    /// 出售装备
    /// </summary>
    public bool SellEquipment(string equipmentId)
    {
        EquipmentData equipment = ownedEquipment.Find(e => e.equipmentId == equipmentId);
        if (equipment == null) return false;
        
        // 计算出售价格（强化等级的50%）
        long sellPrice = equipment.Cost / 2;
        
        ownedEquipment.Remove(equipment);
        
        // 如果已穿戴，卸下
        for (int i = 0; i < equippedItems.Length; i++)
        {
            if (equippedItems[i] != null && equippedItems[i].equipmentId == equipmentId)
            {
                equippedItems[i] = null;
            }
        }
        
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(sellPrice);
        }
        
        UpdateEquipmentBonuses();
        SaveEquipment();
        
        Debug.Log($"[EquipmentManager] 出售装备: {equipment.name}, 获得: {sellPrice}");
        return true;
    }
    
    /// <summary>
    /// 获取装备槽位索引
    /// </summary>
    private int GetSlotIndex(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return 0;
            case EquipmentSlot.Armor: return 1;
            case EquipmentSlot.Accessory: return 2;
            default: return -1;
        }
    }
    
    /// <summary>
    /// 更新所有装备属性加成
    /// </summary>
    private void UpdateEquipmentBonuses()
    {
        totalAttackBonus = 0;
        totalHealthBonus = 0;
        totalCritRateBonus = 0;
        totalCritDamageBonus = 0;
        
        foreach (EquipmentData equipment in equippedItems)
        {
            if (equipment != null)
            {
                totalAttackBonus += equipment.attackBonus;
                totalHealthBonus += equipment.healthBonus;
                totalCritRateBonus += equipment.critRateBonus;
                totalCritDamageBonus += equipment.critDamageBonus;
            }
        }
    }
    
    /// <summary>
    /// 获取装备提供的总属性加成
    /// </summary>
    public EquipmentBonus GetEquipmentBonuses()
    {
        return new EquipmentBonus
        {
            attackBonus = totalAttackBonus,
            healthBonus = totalHealthBonus,
            critRateBonus = totalCritRateBonus,
            critDamageBonus = totalCritDamageBonus
        };
    }
    
    /// <summary>
    /// 保存装备数据
    /// </summary>
    public void SaveEquipment()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveEquipmentData(this);
        }
    }
    
    /// <summary>
    /// 加载装备数据
    /// </summary>
    public void LoadEquipment()
    {
        if (SaveManager.Instance != null)
        {
            SaveData data = SaveManager.Instance.LoadGame();
            if (data != null && data.equipmentData != null)
            {
                ownedEquipment = data.equipmentData;
                
                // 恢复穿戴状态
                foreach (EquipmentData equipment in ownedEquipment)
                {
                    if (equipment != null && IsEquipped(equipment))
                    {
                        int index = GetSlotIndex(equipment.slot);
                        if (index >= 0)
                        {
                            equippedItems[index] = equipment;
                        }
                    }
                }
                
                UpdateEquipmentBonuses();
                Debug.Log($"[EquipmentManager] 加载装备数量: {ownedEquipment.Count}");
            }
        }
    }
    
    /// <summary>
    /// 检查装备是否已穿戴
    /// </summary>
    private bool IsEquipped(EquipmentData equipment)
    {
        if (equipment == null) return false;
        int index = GetSlotIndex(equipment.slot);
        return index >= 0 && index < equippedItems.Length && equippedItems[index] != null
            && equippedItems[index].equipmentId == equipment.equipmentId;
    }
}

/// <summary>
/// 装备加成数据结构
/// </summary>
[System.Serializable]
public struct EquipmentBonus
{
    public float attackBonus;
    public float healthBonus;
    public float critRateBonus;
    public float critDamageBonus;
}
