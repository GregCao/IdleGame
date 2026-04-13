using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;

namespace IdleGame.UI
{
    /// <summary>
    /// 红点通知徽章系统。
    /// 提供统一的 ShowBadge / HideBadge / CheckAllBadges 接口，
    /// 自动叠加 DOTween 脉冲动画，支持单例全局访问。
    /// 
    /// -badge 规则：
    ///   - EquipBadge   → 背包有新装备时显示
    ///   - CheckInBadge → 今日未签到时显示
    ///   - QuestBadge   → 有可领取奖励时显示
    /// </summary>
    public class NotificationBadge : Singleton<NotificationBadge>
    {
        // ==================== Singleton ====================
        protected override bool IsPersistent() => false; // UI 管理器不需要跨场景

        // ==================== Badge Types ====================
        public enum BadgeType
        {
            EquipBadge,
            CheckInBadge,
            QuestBadge
        }

        // ==================== Configuration ====================
        [Header("徽章引用(由 UIManager 注入,或由 CacheBadges 自动查找)")]
        [SerializeField] private Image _equipBadge;
        [SerializeField] private Image _checkInBadge;
        [SerializeField] private Image _questBadge;

        [Header("脉冲动画配置")]
        [Tooltip("脉冲放大倍率")]
        [SerializeField] private float _pulseScale = 1.2f;

        [Tooltip("脉冲单个周期时长(秒)")]
        [SerializeField] private float _pulseDuration = 0.5f;

        [Tooltip("脉冲循环次数(-1 = 无限)")]
        [SerializeField] private int _pulseLoops = -1;

        // ==================== Internal State ====================
        private Dictionary<BadgeType, Image> _badgeMap = new Dictionary<BadgeType, Image>();
        private Dictionary<BadgeType, Tween> _pulseTweens = new Dictionary<BadgeType, Tween>();
        private Dictionary<BadgeType, bool> _badgeVisible = new Dictionary<BadgeType, bool>();

        protected override void Awake()
        {
            base.Awake();
            if (_instance == this)
            {
                CacheBadges();
                InitializeBadgeState();
            }
        }

        private void OnDestroy()
        {
            KillAllPulseTweens();
        }

        // ==================== Initialization ====================

        /// <summary>
        /// 注册徽章引用到字典,若未配置则尝试自动查找。
        /// </summary>
        private void CacheBadges()
        {
            _badgeMap.Clear();

            if (_equipBadge != null)
                _badgeMap[BadgeType.EquipBadge] = _equipBadge;
            else
                _badgeMap[BadgeType.EquipBadge] = FindBadgeImage("MainMenuBar/EquipBtn/Badge");

            if (_checkInBadge != null)
                _badgeMap[BadgeType.CheckInBadge] = _checkInBadge;
            else
                _badgeMap[BadgeType.CheckInBadge] = FindBadgeImage("MainMenuBar/CheckInBtn/Badge");

            if (_questBadge != null)
                _badgeMap[BadgeType.QuestBadge] = _questBadge;
            else
                _badgeMap[BadgeType.QuestBadge] = FindBadgeImage("MainMenuBar/QuestBtn/Badge");

            // 初始化可见性字典
            foreach (BadgeType type in Enum.GetValues(typeof(BadgeType)))
            {
                if (!_badgeVisible.ContainsKey(type))
                    _badgeVisible[type] = false;
            }
        }

        /// <summary>
        /// 初始化所有徽章为隐藏状态。
        /// </summary>
        private void InitializeBadgeState()
        {
            foreach (var kvp in _badgeMap)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.gameObject.SetActive(false);
                    // 重置缩放防止残留
                    kvp.Value.transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// 尝试根据路径在自身 RectTransform 下查找 Image。
        /// </summary>
        private Image FindBadgeImage(string path)
        {
            var tf = transform.Find(path);
            return tf != null ? tf.GetComponent<Image>() : null;
        }

        // ==================== Public API ====================

        /// <summary>
        /// 显示指定类型的徽章(播放脉冲动画)。
        /// </summary>
        /// <param name="badgeType">徽章类型</param>
        /// <param name="animate">是否播放脉冲动画(默认 true)</param>
        public void ShowBadge(BadgeType badgeType, bool animate = true)
        {
            if (!_badgeMap.TryGetValue(badgeType, out var image) || image == null)
                return;

            if (_badgeVisible.TryGetValue(badgeType, out var wasVisible) && wasVisible)
            {
                // 已显示时,只确保动画在运行
                if (animate)
                    EnsurePulsePlaying(badgeType, image);
                return;
            }

            // 激活并播放显示动画
            image.gameObject.SetActive(true);
            image.transform.localScale = Vector3.zero;

            _badgeVisible[badgeType] = true;

            // 弹入动画
            image.transform
                .DOScale(1f, 0.2f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    if (animate)
                        EnsurePulsePlaying(badgeType, image);
                });
        }

