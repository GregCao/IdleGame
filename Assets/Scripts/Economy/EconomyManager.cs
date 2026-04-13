using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 经济系统管理器 - 金币管理核心
/// </summary>
public class EconomyManager : MonoBehaviour
{
    [Header("离线收益配置")]
    [SerializeField] private float _maxOfflineHours = 8f;           // 最大离线小时数
    [SerializeField] private float _offlineGoldPerAttack = 0.5f;    // 每点攻击每秒产出
    
    private const string OFFLINE_KEY = "LastLogoutTime";
    private bool _hasShownOfflinePopupThisSession = false;  // 防止在同一 session 内重复弹出离线奖励
    
    // 事件
    public UnityEvent<long> OnOfflineEarningsCalculated;  // 离线收益已计算
    public UnityEvent<long, long> OnGoldChanged;          // 金币变化 (old, new)

    /// <summary>
    /// 计算离线收益
    /// </summary>
    public void CalculateOfflineEarnings()
    {
        if (!PlayerPrefs.HasKey(OFFLINE_KEY))
        {
            Debug.Log("[EconomyManager] First time playing. No offline earnings.");
            return;
        }
        
        string lastLogoutStr = PlayerPrefs.GetString(OFFLINE_KEY);
        if (!System.DateTime.TryParse(lastLogoutStr, out System.DateTime lastLogout))
        {
            Debug.LogWarning("[EconomyManager] Failed to parse last logout time.");
            return;
        }
        
        System.TimeSpan offlineTime = System.DateTime.Now - lastLogout;
        double offlineSeconds = offlineTime.TotalSeconds;
        double maxOfflineSeconds = _maxOfflineHours * 3600;
        double effectiveSeconds = Mathf.Min((float)offlineSeconds, (float)maxOfflineSeconds);
        
        if (effectiveSeconds <= 0)
        {
            Debug.Log("[EconomyManager] Offline time is zero or negative.");
            return;
        }
        
        // 计算离线收益
        float goldPerSecond = GameManager.Instance.PlayerManager.GetGoldPerSecond();
        long offlineGold = (long)(goldPerSecond * effectiveSeconds);
        
        Debug.Log($"[EconomyManager] Offline for {offlineTime.Hours}h {offlineTime.Minutes}m. " +
                  $"Calculated earnings: {offlineGold} gold (rate: {goldPerSecond}/s)");
        
        // 发放离线收益
        if (offlineGold > 0)
        {
            long oldGold = GameManager.Instance.PlayerManager.Data.gold;
            GameManager.Instance.PlayerManager.AddGold(offlineGold);
            
            OnGoldChanged?.Invoke(oldGold, GameManager.Instance.PlayerManager.Data.gold);
            
            // 只在首次（启动时）弹出离线奖励，避免 RefreshAllUI 重复触发
            if (!_hasShownOfflinePopupThisSession)
            {
                _hasShownOfflinePopupThisSession = true;
                OnOfflineEarningsCalculated?.Invoke(offlineGold);
            }
        }

        // 清除离线时间戳，防止重复发放
        PlayerPrefs.DeleteKey(OFFLINE_KEY);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 保存离线时间戳
    /// </summary>
    public void SaveLogoutTime()
    {
        PlayerPrefs.SetString(OFFLINE_KEY, System.DateTime.Now.ToString());
        PlayerPrefs.Save();
        Debug.Log("[EconomyManager] Logout time saved.");
    }

    /// <summary>
    /// 获取离线金币每秒产出
    /// </summary>
    public float GetGoldPerSecond()
    {
        return GameManager.Instance.PlayerManager.GetGoldPerSecond();
    }

    /// <summary>
    /// 检查金币是否足够
    /// </summary>
    public bool HasEnoughGold(long amount)
    {
        return GameManager.Instance.PlayerManager.Data.gold >= amount;
    }

    /// <summary>
    /// 获取离线秒数上限
    /// </summary>
    public float GetMaxOfflineSeconds()
    {
        return _maxOfflineHours * 3600;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveLogoutTime();
        }
    }

    private void OnApplicationQuit()
    {
        SaveLogoutTime();
    }
}
