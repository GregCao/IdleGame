using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

namespace IdleGame.UI
{
    /// <summary>
    /// Loading 屏幕管理。
    /// 支持进度条、百分比文字、渐入渐出动画。
    /// 典型用法：场景切换前 ShowLoading()，切换完成后 HideLoading()。
    /// </summary>
    public class LoadingScreen : Singleton<LoadingScreen>
    {
        // ==================== Singleton ====================
        protected override bool IsPersistent() => false; // Loading 界面不需要跨场景

        // ==================== UI References ====================
        [Header("根节点")]}
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("进度条")]
        [SerializeField] private Image _progressBarFill;

        [Header("百分比文字")]
        [SerializeField] private TextMeshProUGUI _percentText;

        [Header("提示文字")]
        [SerializeField] private TextMeshProUGUI _tipText;

        [Header("渐入/渐出时长")]
        [SerializeField] private float _fadeDuration = 0.3f;

        [Header("进度条填充速度（每秒增量，越大越快）")]
        [SerializeField] private float _fillSpeed = 2f;

        // ==================== Internal State ====================
        private bool _isShowing;
        private float _targetProgress;
        private float _displayProgress;
        private Sequence _fadeSequence;

        private const float MinProgress = 0f;
        private const float MaxProgress = 1f;

        // ==================== Unity Lifecycle ====================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            CacheComponents();
            // 初始化为隐藏
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        private void Update()
        {
            if (!_isShowing)
                return;

            // 平滑逼近目标进度
            if (Mathf.Abs(_displayProgress - _targetProgress) > 0.001f)
            {
                _displayProgress = Mathf.MoveTowards(
                    _displayProgress,
                    _targetProgress,
                    _fillSpeed * Time.deltaTime
                );

                // 限制范围
                _displayProgress = Mathf.Clamp01(_displayProgress);

                UpdateProgressUI(_displayProgress);
            }
        }

        private void OnDestroy()
        {
            _fadeSequence?.Kill();
        }

        // ==================== Initialization ====================

        /// <summary>
        /// 尝试从 Hierarchy 自动查找子节点引用。
        /// </summary>
        private void CacheComponents()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null && transform.childCount > 0)
            {
                _canvasGroup = transform.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_progressBarFill == null)
                _progressBarFill = transform.Find("ProgressBar/Fill")?.GetComponent<Image>();

            if (_percentText == null)
                _percentText = transform.Find("PercentText")?.GetComponent<TextMeshProUGUI>();

            if (_tipText == null)
                _tipText = transform.Find("TipText")?.GetComponent<TextMeshProUGUI>();

            // 初始进度归零
            if (_progressBarFill != null)
                _progressBarFill.fillAmount = 0f;

            if (_percentText != null)
                _percentText.text = "0%";
        }

        // ==================== Public API ====================

        /// <summary>
        /// 显示 Loading 屏幕并设置提示文字。
        /// 若已显示则仅更新提示文字。
        /// </summary>
        /// <param name="tipText">提示文字（如"正在加载资源..."）</param>
        public void ShowLoading(string tipText = "正在加载...")
        {
            _isShowing = true;
            _targetProgress = 0f;
            _displayProgress = 0f;

            if (_tipText != null)
                _tipText.text = tipText;

            if (_progressBarFill != null)
                _progressBarFill.fillAmount = 0f;

            if (_percentText != null)
                _percentText.text = "0%";

            // 渐入
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            _fadeSequence?.Kill();
            _fadeSequence = _canvasGroup
                .DOFade(1f, _fadeDuration)
                .SetEase(Ease.Linear);
        }

        /// <summary>
        /// 更新加载进度（0.0 ~ 1.0）。
        /// 内部做平滑插值，避免进度条跳动。
        /// </summary>
        /// <param name="progress">目标进度（会被插值逼近）</param>
        public void UpdateProgress(float progress)
        {
            if (!_isShowing)
            {
                Debug.LogWarning("[LoadingScreen] UpdateProgress called but loading screen is not showing.");
                return;
            }

            _targetProgress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// 直接设置进度条（无插值，常用于异步加载回调）。
        /// </summary>
        public void SetProgressInstant(float progress)
        {
            float p = Mathf.Clamp01(progress);
            _targetProgress = p;
            _displayProgress = p;
            UpdateProgressUI(p);
        }

        /// <summary>
        /// 隐藏 Loading 屏幕。
        /// </summary>
        /// <param name="onComplete">隐藏动画完成后的回调（可选）</param>
        public void HideLoading(Action onComplete = null)
        {
            if (!_isShowing && _canvasGroup.alpha <= 0.01f)
            {
                onComplete?.Invoke();
                return;
            }

            _isShowing = false;

            _fadeSequence?.Kill();
            _fadeSequence = _canvasGroup
                .DOFade(0f, _fadeDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;

                    // 重置进度
                    _targetProgress = 0f;
                    _displayProgress = 0f;
                    if (_progressBarFill != null)
                        _progressBarFill.fillAmount = 0f;

                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 渐入 + 设置进度（组合调用，适用于已知总时长的情况）。
        /// </summary>
        /// <param name="tipText">提示文字</param>
        /// <param name="duration">总进度动画时长（秒）</param>
        /// <param name="onComplete">完成回调</param>
        public void ShowLoadingWithFixedDuration(string tipText, float duration, Action onComplete = null)
        {
            ShowLoading(tipText);

            // 通过 Tween 驱动进度
            if (_progressBarFill != null)
            {
                _progressBarFill.fillAmount = 0f;
                _progressBarFill
                    .DOFillAmount(1f, duration)
                    .SetEase(Ease.Linear)
                    .OnUpdate(() =>
                    {
                        _displayProgress = _progressBarFill.fillAmount;
                        _targetProgress = _displayProgress;
                        if (_percentText != null)
                            _percentText.text = Mathf.RoundToInt(_displayProgress * 100f) + "%";
                    })
                    .OnComplete(() =>
                    {
                        HideLoading();
                        onComplete?.Invoke();
                    });
            }
        }

        // ==================== Private Helpers ====================

        private void UpdateProgressUI(float progress)
        {
            if (_progressBarFill != null)
                _progressBarFill.fillAmount = progress;

            if (_percentText != null)
                _percentText.text = Mathf.RoundToInt(progress * 100f) + "%";
        }
    }
}
