using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using IdleGame.Platform;

/// <summary>
/// 支付/订阅管理器
/// 
/// 支持的商城：
/// - 微信小程序支付
/// - Google Play内购（海外版）
/// - Apple App Store内购（海外版）
/// 
/// 当前为微信小程序版本
/// </summary>
public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    [Header("商品配置")]
    [SerializeField] private List<IAPProduct> _products = new List<IAPProduct>();

    // 事件
    public UnityEvent<IAPProduct, bool> OnPurchaseCompleted;  // 购买结果
    public UnityEvent<IAPProduct> OnPurchaseFailed;
    public UnityEvent OnStoreInitialized;

    // 已购买状态
    private HashSet<string> _purchasedProductIds = new HashSet<string>();
    private bool _isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 模拟购买（Mock模式延迟回调）
    /// </summary>
    private System.Collections.IEnumerator SimulatePurchase(IAPProduct product, Action<bool> callback)
    {
        yield return new WaitForSeconds(0.5f);
        OnPurchaseSuccess(product);
        callback?.Invoke(true);
    }

    /// <summary>
    /// 初始化支付系统
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[IAPManager] Already initialized.");
            return;
        }

        Debug.Log("[IAPManager] Initializing IAP system...");

        // 初始化默认商品
        InitializeDefaultProducts();

        // TODO: 初始化微信支付
        // WeChatSDK.Instance.InitializeIAP();

        _isInitialized = true;
        OnStoreInitialized?.Invoke();
        Debug.Log("[IAPManager] IAP system initialized.");
    }

    /// <summary>
    /// 初始化默认商品
    /// </summary>
    private void InitializeDefaultProducts()
    {
        if (_products.Count == 0)
        {
            _products.Add(new IAPProduct
            {
                id = "remove_ads",
                name = "去除广告",
                description = "永久去除所有广告",
                price = 1.99m,
                priceString = "$1.99",
                type = IAPProductType.NonConsumable
            });

            _products.Add(new IAPProduct
            {
                id = "monthly_card",
                name = "月卡",
                description = "每日领取1000金币",
                price = 4.99m,
                priceString = "$4.99",
                type = IAPProductType.Subscription,
                subscriptionDays = 30
            });

            _products.Add(new IAPProduct
            {
                id = "first_pack",
                name = "新手礼包",
                description = "获得5000金币和10倍伤害加成（永久）",
                price = 0.99m,
                priceString = "$0.99",
                type = IAPProductType.NonConsumable
            });

            _products.Add(new IAPProduct
            {
                id = "gold_pack_small",
                name = "金币小礼包",
                description = "获得10000金币",
                price = 0.99m,
                priceString = "$0.99",
                type = IAPProductType.Consumable
            });

            _products.Add(new IAPProduct
            {
                id = "gold_pack_medium",
                name = "金币中礼包",
                description = "获得50000金币",
                price = 4.99m,
                priceString = "$4.99",
                type = IAPProductType.Consumable
            });

            _products.Add(new IAPProduct
            {
                id = "gold_pack_large",
                name = "金币大礼包",
                description = "获得200000金币",
                price = 9.99m,
                priceString = "$9.99",
                type = IAPProductType.Consumable
            });
        }
    }

    /// <summary>
    /// 购买商品
    /// </summary>
    public void Purchase(string productId, Action<bool> callback = null)
    {
        IAPProduct product = GetProduct(productId);
        if (product == null)
        {
            Debug.LogError($"[IAPManager] Product not found: {productId}");
            callback?.Invoke(false);
            return;
        }

        Debug.Log($"[IAPManager] Purchasing product: {product.name}");

if (SdkConfig.Instance.IsWeChatMode && WeChatSDK.Instance != null)
        {
            // 微信支付（生产模式）
            WeChatSDK.Instance.Pay(productId, (success) =>
            {
                if (success)
                    OnPurchaseSuccess(product);
                else
                    OnPurchaseFailed(product);
                callback?.Invoke(success);
            });
        }
        else
        {
            // Mock模式（编辑器/开发测试）
            Debug.Log("[IAPManager] Simulating purchase (Mock mode)...");
            StartCoroutine(SimulatePurchase(product, callback));
        }
    }

    /// <summary>
    /// 购买成功
    /// </summary>
    private void OnPurchaseSuccess(IAPProduct product)
    {
        Debug.Log($"[IAPManager] Purchase success: {product.name}");

        // 处理不同类型商品
        switch (product.type)
        {
            case IAPProductType.Consumable:
                GrantConsumableReward(product);
                break;

            case IAPProductType.NonConsumable:
                GrantNonConsumableReward(product);
                _purchasedProductIds.Add(product.id);
                break;

            case IAPProductType.Subscription:
                GrantSubscriptionReward(product);
                break;
        }

        OnPurchaseCompleted?.Invoke(product, true);

        // 刷新UI
        if (GameManager.Instance?.UIManager != null)
        {
            GameManager.Instance.UIManager.RefreshAllUI();
        }
    }

    /// <summary>
    /// 购买失败
    /// </summary>
    private void OnPurchaseFailed(IAPProduct product)
    {
        Debug.LogWarning($"[IAPManager] Purchase failed: {product.name}");
        OnPurchaseFailed?.Invoke(product);
    }

    /// <summary>
    /// 发放消耗品奖励
    /// </summary>
    private void GrantConsumableReward(IAPProduct product)
    {
        switch (product.id)
        {
            case "gold_pack_small":
                GameManager.Instance.PlayerManager.AddGold(10000);
                GameManager.Instance.UIManager.ShowReward($"+10,000 金币");
                break;

            case "gold_pack_medium":
                GameManager.Instance.PlayerManager.AddGold(50000);
                GameManager.Instance.UIManager.ShowReward($"+50,000 金币");
                break;

            case "gold_pack_large":
                GameManager.Instance.PlayerManager.AddGold(200000);
                GameManager.Instance.UIManager.ShowReward($"+200,000 金币");
                break;
        }
    }

    /// <summary>
    /// 发放非消耗品奖励（永久）
    /// </summary>
    private void GrantNonConsumableReward(IAPProduct product)
    {
        switch (product.id)
        {
            case "remove_ads":
                // 设置去除广告标志
                PlayerPrefs.SetInt("AdsRemoved", 1);
                Debug.Log("[IAPManager] Ads removed permanently.");
                break;

            case "first_pack":
                // 设置首充标志 + 永久加成
                PlayerPrefs.SetInt("FirstPackPurchased", 1);
                PlayerPrefs.SetFloat("FirstPackDamageBonus", 10f);
                Debug.Log("[IAPManager] First pack purchased with 10x damage bonus.");
                break;
        }
    }

    /// <summary>
    /// 发放订阅奖励
    /// </summary>
    private void GrantSubscriptionReward(IAPProduct product)
    {
        switch (product.id)
        {
            case "monthly_card":
                // 设置月卡到期时间
                DateTime expireTime = DateTime.Now.AddDays(product.subscriptionDays);
                PlayerPrefs.SetString("MonthlyCardExpire", expireTime.ToString());
                Debug.Log($"[IAPManager] Monthly card activated, expires: {expireTime}");
                break;
        }
    }

    /// <summary>
    /// 获取商品信息
    /// </summary>
    public IAPProduct GetProduct(string productId)
    {
        return _products.Find(p => p.id == productId);
    }

    /// <summary>
    /// 获取所有商品
    /// </summary>
    public List<IAPProduct> GetAllProducts()
    {
        return new List<IAPProduct>(_products);
    }

    /// <summary>
    /// 检查是否已购买（非消耗品）
    /// </summary>
    public bool IsPurchased(string productId)
    {
        return _purchasedProductIds.Contains(productId);
    }

    /// <summary>
    /// 检查广告是否已去除
    /// </summary>
    public bool IsAdsRemoved()
    {
        return PlayerPrefs.GetInt("AdsRemoved", 0) == 1;
    }

    /// <summary>
    /// 检查月卡是否有效
    /// </summary>
    public bool IsMonthlyCardActive()
    {
        if (!PlayerPrefs.HasKey("MonthlyCardExpire"))
            return false;

        string expireStr = PlayerPrefs.GetString("MonthlyCardExpire");
        if (DateTime.TryParse(expireStr, out DateTime expireTime))
        {
            return DateTime.Now < expireTime;
        }
        return false;
    }

    /// <summary>
    /// 获取首充加成倍数
    /// </summary>
    public float GetFirstPackBonus()
    {
        if (PlayerPrefs.GetInt("FirstPackPurchased", 0) == 1)
        {
            return PlayerPrefs.GetFloat("FirstPackDamageBonus", 1f);
        }
        return 1f;
    }
}

/// <summary>
/// IAP商品数据
/// </summary>
[System.Serializable]
public class IAPProduct
{
    public string id;           // 商品ID
    public string name;         // 显示名称
    public string description;  // 描述
    public decimal price;       // 价格（数字）
    public string priceString;  // 价格（显示字符串，如"$0.99"）
    public IAPProductType type; // 商品类型
    public int subscriptionDays;  // 订阅天数（如果是订阅类型）
}

/// <summary>
/// IAP商品类型
/// </summary>
public enum IAPProductType
{
    Consumable,        // 消耗品（金币等）
    NonConsumable,     // 非消耗品（永久有效）
    Subscription       // 订阅（有时间限制）
}
