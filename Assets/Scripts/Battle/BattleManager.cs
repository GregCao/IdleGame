using UnityEngine;
using UnityEngine.Events;
using IdleGame;

/// <summary>
/// 战斗管理器 - 自动战斗核心
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("战斗配置")]
    [SerializeField] private float _playerAttackInterval = 1f;  // 玩家攻击间隔
    [SerializeField] private float _monsterAttackInterval = 2f;  // 怪物攻击间隔
    
    private float _playerAttackTimer = 0f;
    private float _monsterAttackTimer = 0f;
    
    private MonsterData _currentMonster;
    private bool _isInBattle = false;
    
    // 事件
    public UnityEvent<MonsterData> OnMonsterSpawned;
    public UnityEvent<MonsterData, float> OnMonsterDamaged;  // 怪物, 伤害值
    public UnityEvent<MonsterData, long> OnMonsterKilled;     // 击杀怪物, 奖励金币
    public UnityEvent<int> OnWaveChanged;
    public UnityEvent OnBattleStarted;
    public UnityEvent OnPlayerDamaged;
    public UnityEvent<float> OnPlayerHealed;

    public int CurrentWave { get; private set; } = 1;
    public MonsterData CurrentMonster => _currentMonster;
    public bool IsInBattle => _isInBattle;

    private void Awake()
    {
        CurrentWave = 1;
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle()
    {
        _isInBattle = true;
        _playerAttackTimer = 0f;
        _monsterAttackTimer = 0f;
        SpawnMonster();
        OnBattleStarted?.Invoke();
        Debug.Log("[BattleManager] Battle started.");
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    public void StopBattle()
    {
        _isInBattle = false;
    }

    /// <summary>
    /// 每秒Tick - 由GameManager调用
    /// </summary>
    public void OnTick()
    {
        if (!_isInBattle) return;
        
        // 玩家攻击（每次 OnTick 固定增加一个 tick 间隔，累积到攻击间隔后重置）
        _playerAttackTimer += _tickInterval;
        if (_playerAttackTimer >= _playerAttackInterval)
        {
            _playerAttackTimer = 0f;
            PlayerAttack();
        }
        
        // 怪物攻击
        _monsterAttackTimer += _tickInterval;
        if (_monsterAttackTimer >= _monsterAttackInterval && _currentMonster != null)
        {
            _monsterAttackTimer = 0f;
            MonsterAttack();
        }
    }

    /// <summary>
    /// 玩家攻击怪物
    /// </summary>
    private void PlayerAttack()
    {
        if (_currentMonster == null || _currentMonster.IsDead()) return;
        
        // 获取玩家基础攻击
        float baseAttack = GameManager.Instance.PlayerManager.Data.GetCurrentAttack();
        
        // 加上装备攻击加成
        float equipmentAttack = 0f;
        if (GameManager.Instance.PlayerManager != null)
        {
            equipmentAttack = GameManager.Instance.PlayerManager.GetEquipmentAttackBonus();
        }
        
        // 计算伤害：baseDamage = (playerAttack + equipmentAttack) * critMultiplier
        float critMultiplier = 1f;
        bool isCrit = Random.value < GameManager.Instance.PlayerManager.Data.critRate;
        if (isCrit)
        {
            critMultiplier = GameManager.Instance.PlayerManager.Data.critDamage;
            Debug.Log("[BattleManager] CRITICAL HIT!");
        }
        
        float baseDamage = (baseAttack + equipmentAttack) * critMultiplier;
        float finalDamage = GameManager.Instance.PlayerManager.CalculateDamageFromBase(baseDamage, isCrit);
        
        // 造成伤害
        _currentMonster.TakeDamage(finalDamage);
        GameManager.Instance.PlayerManager.Data.totalDamageDealt += (long)finalDamage;
        
        OnMonsterDamaged?.Invoke(_currentMonster, finalDamage);
        
        Debug.Log($"[BattleManager] Player dealt {finalDamage} damage to {_currentMonster.monsterName}");
        
        // 检查怪物死亡
        if (_currentMonster.IsDead())
        {
            KillMonster();
        }
    }

    /// <summary>
    /// 怪物攻击玩家
    /// </summary>
    private void MonsterAttack()
    {
        if (_currentMonster == null || _currentMonster.IsDead()) return;
        
        float damage = _currentMonster.attack;
        OnPlayerDamaged?.Invoke();
        
        Debug.Log($"[BattleManager] Monster dealt {damage} damage to player");
    }

    /// <summary>
    /// 生成新怪物
    /// </summary>
    private void SpawnMonster()
    {
        int playerLevel = GameManager.Instance.PlayerManager.Data.level;
        _currentMonster = MonsterData.CreateByPlayerLevel(playerLevel, CurrentWave);
        
        Debug.Log($"[BattleManager] Spawned: {_currentMonster.monsterName} (HP: {_currentMonster.maxHealth}, ATK: {_currentMonster.attack})");
        
        OnMonsterSpawned?.Invoke(_currentMonster);
    }

    /// <summary>
    /// 击杀怪物
    /// </summary>
    private void KillMonster()
    {
        if (_currentMonster == null) return;
        
        // 计算奖励（金币加成）
        long baseReward = _currentMonster.rewardGold;
        long finalReward = (long)(baseReward * GameManager.Instance.PlayerManager.GetWaveBonus());
        
        // 发放奖励
        GameManager.Instance.PlayerManager.AddGold(finalReward);
        
        // 更新怪物击杀统计
        GameManager.Instance.PlayerManager.Data.monstersKilled++;
        
        // 触发每日任务进度 - 击杀怪物
        if (DailyQuestManager.Instance != null)
        {
            DailyQuestManager.Instance.UpdateQuestProgress(QuestType.KillMonster, 1);
        }
        
        // 检查波次任务
        if (DailyQuestManager.Instance != null)
        {
            int currentWave = CurrentWave;
            DailyQuestManager.Instance.UpdateQuestProgress(QuestType.ReachWave, currentWave);
        }
        
        Debug.Log($"[BattleManager] Monster killed! Reward: {finalReward} gold");
        
        OnMonsterKilled?.Invoke(_currentMonster, finalReward);
        
        // 波次+1
        CurrentWave++;
        
        // 更新最高波次
        if (CurrentWave > GameManager.Instance.PlayerManager.Data.highestWave)
        {
            GameManager.Instance.PlayerManager.Data.highestWave = CurrentWave;
        }
        
        OnWaveChanged?.Invoke(CurrentWave);
        
        // 延迟生成新怪物
        _ = DelayedSpawnMonster();
    }

    /// <summary>
    /// 延迟生成新怪物（给玩家反馈时间）
    /// </summary>
    private System.Collections.IEnumerator DelayedSpawnMonster()
    {
        yield return new WaitForSeconds(0.5f);
        SpawnMonster();
    }

    /// <summary>
    /// 重置战斗（用于重新开始）
    /// </summary>
    public void ResetBattle()
    {
        CurrentWave = 1;
        _currentMonster = null;
        _playerAttackTimer = 0f;
        _monsterAttackTimer = 0f;
    }
}
