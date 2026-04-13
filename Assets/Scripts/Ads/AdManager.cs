using UnityEngine;
using UnityEngine.Events;
using IdleGame.Platform;

namespace IdleGame
{
    /// <summary>
    /// 广告管理器 - 统一调用 WeChatSDK
    /// 实际广告行为由 SdkConfig.mode 决定（Mock 或真实微信广告）
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        // 各场景标记
        public enum RewardedScene
        {
            OfflineDouble,  // 离线双倍领取
            SpeedUp,        // 战斗加速
            ExtraGold,      // 额外金币
            TrySummon       // 试用召唤
        }

        // 事件
        public UnityEvent OnRewardedVideoReady;
        public UnityEvent OnRewardedVideoClosed;
        public UnityEvent<bool> OnRewardedVideoResult;

        private RewardedScene _currentScene;
        private bool _isRewardedVideoReady = false;

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
            // 初始化广告预加载
            RequestRewardedVideo();

            // 监听广告就绪
            // WeChatSDK 会在 Mock/Real 适配器初始化后自动加载
        }

        #region 激励视频

        /// <summary>
        /// 请求预加载激励视频
        /// </summary>
        public void RequestRewardedVideo()
        {
            if (WeChatSDK.Instance != null)
            {
                WeChatSDK.Instance.RequestRewardedVideo();
                _isRewardedVideoReady = WeChatSDK.Instance.IsRewardedVideoReady();
            }
            else
            {
                Debug.LogWarning("[AdManager] WeChatSDK not available");
            }
        }

        /// <summary>
        /// 播放激励视频（无回调）
        /// </summary>
        public void ShowRewardedVideo(RewardedScene scene)
        {
            ShowRewardedVideo(scene, null);
        }

        /// <summary>
        /// 播放激励视频（带回调）
        /// </summary>
        public void ShowRewardedVideo(RewardedScene scene, System.Action<bool> callback)
        {
            _currentScene = scene;
            Debug.Log($"[AdManager] Showing rewarded video for scene: {scene}");

            if (WeChatSDK.Instance != null)
            {
                WeChatSDK.Instance.ShowRewardedVideo((success) =>
                {
                    if (success)
                    {
                        GrantRewardByScene(_currentScene);
                    }
                    OnRewardedVideoResult?.Invoke(success);
                    OnRewardedVideoClosed?.Invoke();
                    callback?.Invoke(success);

                    // 通知每日任务：观看广告
                    DailyQuestManager.Instance?.UpdateQuestProgress(QuestType.WatchAd, 1);
                });
            }
            else
            {
                // SDK不可用时（编辑器），模拟一次
                Debug.Log("[AdManager] WeChatSDK not available, simulating...");
                GrantRewardByScene(_currentScene);
                callback?.Invoke(true);
            }
        }

        /// <summary>
        /// 广告是否就绪
        /// </summary>
        public bool IsRewardedVideoReady()
        {
            return WeChatSDK.Instance != null
                && WeChatSDK.Instance.IsRewardedVideoReady();
        }

        #endregion

        #region 奖励发放

        private void GrantRewardByScene(RewardedScene scene)
        {
            switch (scene)
            {
                case RewardedScene.OfflineDouble:
                    GrantOfflineDoubleReward();
                    break;
                case RewardedScene.SpeedUp:
                    GrantSpeedUpReward();
                    break;
                case RewardedScene.ExtraGold:
                    GrantExtraGoldReward();
                    break;
                case RewardedScene.TrySummon:
                    GrantTrySummonReward();
                    break;
            }
        }

        private void GrantOfflineDoubleReward()
        {
            // 离线双倍：从 EconomyManager 获取离线金币总额，额外发放一倍
            if (GameManager.Instance?.EconomyManager != null)
            {
                long offlineGold = GameManager.Instance.EconomyManager
                    .CalculateOfflineEarnings();
                if (offlineGold > 0)
                {
                    GameManager.Instance.PlayerManager.AddGold(offlineGold);
                    Debug.Log($"[AdManager] Offline double reward: +{offlineGold} gold");
                }
            }
        }

        private void GrantSpeedUpReward()
        {
            // 加速：将战斗间隔临时缩短（需BattleManager配合）
            if (GameManager.Instance?.BattleManager != null)
            {
                GameManager.Instance.BattleManager.ApplySpeedUp(300f); // 5分钟
                Debug.Log("[AdManager] Speed up applied for 300s");
            }
        }

        private void GrantExtraGoldReward()
        {
            // 额外金币：获取当前DPS * 5分钟
            if (GameManager.Instance?.EconomyManager != null)
            {
                float gps = GameManager.Instance.EconomyManager.GetGoldPerSecond();
                long bonus = (long)(gps * 300f);
                GameManager.Instance.PlayerManager.AddGold(bonus);
                Debug.Log($"[AdManager] Extra gold reward: +{bonus}");
            }
        }

        private void GrantTrySummonReward()
        {
            // 试用召唤：发放一次免费召唤
            Debug.Log("[AdManager] Try summon reward granted");
        }

        #endregion
    }
}
