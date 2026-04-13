using UnityEngine;

namespace IdleGame.Platform
{
    /// <summary>
    /// SDK运行模式配置
    /// 在 Unity Inspector 中设置，或通过代码动态切换
    /// </summary>
    public class SdkConfig : ScriptableObject
    {
        public enum SdkMode
        {
            /// <summary>
            /// Mock模式 - 模拟SDK响应，用于编辑器和开发测试
            /// </summary>
            Mock,

            /// <summary>
            /// 微信小程序生产模式 - 调用真实微信SDK
            /// </summary>
            WeChatMinProgram
        }

        [Header("SDK运行模式")]
        [Tooltip("Mock = 模拟响应（开发/测试用）；WeChatMinProgram = 真实微信SDK")]
        public SdkMode mode = SdkMode.Mock;

        [Header("微信广告位ID（生产模式使用）")]
        [Tooltip("激励视频广告单元ID，从微信小程序后台获取")]
        public string rewardedVideoAdUnitId = "adunit-xxxxxxxxxxxx";

        [Header("微信AppID（生产模式使用）")]
        [Tooltip("微信小程序的AppID，从微信公众平台获取")]
        public string wechatAppId = "wx0000000000000000";

        private static SdkConfig _instance;
        public static SdkConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试从Resources加载
                    _instance = Resources.Load<SdkConfig>("SdkConfig");
                    if (_instance == null)
                    {
                        // 使用默认配置（Mock模式）
                        Debug.LogWarning("[SdkConfig] SdkConfig asset not found in Resources, using default (Mock mode). " +
                            "Create Assets/Resources/SdkConfig.asset to configure SDK.");
                        _instance = CreateInstance<SdkConfig>();
                    }
                }
                return _instance;
            }
        }

        public bool IsMockMode => mode == SdkMode.Mock;
        public bool IsWeChatMode => mode == SdkMode.WeChatMinProgram;
    }
}