        /// <summary>
        /// 隐藏指定类型的徽章(停止动画并渐隐)。
        /// </summary>
        /// <param name="badgeType">徽章类型</param>
        public void HideBadge(BadgeType badgeType)
        {
            if (!_badgeMap.TryGetValue(badgeType, out var image) || image == null)
                return;

            if (!_badgeVisible.TryGetValue(badgeType, out var isVisible) || !isVisible)
                return;

            _badgeVisible[badgeType] = false;

            // 停止脉冲动画
            StopPulse(badgeType);

            // 缩出动画
            image.transform
                .DOScale(0f, 0.15f)
                .SetEase(Ease.InBack)
                .OnComplete(() => image.gameObject.SetActive(false));
        }

        /// <summary>
        /// 切换徽章显示状态。
        /// </summary>
        public void ToggleBadge(BadgeType badgeType)
        {
            if (_badgeVisible.TryGetValue(badgeType, out var isVisible) && isVisible)
                HideBadge(badgeType);
            else
                ShowBadge(badgeType);
        }

        /// <summary>
        /// 根据当前游戏状态检查并刷新所有徽章的显示。
        /// 由 UIManager 在适当时机调用。
        /// </summary>
        public void CheckAllBadges()
        {
            // === 装备徽章 ===
            if (_badgeMap.TryGetValue(BadgeType.EquipBadge, out var equipImg) && equipImg != null)
            {
                bool shouldShow = false;
                if (EquipmentManager.Instance != null)
                {
                    var owned = EquipmentManager.Instance.GetOwnedEquipment();
                    shouldShow = owned != null && owned.Count > 0;
                }
                // 仅在状态变化时更新
                bool isVisible = _badgeVisible.TryGetValue(BadgeType.EquipBadge, out var v) && v;
                if (shouldShow && !isVisible)
                    ShowBadge(BadgeType.EquipBadge);
                else if (!shouldShow && isVisible)
                    HideBadge(BadgeType.EquipBadge);
            }

            // === 签到徽章 ===
            if (_badgeMap.TryGetValue(BadgeType.CheckInBadge, out var checkInImg) && checkInImg != null)
            {
                bool shouldShow = DailyCheckInManager.Instance != null
                                  && DailyCheckInManager.Instance.CanCheckInToday;
                bool isVisible = _badgeVisible.TryGetValue(BadgeType.CheckInBadge, out var v) && v;
                if (shouldShow && !isVisible)
                    ShowBadge(BadgeType.CheckInBadge);
                else if (!shouldShow && isVisible)
                    HideBadge(BadgeType.CheckInBadge);
            }

            // === 任务徽章 ===
            if (_badgeMap.TryGetValue(BadgeType.QuestBadge, out var questImg) && questImg != null)
            {
                bool shouldShow = DailyQuestManager.Instance != null
                                  && DailyQuestManager.Instance.GetClaimableCount() > 0;
                bool isVisible = _badgeVisible.TryGetValue(BadgeType.QuestBadge, out var v) && v;
                if (shouldShow && !isVisible)
                    ShowBadge(BadgeType.QuestBadge);
                else if (!shouldShow && isVisible)
                    HideBadge(BadgeType.QuestBadge);
            }
        }

        /// <summary>
        /// 注入徽章引用(在 UIManager.Start 之后调用,确保引用有效)。
        /// </summary>
        public void InjectBadge(BadgeType badgeType, Image badgeImage)
        {
            if (badgeImage == null) return;
            _badgeMap[badgeType] = badgeImage;
        }

        // ==================== Private Animation Helpers ====================

        /// <summary>
        /// 确保脉冲动画正在播放,若未播放则启动。
        /// </summary>
        private void EnsurePulsePlaying(BadgeType badgeType, Image image)
        {
            if (_pulseTweens.TryGetValue(badgeType, out var existing) && existing != null && existing.IsActive())
                return; // 已在播放

            StopPulse(badgeType);

            var rt = image.GetComponent<RectTransform>();
            if (rt == null) return;

            var tween = rt
                .DOScale(_pulseScale, _pulseDuration * 0.5f)
                .SetEase(Ease.InOutQuad)
                .SetLoops(_pulseLoops, LoopType.Yoyo);

            _pulseTweens[badgeType] = tween;
        }

        /// <summary>
        /// 停止指定徽章的脉冲动画。
        /// </summary>
        private void StopPulse(BadgeType badgeType)
        {
            if (_pulseTweens.TryGetValue(badgeType, out var tw) && tw != null)
            {
                tw.Kill();
                _pulseTweens[badgeType] = null;
            }

            // 重置缩放到正常大小
            if (_badgeMap.TryGetValue(badgeType, out var img) && img != null)
            {
                img.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 停止所有脉冲动画。
        /// </summary>
        private void KillAllPulseTweens()
        {
            foreach (var kvp in _pulseTweens)
            {
                kvp.Value?.Kill();
            }
            _pulseTweens.Clear();
        }
    }
}
