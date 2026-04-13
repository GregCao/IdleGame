using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace IdleGame.UI
{
    /// <summary>
    /// 管理所有 Panel 的显示/隐藏和动画切换。
    /// 使用单例模式，确保全局唯一入口。
    /// </summary>
    public class UIPanelController : Singleton<UIPanelController>
    {
        /// <summary>
        /// 支持的面板名称枚举
        /// </summary>
        public enum PanelName
        {
            EquipmentPanel,
            CheckInPanel,
            QuestPanel,
            ShopPanel,
            SettingsPopup,
            RewardPopup
        }

        // ==================== Singleton ====================
        protected override bool IsPersistent() => false; // UI 管理器不需要跨场景
        // ==================== Configuration ====================

        [Header("面板预设")]
        [SerializeField] private GameObject _equipmentPanel;
        [SerializeField] private GameObject _checkInPanel;
        [SerializeField] private GameObject _questPanel;
        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private GameObject _settingsPopup;
        [SerializeField] private GameObject _rewardPopup;

        [Header("动画配置")]
        [Tooltip("面板滑入/滑出动画时长")]
        [SerializeField] private float _panelAnimationDuration = 0.3f;

        /// <summary>
        /// 面板预设字典（Name → GameObject）
        /// </summary>
        private Dictionary<PanelName, GameObject> _panelMap;

        /// <summary>
        /// 当前正在显示的面板（用于互斥管理）
        /// </summary>
        private HashSet<PanelName> _activePanels = new HashSet<PanelName>();

        /// <summary>
        /// 正在播放动画的面板（防止重复触发）
        /// </summary>
        private HashSet<PanelName> _animatingPanels = new HashSet<PanelName>();

        // ==================== Unity Lifecycle ====================

        protected override void Awake()
        {
            base.Awake();
            if (_instance == this)
            {
                BuildPanelMap();
            }
        }

        // ==================== Initialization ====================

        /// <summary>
        /// 构建面板名称到 GameObject 的映射
        /// </summary>
        private void BuildPanelMap()
        {
            _panelMap = new Dictionary<PanelName, GameObject>
            {
                { PanelName.EquipmentPanel, _equipmentPanel },
                { PanelName.CheckInPanel, _checkInPanel },
                { PanelName.QuestPanel, _questPanel },
                { PanelName.ShopPanel, _shopPanel },
                { PanelName.SettingsPopup, _settingsPopup },
                { PanelName.RewardPopup, _rewardPopup }
            };

            // 初始化：全部隐藏
            foreach (var kvp in _panelMap)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(false);
                }
            }
        }

        // ==================== Public API ====================

        /// <summary>
        /// 显示指定面板（带滑入动画）
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <param name="allowMultiple">是否允许与其他面板同时显示（默认 false，互斥）</param>
        public void ShowPanel(PanelName panelName, bool allowMultiple = false)
        {
            if (!ValidatePanel(panelName)) return;

            // 防止重复触发
            if (_animatingPanels.Contains(panelName)) return;

            GameObject panel = _panelMap[panelName];
            if (panel == null) return;

            // 互斥模式：先隐藏其他面板
            if (!allowMultiple)
            {
                HideAllPanels(ignoreSelf: panelName);
            }

            _animatingPanels.Add(panelName);

            // 设置初始位置（底部外侧）
            var rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                float startY = -rect.rect.height;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, startY);
            }

            panel.SetActive(true);
            _activePanels.Add(panelName);

            // 滑入动画
            var rt = panel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.DOAnchorPosY(0f, _panelAnimationDuration)
                  .SetEase(Ease.OutBack)
                  .OnComplete(() => _animatingPanels.Remove(panelName));
            }
            else
            {
                _animatingPanels.Remove(panelName);
            }
        }

        /// <summary>
        /// 重载：使用字符串显示面板
        /// </summary>
        public void ShowPanel(string panelName, bool allowMultiple = false)
        {
            if (Enum.TryParse<PanelName>(panelName, out var name))
            {
                ShowPanel(name, allowMultiple);
            }
            else
            {
                Debug.LogWarning($"[UIPanelController] Unknown panel name: {panelName}");
            }
        }

        /// <summary>
        /// 隐藏指定面板（带滑出动画）
        /// </summary>
        /// <param name="panelName">面板名称</param>
        public void HidePanel(PanelName panelName)
        {
            if (!ValidatePanel(panelName)) return;

            GameObject panel = _panelMap[panelName];
            if (panel == null || !panel.activeSelf) return;

            // 防止重复触发
            if (_animatingPanels.Contains(panelName)) return;
            _animatingPanels.Add(panelName);

            var rt = panel.GetComponent<RectTransform>();
            if (rt != null)
            {
                float endY = rt.rect.height;

                rt.DOAnchorPosY(endY, _panelAnimationDuration)
                  .SetEase(Ease.InBack)
                  .OnComplete(() =>
                  {
                      panel.SetActive(false);
                      _activePanels.Remove(panelName);
                      _animatingPanels.Remove(panelName);
                  });
            }
            else
            {
                panel.SetActive(false);
                _activePanels.Remove(panelName);
                _animatingPanels.Remove(panelName);
            }
        }

        /// <summary>
        /// 重载：使用字符串隐藏面板
        /// </summary>
        public void HidePanel(string panelName)
        {
            if (Enum.TryParse<PanelName>(panelName, out var name))
            {
                HidePanel(name);
            }
            else
            {
                Debug.LogWarning($"[UIPanelController] Unknown panel name: {panelName}");
            }
        }

        /// <summary>
        /// 隐藏所有面板（带滑出动画）
        /// </summary>
        /// <param name="ignoreSelf">不隐藏的指定面板</param>
        public void HideAllPanels(PanelName? ignoreSelf = null)
        {
            foreach (PanelName name in Enum.GetValues(typeof(PanelName)))
            {
                if (ignoreSelf.HasValue && name == ignoreSelf.Value)
                    continue;

                HidePanel(name);
            }
        }

        /// <summary>
        /// 隐藏所有面板（无参数版本）
        /// </summary>
        public void HideAllPanels()
        {
            HideAllPanels(ignoreSelf: null);
        }

        /// <summary>
        /// 切换面板显示状态（显示中则隐藏，隐藏中则显示）
        /// </summary>
        public void TogglePanel(PanelName panelName)
        {
            if (IsPanelActive(panelName))
                HidePanel(panelName);
            else
                ShowPanel(panelName);
        }

        /// <summary>
        /// 判断面板是否处于激活状态
        /// </summary>
        public bool IsPanelActive(PanelName panelName)
        {
            return _activePanels.Contains(panelName);
        }

        /// <summary>
        /// 判断面板是否正在播放动画
        /// </summary>
        public bool IsAnimating(PanelName panelName)
        {
            return _animatingPanels.Contains(panelName);
        }

        /// <summary>
        /// 立即关闭面板（无动画，用于场景切换等紧急情况）
        /// </summary>
        public void ForceClosePanel(PanelName panelName)
        {
            if (!ValidatePanel(panelName)) return;

            GameObject panel = _panelMap[panelName];
            if (panel == null) return;

            panel.SetActive(false);
            _activePanels.Remove(panelName);
            _animatingPanels.Remove(panelName);
        }

        /// <summary>
        /// 立即关闭所有面板（无动画）
        /// </summary>
        public void ForceCloseAllPanels()
        {
            foreach (PanelName name in Enum.GetValues(typeof(PanelName)))
            {
                ForceClosePanel(name);
            }
        }

        /// <summary>
        /// 获取面板 GameObject（供其他脚本直接操作）
        /// </summary>
        public GameObject GetPanel(PanelName panelName)
        {
            return ValidatePanel(panelName) ? _panelMap[panelName] : null;
        }

        /// <summary>
        /// 设置动画时长（运行时调整）
        /// </summary>
        public void SetAnimationDuration(float duration)
        {
            _panelAnimationDuration = Mathf.Max(0f, duration);
        }

        // ==================== Private Helpers ====================

        private bool ValidatePanel(PanelName name)
        {
            if (!_panelMap.ContainsKey(name) || _panelMap[name] == null)
            {
                Debug.LogWarning($"[UIPanelController] Panel '{name}' is not registered or has no GameObject assigned.");
                return false;
            }
            return true;
        }
    }
}
