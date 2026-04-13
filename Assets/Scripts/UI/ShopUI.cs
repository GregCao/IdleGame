using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using IdleGame;
using IdleGame.Audio;

/// <summary>
/// 商店面板UI控制器
/// IAP商品列表：新手礼包、月卡、金币包
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("商品预制体")]
    public GameObject shopItemPrefab;
    public Transform shopGrid;

    [Header("分类标签")]
    public Button allCategoryButton;
    public Button starterCategoryButton;
    public Button subscriptionCategoryButton;
    public Button goldCategoryButton;

    [Header("当前分类")]
    public TextMeshProUGUI currentCategoryText;

    [Header("关闭按钮")]
    public Button closeButton;

    [Header("提示文字")]
    public TextMeshProUGUI hintText;

    [Header("商品图标")]
    public Sprite goldIconSprite;
    public Sprite gemIconSprite;
    public Sprite starterPackIconSprite;
    public Sprite monthlyCardIconSprite;

    private List<ShopItemUI> _shopItems = new List<ShopItemUI>();
    private List<ShopProduct> _allProducts = new List<ShopProduct>();
    private string _currentCategory = "all";

    // 商品配置
    private readonly ShopProduct[] _productTemplates = new ShopProduct[]
    {
        // 新手礼包
        new ShopProduct
        {
            productId = "starter_pack",
            name = "新手礼包",
            description = "包含51000金币和1件稀有装备！",

            price = 0.99f,
            priceText = "$0.99",
            category = "starter",
            iconType = ShopIconType.StarterPack,
            rewards = new ShopReward[]
            {
                new ShopReward { type = RewardType.Gold, amount = 50000 },
                new ShopReward { type = RewardType.Gold, amount = 1000 }, // 钻石替换为金币（10倍）
                new ShopReward { type = RewardType.Equipment, amount = 1 }
            },
            isPurchased = false
        },
        // 月卡
        new ShopProduct
        {
            productId = "monthly_card",
            name = "月卡",
            description = "每日领取1000金币，连续30天，总计30000金币！",
            price = 4.99f,
            priceText = "$4.99",
            category = "subscription",
            iconType = ShopIconType.MonthlyCard,
            rewards = new ShopReward[]
            {
                new ShopReward { type = RewardType.Gold, amount = 30000, daily = 1000, duration = 30 }
            },
            isPurchased = false
        },
        // 金币包 - 小
        new ShopProduct
        {
            productId = "gold_pack_small",
            name = "金币包(小)",
            description = "获得100000金币",
            price = 0.99f,
            priceText = "$0.99",
            category = "gold",
            iconType = ShopIconType.Gold,
            rewards = new ShopReward[]
            {
                new ShopReward { type = RewardType.Gold, amount = 100000 }
            },
            isPurchased = false
        },
        // 金币包 - 中
        new ShopProduct
        {
            productId = "gold_pack_medium",
            name = "金币包(中)",
            description = "获得500000金币，超值优惠！",
            price = 4.99f,
            priceText = "$4.99",
            category = "gold",
            iconType = ShopIconType.Gold,
            rewards = new ShopReward[]
            {
                new ShopReward { type = RewardType.Gold, amount = 500000 }
            },
            isPurchased = false
        },
        // 金币包 - 大
        new ShopProduct
        {
            productId = "gold_pack_large",
            name = "金币包(大)",
            description = "获得2000000金币，限时特惠！",
            price = 9.99f,
            priceText = "$9.99",
            category = "gold",
            iconType = ShopIconType.Gold,
            rewards = new ShopReward[]
            {
                new ShopReward { type = RewardType.Gold, amount = 2000000 }
            },
            isPurchased = false
        }
    };

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        DOTweenAnimations.ScaleIn(rectTransform, 0.3f);
        RefreshAll();
        LoadPurchaseState();
    }

    private void CacheComponents()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // 分类按钮
        if (allCategoryButton != null)
            allCategoryButton.onClick.AddListener(() => OnCategoryClicked("all"));
        if (starterCategoryButton != null)
            starterCategoryButton.onClick.AddListener(() => OnCategoryClicked("starter"));
        if (subscriptionCategoryButton != null)
            subscriptionCategoryButton.onClick.AddListener(() => OnCategoryClicked("subscription"));
        if (goldCategoryButton != null)
            goldCategoryButton.onClick.AddListener(() => OnCategoryClicked("gold"));
    }

    private void Initialize()
    {
        // 初始化商品数据
        _allProducts.Clear();
        foreach (var template in _productTemplates)
        {
            _allProducts.Add(new ShopProduct
            {
                productId = template.productId,
                name = template.name,
                description = template.description,
                price = template.price,
                priceText = template.priceText,
                category = template.category,
                iconType = template.iconType,
                rewards = template.rewards,
                isPurchased = false
            });
        }

        // 生成商品UI
        GenerateShopItems();
        RefreshAll();
    }

    /// <summary>
    /// 生成商店物品
    /// </summary>
    private void GenerateShopItems()
    {
        if (shopGrid == null || shopItemPrefab == null) return;

        // 清除旧物品
        foreach (var item in _shopItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        _shopItems.Clear();

        // 生成新物品
        foreach (var product in _allProducts)
        {
            GameObject obj = Instantiate(shopItemPrefab, shopGrid);
            ShopItemUI itemUI = obj.GetComponent<ShopItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(product, this);
                _shopItems.Add(itemUI);
            }
        }

        Debug.Log($"[ShopUI] Generated {_allProducts.Count} shop items");
    }

    /// <summary>
    /// 刷新所有UI
    /// </summary>
    public void RefreshAll()
    {
        RefreshCategoryButtons();
        RefreshShopItems();
        RefreshCategoryText();
    }

    /// <summary>
    /// 刷新分类按钮
    /// </summary>
    private void RefreshCategoryButtons()
    {
        // 重置所有按钮颜色
        ResetButtonColors();

        // 高亮当前分类
        Color selectedColor = new Color(1f, 0.84f, 0f); // 金色
        Color normalColor = new Color(0.6f, 0.6f, 0.6f); // 灰色

        switch (_currentCategory)
        {
            case "all":
                if (allCategoryButton != null)
                    allCategoryButton.GetComponent<Image>().color = selectedColor;
                break;
            case "starter":
                if (starterCategoryButton != null)
                    starterCategoryButton.GetComponent<Image>().color = selectedColor;
                break;
            case "subscription":
                if (subscriptionCategoryButton != null)
                    subscriptionCategoryButton.GetComponent<Image>().color = selectedColor;
                break;
            case "gold":
                if (goldCategoryButton != null)
                    goldCategoryButton.GetComponent<Image>().color = selectedColor;
                break;
        }
    }

    private void ResetButtonColors()
    {
        Color normalColor = new Color(0.4f, 0.4f, 0.4f);

        if (allCategoryButton != null)
            allCategoryButton.GetComponent<Image>().color = normalColor;
        if (starterCategoryButton != null)
            starterCategoryButton.GetComponent<Image>().color = normalColor;
        if (subscriptionCategoryButton != null)
            subscriptionCategoryButton.GetComponent<Image>().color = normalColor;
        if (goldCategoryButton != null)
            goldCategoryButton.GetComponent<Image>().color = normalColor;
    }

    /// <summary>
    /// 刷新商店物品
    /// </summary>
    private void RefreshShopItems()
    {
        foreach (var item in _shopItems)
        {
            if (item == null) continue;

            // 根据分类显示/隐藏
            bool show = _currentCategory == "all" || item.Product.category == _currentCategory;
            item.gameObject.SetActive(show);

            if (show)
            {
                item.RefreshUI();
            }
        }
    }

    /// <summary>
    /// 刷新分类文字
    /// </summary>
    private void RefreshCategoryText()
    {
        if (currentCategoryText == null) return;

        switch (_currentCategory)
        {
            case "all":
                currentCategoryText.text = "全部商品";
                break;
            case "starter":
                currentCategoryText.text = "新手礼包";
                break;
            case "subscription":
                currentCategoryText.text = "订阅";
                break;
            case "gold":
                currentCategoryText.text = "金币";
                break;
            default:
                currentCategoryText.text = "商品";
                break;
        }
    }

    // ==================== 点击事件 ====================

    /// <summary>
    /// 分类按钮点击
    /// </summary>
    private void OnCategoryClicked(string category)
    {
        DOTweenAnimations.ButtonClickScale(allCategoryButton);
        SoundManager.Instance?.PlaySFX(SoundType.SFX_Click);

        _currentCategory = category;
        RefreshAll();

        Debug.Log($"[ShopUI] Category changed to: {category}");
    }

    /// <summary>
    /// 购买商品
    /// </summary>
    public void OnPurchaseClicked(string productId)
    {
        // 查找对应商品的购买按钮并播放动画
        ShopItemUI targetItem = _shopItems.Find(item => item.ProductId == productId);
        if (targetItem != null && targetItem.BuyButton != null)
            DOTweenAnimations.ButtonClickScale(targetItem.BuyButton);

        // 查找商品
        ShopProduct product = FindProduct(productId);
        if (product == null)
        {
            Debug.LogError($"[ShopUI] Product not found: {productId}");
            return;
        }

        // 检查是否已购买（如果是只能购买一次的商品）
        if (product.isPurchased)
        {
            ShowHint("该商品已购买！");
            return;
        }

        Debug.Log($"[ShopUI] Purchase clicked: {product.name} - {product.priceText}");

        // 调用IAPManager进行购买
        PurchaseProduct(product);
    }

    /// <summary>
    /// 执行购买
    /// </summary>
    private void PurchaseProduct(ShopProduct product)
    {
        // TODO: 调用IAPManager进行购买
        // IAPManager.Instance.Purchase(product.productId, OnPurchaseSuccess, OnPurchaseFailed);

        // 模拟购买成功
        // 实际项目中需要接入IAP SDK
        SimulatePurchase(product);
    }

    /// <summary>
    /// 模拟购买（测试用）
    /// </summary>
    private void SimulatePurchase(ShopProduct product)
    {
        Debug.Log($"[ShopUI] Simulating purchase: {product.name}");

        // 显示购买确认（这里简化处理，直接发货）
        // 实际项目中应该显示确认对话框

        // 发放奖励
        foreach (var reward in product.rewards)
        {
            GrantReward(reward);
        }

        // 标记为已购买（如果是永久性商品）
        if (IsPermanentProduct(product))
        {
            product.isPurchased = true;
            SavePurchaseState();
        }

        // 刷新UI
        RefreshAll();

        // 显示成功提示
        ShowPurchaseSuccess(product);
    }

    /// <summary>
    /// 购买成功回调
    /// </summary>
    private void OnPurchaseSuccess(string productId)
    {
        ShopProduct product = FindProduct(productId);
        if (product == null) return;

        Debug.Log($"[ShopUI] Purchase success: {productId}");

        // 发放奖励
        foreach (var reward in product.rewards)
        {
            GrantReward(reward);
        }

        // 标记为已购买
        if (IsPermanentProduct(product))
        {
            product.isPurchased = true;
            SavePurchaseState();
        }

        RefreshAll();
        ShowPurchaseSuccess(product);
    }

    /// <summary>
    /// 购买失败回调
    /// </summary>
    private void OnPurchaseFailed(string productId, string error)
    {
        Debug.LogError($"[ShopUI] Purchase failed: {productId} - {error}");
        ShowHint($"购买失败: {error}");
    }

    /// <summary>
    /// 发放奖励
    /// </summary>
    private void GrantReward(ShopReward reward)
    {
        switch (reward.type)
        {
            case RewardType.Gold:
                GameManager.Instance?.PlayerManager?.AddGold(reward.amount);
                Debug.Log($"[ShopUI] Granted {reward.amount} gold");
                break;

            case RewardType.Gem:
                // 钻石系统暂未实现，改为发放金币作为补偿
                GameManager.Instance?.PlayerManager?.AddGold(reward.amount * 10);
                Debug.Log($"[ShopUI] Granted {reward.amount * 10} gold (Gem not implemented)");
                break;

            case RewardType.Equipment:
                // EquipmentManager.Instance?.GenerateEquipment(...);
                Debug.Log($"[ShopUI] Granted equipment");
                break;

            case RewardType.Item:
                // 发放道具
                Debug.Log($"[ShopUI] Granted item x{reward.amount}");
                break;
        }
    }

    /// <summary>
    /// 是否是永久性商品（购买一次后不再显示）
    /// </summary>
    private bool IsPermanentProduct(ShopProduct product)
    {
        return product.category == "starter" || product.category == "subscription";
    }

    /// <summary>
    /// 显示购买成功
    /// </summary>
    private void ShowPurchaseSuccess(ShopProduct product)
    {
        string rewardText = GetRewardText(product);
        ShowHint($"购买成功！\n{rewardText}");
        SoundManager.Instance?.PlaySFX(SoundType.SFX_Chest);

        // TODO: 显示奖励弹窗
    }

    /// <summary>
    /// 获取奖励描述
    /// </summary>
    private string GetRewardText(ShopProduct product)
    {
        List<string> rewards = new List<string>();
        foreach (var reward in product.rewards)
        {
            switch (reward.type)
            {
                case RewardType.Gold:
                    rewards.Add($"+{FormatNumber(reward.amount)} 金币");
                    break;
                case RewardType.Gem:
                    rewards.Add($"+{reward.amount} 钻石");
                    break;
                case RewardType.Equipment:
                    rewards.Add($"+{reward.amount} 装备");
                    break;
                case RewardType.Item:
                    rewards.Add($"+{reward.amount} 道具");
                    break;
            }
        }
        return string.Join("\n", rewards);
    }

    private void ShowHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
            hintText.gameObject.SetActive(true);

            // 延迟隐藏
            CancelInvoke(nameof(HideHint));
            Invoke(nameof(HideHint), 3f);
        }

        Debug.Log($"[ShopUI] Hint: {message}");
    }

    private void HideHint()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
    }

    private void Hide()
    {
        DOTweenAnimations.ScaleOut(rectTransform, 0.2f);

        if (UIPanelController.Instance != null)
            UIPanelController.Instance.ClosePanel("Shop");
        else
            gameObject.SetActive(false);
    }

    // ==================== 数据管理 ====================

    private ShopProduct FindProduct(string productId)
    {
        return _allProducts.Find(p => p.productId == productId);
    }

    private void LoadPurchaseState()
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return;

        // TODO: 从PlayerData加载已购买的商品ID
        // var purchasedIds = playerData.purchasedProductIds;
        // foreach (var id in purchasedIds)
        // {
        //     var product = FindProduct(id);
        //     if (product != null)
        //         product.isPurchased = true;
        // }

        RefreshAll();
    }

    private void SavePurchaseState()
    {
        var playerData = GameManager.Instance?.PlayerManager?.Data;
        if (playerData == null) return;

        // TODO: 保存已购买的商品ID到PlayerData
        // var purchasedIds = _allProducts.Where(p => p.isPurchased).Select(p => p.productId).ToList();
        // playerData.purchasedProductIds = purchasedIds;

        Debug.Log("[ShopUI] Purchase state saved");
    }

    // ==================== 辅助方法 ====================

    private string FormatNumber(long num)
    {
        if (num >= 1000000000)
            return $"{num / 1000000000.0:F1}B";
        if (num >= 1000000)
            return $"{num / 1000000.0:F1}M";
        if (num >= 1000)
            return $"{num / 1000.0:F1}K";
        return num.ToString();
    }
}

