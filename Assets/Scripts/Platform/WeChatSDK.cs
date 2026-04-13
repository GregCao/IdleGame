using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame.Platform
{
    /// <summary>
    /// 微信SDK适配器接口
    /// </summary>
    public interface IWeChatAdapter
    {
        void Initialize(Action onComplete);
        void ShowRewardedVideo(Action<bool> onComplete);
        bool IsRewardedVideoReady();
        void RequestRewardedVideo();
        void Pay(string productId, Action<bool> onComplete);
        void Share(string title, string description, string imageUrl = null);
        void ShowShareButton();
    }

    /// <summary>
    /// Mock模式适配器 - 用于编辑器和开发测试
    /// </summary>
    public class MockWeChatAdapter : IWeChatAdapter
    {
        private bool _rewardedVideoReady = true;
        private float _mockDelay = 0.5f;

        public void Initialize(Action onComplete)
        {
            Debug.Log("[MockWeChat] SDK initialized (Mock mode)");
            onComplete?.Invoke();
        }

        public bool IsRewardedVideoReady() => _rewardedVideoReady;

        public void RequestRewardedVideo()
        {
            _rewardedVideoReady = true;
            Debug.Log("[MockWeChat] Rewarded video loaded");
        }

        public void ShowRewardedVideo(Action<bool> onComplete)
        {
            if (!_rewardedVideoReady)
            {
                Debug.LogWarning("[MockWeChat] Rewarded video not ready!");
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log("[MockWeChat] Showing rewarded video (Mock)...");
            // 模拟完整观看
            CoroutineHelper.StartCoroutine(DelayedCallback(_mockDelay, () =>
            {
                Debug.Log("[MockWeChat] Rewarded video completed");
                _rewardedVideoReady = false;
                onComplete?.Invoke(true);
            }));
        }

        public void Pay(string productId, Action<bool> onComplete)
        {
            Debug.Log($"[MockWeChat] Processing payment for: {productId}");
            CoroutineHelper.StartCoroutine(DelayedCallback(_mockDelay, () =>
            {
                Debug.Log("[MockWeChat] Payment success (Mock)");
                onComplete?.Invoke(true);
            }));
        }

        public void Share(string title, string description, string imageUrl = null)
        {
            Debug.Log($"[MockWeChat] Share: {title} - {description}");
        }

        public void ShowShareButton()
        {
            Debug.Log("[MockWeChat] Share button shown (Mock)");
        }

        private static System.Collections.IEnumerator DelayedCallback(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }
    }

    /// <summary>
    /// 微信小程序真实适配器 - 需要导入微信官方SDK后完善实现
    /// 
    /// 接入步骤：
    /// 1. 在微信公众平台申请小程序，获得 AppID
    /// 2. 在微信小程序后台「广告」页面申请广告位，获得 adUnitId
    /// 3. 导入微信官方 WXSDK 插件（TGPA 或 wemeet-unity-minigame）
    /// 4. 将 SdkConfig.mode 切换为 WeChatMinProgram，并填入正确的 adUnitId 和 AppID
    /// 5. 完善本文件中的真实SDK调用代码
    /// </summary>
    public class RealWeChatAdapter : IWeChatAdapter
    {
        private bool _rewardedVideoReady = false;
        private Action<bool> _rewardedCallback;
        private Action<bool> _payCallback;
        private string _currentAdUnitId;

        public void Initialize(Action onComplete)
        {
            Debug.Log("[RealWeChat] Initializing WeChat SDK...");

            // ============================================================
            // TODO: 实际初始化微信SDK（根据你使用的微信SDK包填写）
            // 示例（TGPA）：
            // TGPA.Init(success =>
            // {
            //     Debug.Log($"[RealWeChat] WeChat SDK init: {success}");
            //     onComplete?.Invoke();
            // });
            //
            // 示例（wemeet-unity-minigame）：
            // WXSDK.Init((code, msg) =>
            // {
            //     Debug.Log($"[RealWeChat] WeChat SDK init: {code} {msg}");
            //     onComplete?.Invoke(code == 0);
            // });
            // ============================================================
            onComplete?.Invoke();
        }

        public bool IsRewardedVideoReady() => _rewardedVideoReady;

        public void RequestRewardedVideo()
        {
            string adUnitId = SdkConfig.Instance.rewardedVideoAdUnitId;
            Debug.Log($"[RealWeChat] Requesting rewarded video, adUnitId: {adUnitId}");

            // ============================================================
            // TODO: 加载激励视频广告
            // 示例：
            // var ad = WX.CreateRewardedVideoAd(adUnitId);
            // ad.OnLoad += () =>
            // {
            //     _rewardedVideoReady = true;
            //     Debug.Log("[RealWeChat] Rewarded video ready");
            // };
            // ad.OnError += (err) =>
            // {
            //     Debug.LogError($"[RealWeChat] Rewarded video error: {err}");
            //     _rewardedVideoReady = false;
            // };
            // ad.OnClose += (hasRewarded) =>
            // {
            //     _rewardedCallback?.Invoke(hasRewarded);
            //     _rewardedVideoReady = false;
            // };
            // ad.Load();
            // ============================================================
        }

        public void ShowRewardedVideo(Action<bool> onComplete)
        {
            _rewardedCallback = onComplete;

            if (!_rewardedVideoReady)
            {
                Debug.LogWarning("[RealWeChat] Rewarded video not ready, requesting...");
                RequestRewardedVideo();
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log("[RealWeChat] Showing rewarded video...");

            // ============================================================
            // TODO: 显示激励视频
            // _rewardedVideoAd.Show();
            // ============================================================
        }

        public void Pay(string productId, Action<bool> onComplete)
        {
            _payCallback = onComplete;
            Debug.Log($"[RealWeChat] Initiating payment for: {productId}");

            // ============================================================
            // TODO: 发起微信支付
            // 微信小程序支付流程：
            // 1. 前端调用服务端接口获取支付参数（wx.requestPayment）
            // 2. 服务端调用微信支付统一下单API
            //
            // 示例：
            // var param = new WXPaymentParam
            // {
            //     appId = SdkConfig.Instance.wechatAppId,
            //     timeStamp = timestamp,
            //     nonceStr = nonceStr,
            //     package = "prepay_id=xxxxx",
            //     signType = "MD5",
            //     paySign = signature
            // };
            // WX.RequestPayment(param,
            //     () => { Debug.Log("Payment success"); _payCallback?.Invoke(true); },
            //     (err) => { Debug.LogError($"Payment failed: {err}"); _payCallback?.Invoke(false); }
            // );
            // ============================================================
            onComplete?.Invoke(false); // 未实现时返回失败
        }

        public void Share(string title, string description, string imageUrl = null)
        {
            // ============================================================
            // TODO: 分享到聊天
            // WX.ShareAppMessage(new ShareMessage
            // {
            //     title = title,
            //     description = description,
            //     imageUrl = imageUrl,
            //     query = "from=game"
            // });
            // ============================================================
            Debug.Log($"[RealWeChat] Share: {title} - {description}");
        }

        public void ShowShareButton()
        {
            // ============================================================
            // TODO: 显示转发按钮
            // WX.ShowShareMenu(new ShareMenuConfig
            // {
            //     withShareTicket = true,
            //     imageUrl = "..."
            // });
            // ============================================================
            Debug.Log("[RealWeChat] Share button shown");
        }
    }

    /// <summary>
    /// 微信SDK统一入口
    /// 根据 SdkConfig.mode 自动选择 Mock 或真实适配器
    /// 
    /// 使用方式：
    ///   WeChatSDK.Instance.ShowRewardedVideo(callback);
    ///   WeChatSDK.Instance.Pay("product_id", callback);
    /// </summary>
    public class WeChatSDK : Singleton<WeChatSDK>
    {
        private IWeChatAdapter _adapter;

        protected override void Awake()
        {
            base.Awake();
            InitializeAdapter();
        }

        private void InitializeAdapter()
        {
            if (SdkConfig.Instance.IsMockMode)
            {
                _adapter = new MockWeChatAdapter();
                Debug.Log("[WeChatSDK] Running in MOCK mode. Set SdkConfig to WeChatMinProgram for production.");
            }
            else
            {
                _adapter = new RealWeChatAdapter();
                Debug.Log("[WeChatSDK] Running in WECHAT MINIPROGRAM mode.");
            }

            _adapter.Initialize(() =>
            {
                Debug.Log("[WeChatSDK] Adapter initialized successfully.");
            });
        }

        #region 激励视频广告

        public void ShowRewardedVideo(Action<bool> callback)
        {
            _adapter.ShowRewardedVideo(callback);
        }

        public bool IsRewardedVideoReady() => _adapter.IsRewardedVideoReady();

        public void RequestRewardedVideo() => _adapter.RequestRewardedVideo();

        #endregion

        #region 支付

        public void Pay(string productId, Action<bool> callback)
        {
            _adapter.Pay(productId, callback);
        }

        #endregion

        #region 分享

        public void Share(string title, string description, string imageUrl = null)
        {
            _adapter.Share(title, description, imageUrl);
        }

        public void ShowShareButton()
        {
            _adapter.ShowShareButton();
        }

        #endregion
    }

    /// <summary>
    /// 协程辅助工具（用于Mock模式的异步延迟回调）
    /// </summary>
    public static class CoroutineHelper
    {
        private static MonoBehaviour _host;
        public static MonoBehaviour Host
        {
            get
            {
                if (_host == null)
                {
                    var go = new GameObject("CoroutineHelper");
                    _host = go.AddComponent<CoroutineHelperBehaviour>();
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }
                return _host;
            }
        }

        public static Coroutine StartCoroutine(System.Collections.IEnumerator enumerator)
        {
            return Host.StartCoroutine(enumerator);
        }
    }

    public class CoroutineHelperBehaviour : MonoBehaviour { }
}
