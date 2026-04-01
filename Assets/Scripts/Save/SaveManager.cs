using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 存档数据容器 - 包含所有需要保存的数据
/// </summary>
[System.Serializable]
public class SaveData
{
    public PlayerData playerData;
    public List<EquipmentData> equipmentData;
}

/// <summary>
/// 存档管理器 - 负责玩家数据持久化
/// </summary>
public class SaveManager : MonoBehaviour
{
    [Header("存档配置")]
    [SerializeField] private float _autoSaveInterval = 30f;  // 自动存档间隔（秒）
    
    private const string PLAYER_DATA_KEY = "PlayerData";
    private const string EQUIPMENT_DATA_KEY = "EquipmentData";
    private float _autoSaveTimer = 0f;

    private void Start()
    {
        // 开始时加载数据
        LoadPlayerData();
        
        // 订阅存档相关事件
        SubscribeToSaveEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromSaveEvents();
    }

    private void Update()
    {
        // 自动存档
        _autoSaveTimer += Time.deltaTime;
        if (_autoSaveTimer >= _autoSaveInterval)
        {
            _autoSaveTimer = 0f;
            SaveAllData();
        }
    }

    /// <summary>
    /// 订阅需要触发存档的事件
    /// </summary>
    private void SubscribeToSaveEvents()
    {
        if (GameManager.Instance?.PlayerManager != null)
        {
            GameManager.Instance.PlayerManager.OnLevelUp.AddListener(_ => SaveAllData());
            GameManager.Instance.PlayerManager.OnGoldChanged.AddListener(_ => SaveAllData());
        }
    }

    /// <summary>
    /// 取消订阅事件（防止重复订阅和内存泄漏）
    /// </summary>
    private void UnsubscribeFromSaveEvents()
    {
        if (GameManager.Instance?.PlayerManager != null)
        {
            GameManager.Instance.PlayerManager.OnLevelUp.RemoveListener(_ => SaveAllData());
            GameManager.Instance.PlayerManager.OnGoldChanged.RemoveListener(_ => SaveAllData());
        }
    }

    /// <summary>
    /// 保存玩家数据
    /// </summary>
    public void SavePlayerData()
    {
        try
        {
            PlayerData data = GameManager.Instance.PlayerManager.Data;
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
            PlayerPrefs.Save();
            
            Debug.Log($"[SaveManager] Player data saved. Level: {data.level}, Gold: {data.gold}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save player data: {e.Message}");
        }
    }

    /// <summary>
    /// 加载玩家数据
    /// </summary>
    public void LoadPlayerData()
    {
        try
        {
            if (PlayerPrefs.HasKey(PLAYER_DATA_KEY))
            {
                string json = PlayerPrefs.GetString(PLAYER_DATA_KEY);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                
                if (data != null)
                {
                    // 更新最后登录时间
                    data.lastLoginTime = DateTime.Now;
                    
                    GameManager.Instance.PlayerManager.Initialize(data);
                    
                    Debug.Log($"[SaveManager] Player data loaded. Level: {data.level}, Gold: {data.gold}");
                }
                else
                {
                    Debug.LogWarning("[SaveManager] Loaded data is null. Creating new data.");
                    CreateNewPlayerData();
                }
            }
            else
            {
                Debug.Log("[SaveManager] No saved data found. Creating new player.");
                CreateNewPlayerData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load player data: {e.Message}");
            CreateNewPlayerData();
        }
    }

    /// <summary>
    /// 创建新玩家数据
    /// </summary>
    private void CreateNewPlayerData()
    {
        PlayerData newData = new PlayerData
        {
            level = 1,
            gold = 0,
            baseAttack = 10f,
            baseHealth = 100f,
            critRate = 0.1f,
            critDamage = 1.5f,
            totalGoldEarned = 0,
            highestWave = 0,
            totalDamageDealt = 0,
            monstersKilled = 0,
            lastLoginTime = DateTime.Now
        };
        
        GameManager.Instance.PlayerManager.Initialize(newData);
        SavePlayerData();
        
        Debug.Log("[SaveManager] New player data created.");
    }

    /// <summary>
    /// 保存所有数据（玩家 + 装备），静默版用于自动存档
    /// </summary>
    private void SaveAllData()
    {
        try
        {
            PlayerData data = GameManager.Instance.PlayerManager.Data;
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
            
            // 同时保存装备数据
            if (EquipmentManager.Instance != null)
            {
                List<EquipmentData> equipmentList = EquipmentManager.Instance.GetOwnedEquipment();
                string equipJson = JsonUtility.ToJson(new SerializableList<EquipmentData>(equipmentList));
                PlayerPrefs.SetString(EQUIPMENT_DATA_KEY, equipJson);
            }
            
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Save failed: {e.Message}");
        }
    }

    /// <summary>
    /// 删除存档（重置）
    /// </summary>
    public void DeletePlayerData()
    {
        if (PlayerPrefs.HasKey(PLAYER_DATA_KEY))
        {
            PlayerPrefs.DeleteKey(PLAYER_DATA_KEY);
        }
        if (PlayerPrefs.HasKey(EQUIPMENT_DATA_KEY))
        {
            PlayerPrefs.DeleteKey(EQUIPMENT_DATA_KEY);
        }
        PlayerPrefs.Save();
        Debug.Log("[SaveManager] All save data deleted.");
    }

    /// <summary>
    /// 获取存档是否存在
    /// </summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(PLAYER_DATA_KEY);
    }

    /// <summary>
    /// 保存装备数据
    /// </summary>
    public void SaveEquipmentData(EquipmentManager equipmentManager)
    {
        try
        {
            if (equipmentManager == null) return;
            
            List<EquipmentData> equipmentList = equipmentManager.GetOwnedEquipment();
            string json = JsonUtility.ToJson(new SerializableList<EquipmentData>(equipmentList));
            PlayerPrefs.SetString(EQUIPMENT_DATA_KEY, json);
            PlayerPrefs.Save();
            
            Debug.Log($"[SaveManager] Equipment data saved. Count: {equipmentList.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save equipment data: {e.Message}");
        }
    }

    /// <summary>
    /// 加载游戏数据（包含玩家数据和装备数据）
    /// </summary>
    public SaveData LoadGame()
    {
        try
        {
            if (PlayerPrefs.HasKey(PLAYER_DATA_KEY))
            {
                string playerJson = PlayerPrefs.GetString(PLAYER_DATA_KEY);
                string equipJson = PlayerPrefs.GetString(EQUIPMENT_DATA_KEY);
                
                SaveData data = new SaveData();
                data.playerData = JsonUtility.FromJson<PlayerData>(playerJson);
                
                if (!string.IsNullOrEmpty(equipJson))
                {
                    var serializableList = JsonUtility.FromJson<SerializableList<EquipmentData>>(equipJson);
                    data.equipmentData = serializableList != null ? serializableList.items : new List<EquipmentData>();
                }
                else
                {
                    data.equipmentData = new List<EquipmentData>();
                }
                
                Debug.Log($"[SaveManager] Game data loaded. Player: {data.playerData?.level}, Equipment: {data.equipmentData?.Count}");
                return data;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load game data: {e.Message}");
        }
        
        return null;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveAllData();
        }
    }

    private void OnApplicationQuit()
    {
        SaveAllData();
    }
}

/// <summary>
/// 可序列化的列表包装器（用于Unity的JsonUtility）
/// </summary>
[System.Serializable]
public class SerializableList<T>
{
    public List<T> items;
    
    public SerializableList()
    {
        items = new List<T>();
    }
    
    public SerializableList(List<T> list)
    {
        items = list ?? new List<T>();
    }
}