/// <summary>
/// 商店物品UI组件
/// 挂载在 ShopItemPrefab 上
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("商品图标")]
    public Image iconImage;
    public GameObject iconBackground;

    [Header("商品信息")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI priceText;

    [Header("标签")]
    public GameObject hotTag;
    public GameObject newTag;
    public GameObject saleTag;
    public TextMeshProUGUI salePercentText;

    [Header("购买按钮")]
    public Button purchaseButton;
    public TextMeshProUGUI buttonText;

    [Header("状态")]
    public GameObject purchasedOverlay;
    public TextMeshProUGUI purchasedText;

    private ShopProduct _product;
    private ShopUI _parentUI;

    public ShopProduct Product => _product;

    public void Initialize(ShopProduct product, ShopUI parentUI)
    {
        _product = product;
        _parentUI = parentUI;

        // 绑定按钮事件
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_product == null) return;

        // 商品名称
        if (nameText != null)
            nameText.text = _product.name;

        // 描述
        if (descriptionText != null)
            descriptionText.text = _product.description;

        // 价格
        if (priceText != null)
            priceText.text = _product.priceText;

        // 图标
        if (iconImage != null)
        {
            iconImage.sprite = GetIconSprite(_product.iconType);
            iconImage.color = Color.white;
        }

        // 标签
        RefreshTags();

        // 按钮状态
        RefreshButtonState();

        // 已购买遮罩
        if (purchasedOverlay != null)
        {
            purchasedOverlay.SetActive(_product.isPurchased);
        }

        if (purchasedText != null)
        {
            purchasedText.gameObject.SetActive(_product.isPurchased);
        }
    }

    private void RefreshTags()
    {
        // 热卖标签
        if (hotTag != null)
        {
            hotTag.SetActive(_product.productId == "starter_pack" || _product.productId == "gold_pack_large");
        }

        // 新品标签
        if (newTag != null)
        {
            newTag.SetActive(_product.productId == "monthly_card");
        }

        // 折扣标签
        if (saleTag != null)
        {
            bool isOnSale = _product.originalPrice > _product.price;
            saleTag.SetActive(isOnSale);

            if (isOnSale && salePercentText != null)
            {
                int percent = Mathf.RoundToInt((1 - _product.price / _product.originalPrice) * 100);
                salePercentText.text = $"-{percent}%";
            }
        }
    }

    private void RefreshButtonState()
    {
        if (purchaseButton == null) return;

        if (_product.isPurchased)
        {
            purchaseButton.interactable = false;
            if (buttonText != null)
            {
                buttonText.text = "已购买";
                buttonText.color = Color.gray;
            }
        }
        else
        {
            purchaseButton.interactable = true;
            if (buttonText != null)
            {
                buttonText.text = "购买";
                buttonText.color = Color.white;
            }
        }

        // 图标背景颜色
        if (iconBackground != null)
        {
            Color bgColor = GetCategoryColor(_product.category);
            iconBackground.GetComponent<Image>().color = bgColor;
        }
    }

    private Sprite GetIconSprite(ShopIconType iconType)
    {
        // TODO: 返回对应的图标资源
        switch (iconType)
        {
            case ShopIconType.Gold:
                return _parentUI != null ? _parentUI.goldIconSprite : null;
            case ShopIconType.Gem:
                return _parentUI != null ? _parentUI.gemIconSprite : null;
            case ShopIconType.StarterPack:
                return _parentUI != null ? _parentUI.starterPackIconSprite : null;
            case ShopIconType.MonthlyCard:
                return _parentUI != null ? _parentUI.monthlyCardIconSprite : null;
            default:
                return null;
        }
    }

    private Color GetCategoryColor(string category)
    {
        switch (category)
        {
            case "starter":
                return new Color(1f, 0.5f, 0f); // 橙色
            case "subscription":
                return new Color(0.6f, 0f, 1f); // 紫色
            case "gold":
                return new Color(1f, 0.84f, 0f); // 金色
            default:
                return Color.gray;
        }
    }

    private void OnPurchaseClicked()
    {
        if (_parentUI != null && _product != null)
        {
            _parentUI.OnPurchaseClicked(_product.productId);
        }
    }
}

/// <summary>
/// 商品数据
/// </summary>
public class ShopProduct
{
    public string productId;
    public string name;
    public string description;
    public float price;
    public float originalPrice;
    public string priceText;
    public string category;
    public ShopIconType iconType;
    public ShopReward[] rewards;
    public bool isPurchased;
}

/// <summary>
/// 商店奖励
/// </summary>
public class ShopReward
{
    public RewardType type;
    public int amount;
    public int daily; // 每日领取数量（用于月卡等）
    public int duration; // 持续天数
}

/// <summary>
/// 商店图标类型
/// </summary>
public enum ShopIconType
{
    Gold,
    Gem,
    StarterPack,
    MonthlyCard,
    Item
}
