using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 游戏总管理器 - 核心游戏循环
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("游戏配置")]
    [SerializeField] private float _tickInterval = 1f;  // 战斗tick间隔（秒）
    
    private float _tickTimer = 0f;
    private bool _isPaused = false;
    
    // 事件
    public UnityEvent OnGameStart;
    public UnityEvent OnGamePause;
    public UnityEvent OnGameResume;
    public UnityEvent OnTick;
    
    // 子系统引用
    public PlayerManager PlayerManager { get; private set; }
    public BattleManager BattleManager { get; private set; }
    public EconomyManager EconomyManager { get; private set; }
    public AdManager AdManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public SaveManager SaveManager { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        InitializeSubsystems();
    }

    private void Start()
    {
        StartGame();
    }

    private void InitializeSubsystems()
    {
        // 初始化各管理系统
        PlayerManager = GetComponent<PlayerManager>() ?? gameObject.AddComponent<PlayerManager>();
        BattleManager = GetComponent<BattleManager>() ?? gameObject.AddComponent<BattleManager>();
        EconomyManager = GetComponent<EconomyManager>() ?? gameObject.AddComponent<EconomyManager>();
        AdManager = GetComponent<AdManager>() ?? gameObject.AddComponent<AdManager>();
        UIManager = GetComponent<UIManager>() ?? gameObject.AddComponent<UIManager>();
        SaveManager = GetComponent<SaveManager>() ?? gameObject.AddComponent<SaveManager>();
        
        Debug.Log("[GameManager] All subsystems initialized.");
    }

    private void StartGame()
    {
        // 加载存档
        SaveManager.LoadPlayerData();
        
        // 计算离线收益
        EconomyManager.CalculateOfflineEarnings();
        
        // 通知UI更新
        UIManager.RefreshAllUI();
        
        // 开启战斗
        BattleManager.StartBattle();
        
        _isPaused = false;
        OnGameStart?.Invoke();
        
        Debug.Log("[GameManager] Game started.");
    }

    private void Update()
    {
        if (_isPaused) return;
        
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer = 0f;
            OnTick?.Invoke();
            BattleManager.OnTick();
        }
    }

    public void PauseGame()
    {
        _isPaused = true;
        OnGamePause?.Invoke();
    }

    public void ResumeGame()
    {
        _isPaused = false;
        OnGameResume?.Invoke();
    }
}
