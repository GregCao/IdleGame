using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace IdleGame.UI
{
    /// <summary>
    /// 伤害数字对象池管理器。
    /// 从池中取 DamagePopup 显示伤害数字，动画结束后回收。
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        // ============================================================
        // Singleton
        // ============================================================
        public static DamagePopupManager Instance { get; private set; }

        // ============================================================
        // Inspector 配置
        // ============================================================
        [Header("对象池配置")]
        [Tooltip("预制体数量上限")]
        [SerializeField] private int _poolSize = 10;

        [Header("伤害数字样式")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _critColor = new Color(1f, 0.2f, 0.2f); // 红色
        [SerializeField] private float _normalFontSize = 36f;
        [SerializeField] private float _critFontSize = 52f;

        [Header("动画参数")]
        [SerializeField] private float _floatHeight = 120f;
        [SerializeField] private float _animDuration = 0.8f;
        [SerializeField] private float _randomXRange = 80f;
        [SerializeField] private float _critScaleMultiplier = 1.3f;

        // ============================================================
        // Private fields
        // ============================================================
        /// <summary>可复用的对象池队列</summary>
        private Queue<DamagePopup> _availablePool;

        /// <summary>所有已创建的对象（含正在使用的）</summary>
        private List<DamagePopup> _allPopups;

        private Camera _uiCamera;
        private RectTransform _rectTransform;

        // ============================================================
        // Unity Lifecycle
        // ============================================================
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _rectTransform = GetComponent<RectTransform>();
            _availablePool = new Queue<DamagePopup>(_poolSize);
            _allPopups = new List<DamagePopup>(_poolSize);
        }

        private void Start()
        {
            Initialize();
        }

        // ============================================================
        // Public API
        // ============================================================
        /// <summary>
        /// 初始化对象池。创建 _poolSize 个 DamagePopup 预制体。
        /// </summary>
        public void Initialize()
        {
            Clear();

            for (int i = 0; i < _poolSize; i++)
            {
                CreatePopup();
            }

            // 隐藏所有
            foreach (var p in _allPopups)
            {
                p.Root.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 显示一次伤害数字。
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="isCrit">是否暴击</param>
        /// <param name="worldPos">怪物头顶的世界坐标</param>
        public void ShowDamage(int damage, bool isCrit, Vector3 worldPos)
        {
            DamagePopup popup = GetFromPool();
            if (popup == null) return;

            // 格式化为 K/M 后缀
            string text = FormatDamage(damage);
            popup.TextComponent.text = isCrit ? text + "!" : text;

            // 颜色和字号
            Color color = isCrit ? _critColor : _normalColor;
            float fontSize = isCrit ? _critFontSize : _normalFontSize;

            popup.TextComponent.color = color;
            popup.TextComponent.fontSize = fontSize;

            // 设置初始位置（世界坐标 → UI 坐标）
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                GetUICamera(), worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, screenPos, null, out Vector2 localPos);

            // 随机 X 偏移，Y 从怪物头顶稍下方开始
            float randomX = UnityEngine.Random.Range(-_randomXRange, _randomXRange);
            localPos.x += randomX;
            localPos.y += 20f; // 紧贴头顶

            popup.Root.anchoredPosition = localPos;
            popup.Root.localScale = Vector3.one * (isCrit ? _critScaleMultiplier : 1f);
            popup.Root.gameObject.SetActive(true);

            // 播放飘动 + 渐隐动画
            PlayFloatAnimation(popup, isCrit);
        }

        /// <summary>
        /// 清理所有显示，将所有对象归还池中。
        /// </summary>
        public void Clear()
        {
            foreach (var p in _allPopups)
            {
                if (p != null && p.Root != null)
                {
                    p.Root.DOKill();
                    Destroy(p.Root.gameObject);
                }
            }
            _allPopups.Clear();
            _availablePool.Clear();
        }

        // ============================================================
        // Private Methods
        // ============================================================

        /// <summary>
        /// 创建并注册一个 DamagePopup 到池中。
        /// </summary>
        private DamagePopup CreatePopup()
        {
            // 创建根 RectTransform（挂载在 DamagePopupManager 下）
            GameObject go = new GameObject("DamagePopup_" + _allPopups.Count,
                typeof(RectTransform));
            go.transform.SetParent(_rectTransform, false);

            // 添加 Image（背景描边/发光底板，可选）
            var bg = go.AddComponent<Image>();
            bg.raycastTarget = false;

            // 添加 TextMeshProUGUI
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // 轮廓（让数字更醒目）
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.12f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.6f);

            // 默认尺寸
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 80f);
            rt.anchoredPosition = Vector2.zero;

            var popup = new DamagePopup
            {
                Root = rt,
                TextComponent = tmp
            };

            _allPopups.Add(popup);
            _availablePool.Enqueue(popup);

            go.SetActive(false);
            return popup;
        }

        /// <summary>
        /// 从池中取出一个 Popup，优先复用已存在的，若池空则动态创建。
        /// </summary>
        private DamagePopup GetFromPool()
        {
            if (_availablePool.Count > 0)
            {
                return _availablePool.Dequeue();
            }

            // 池为空，动态创建一个（超出预设上限）
            return CreatePopup();
        }

        /// <summary>
        /// 动画结束后将 Popup 归还池中。
        /// </summary>
        private void ReturnToPool(DamagePopup popup)
        {
            popup.Root.gameObject.SetActive(false);
            _availablePool.Enqueue(popup);
        }

        /// <summary>
        /// DOTween 飘动 + 渐隐动画。
        /// </summary>
        private void PlayFloatAnimation(DamagePopup popup, bool isCrit)
        {
            popup.Root.DOKill();

            // Y 向上飘动
            popup.Root
                .DOAnchorPosY(popup.Root.anchoredPosition.y + _floatHeight, _animDuration)
                .SetEase(isCrit ? Ease.OutBack : Ease.OutQuad)
                .SetUpdate(UpdateType.Normal, false);

            // 渐隐
            popup.TextComponent
                .DOFade(0f, _animDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(UpdateType.Normal, false)
                .OnComplete(() => ReturnToPool(popup));

            // 暴击时额外轻微放大动画
            if (isCrit)
            {
                popup.Root
                    .DOScale(Vector3.one * _critScaleMultiplier * 1.1f, 0.1f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        popup.Root.DOScale(Vector3.one * _critScaleMultiplier, 0.15f)
                            .SetEase(Ease.InOutQuad);
                    });
            }
        }

        /// <summary>
        /// 显示升级特效文字。
        /// </summary>
        public void ShowLevelUp(int newLevel, Vector3 worldPos)
        {
            DamagePopup popup = GetFromPool();
            if (popup == null) return;

            popup.TextComponent.text = $"Lv.{newLevel} ↑";
            popup.TextComponent.color = new Color(1f, 0.85f, 0f, 1f); // 金色
            popup.TextComponent.fontSize = 48f;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(GetUICamera(), worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPos, null, out Vector2 localPos);
            localPos.y += 60f; // 稍微靠上

            popup.Root.anchoredPosition = localPos;
            popup.Root.localScale = Vector3.one * 1.5f;
            popup.Root.gameObject.SetActive(true);

            PlayLevelUpAnimation(popup);
        }

        /// <summary>
        /// 显示金币获得特效。
        /// </summary>
        public void ShowGold(long goldAmount, Vector3 worldPos)
        {
            DamagePopup popup = GetFromPool();
            if (popup == null) return;

            popup.TextComponent.text = $"+{FormatDamage((int)goldAmount)} 金";
            popup.TextComponent.color = new Color(1f, 0.84f, 0f, 1f); // 金色
            popup.TextComponent.fontSize = 40f;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(GetUICamera(), worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPos, null, out Vector2 localPos);
            localPos.y += 80f;

            popup.Root.anchoredPosition = localPos;
            popup.Root.localScale = Vector3.one;
            popup.Root.gameObject.SetActive(true);

            PlayFloatAnimation(popup, false);
        }

        /// <summary>
        /// 升级动画：放大 → 缩小 + 上浮
        /// </summary>
        private void PlayLevelUpAnimation(DamagePopup popup)
        {
            popup.Root.DOKill();

            popup.Root
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack);

            popup.Root
                .DOAnchorPosY(popup.Root.anchoredPosition.y + 60f, 1.0f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Normal, false);

            popup.TextComponent
                .DOFade(0f, 1.0f)
                .SetEase(Ease.InQuad)
                .SetUpdate(UpdateType.Normal, false)
                .OnComplete(() => ReturnToPool(popup));
        }

        /// <summary>
        /// 伤害数字格式化：超过 1000 显示 K，超过 1,000,000 显示 M。
        /// </summary>
        private string FormatDamage(int damage)
        {
            if (damage >= 1_000_000)
                return $"{damage / 1_000_000.0:0.#}M";
            if (damage >= 10_000)
                return $"{damage / 1_000.0:0.#}K";
            if (damage >= 1_000)
                return $"{damage / 1_000.0:0.#}K";
            return damage.ToString("N0");
        }

        private Camera GetUICamera()
        {
            if (_uiCamera != null) return _uiCamera;
            // 尝试从 Canvas 获取 UICamera
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = canvas.worldCamera;
            }
            if (_uiCamera == null)
            {
                _uiCamera = Camera.main;
            }
            return _uiCamera ?? Camera.main;
        }

        // ============================================================
        // Inner type — 避免每次 ShowDamage 申请 GC
        // ============================================================
        private struct DamagePopup
        {
            public RectTransform Root;
            public TextMeshProUGUI TextComponent;
        }
    }
}
