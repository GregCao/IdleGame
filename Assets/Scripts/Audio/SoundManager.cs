using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;

namespace IdleGame.Audio
{
    /// <summary>
    /// 音效类型枚举
    /// </summary>
    public enum SoundType
    {
        // BGM
        BGM_Battle,
        BGM_Menu,

        // SFX - UI交互
        SFX_Click,
        SFX_Upgrade,
        SFX_Reward,
        SFX_Chest,

        // SFX - 战斗
        SFX_Kill,
        SFX_Crit,
    }

    /// <summary>
    /// 音效管理器 - 单例模式
    /// 管理背景音乐和音效播放，支持音量分离控制
    /// </summary>
    public class SoundManager : Singleton<SoundManager>
    {
        // ==================== Inspector 配置 ====================

        [Header("音频片段配置")]
        [SerializeField] private AudioClip _bgmBattle;
        [SerializeField] private AudioClip _bgmMenu;
        [SerializeField] private AudioClip _sfxClick;
        [SerializeField] private AudioClip _sfxUpgrade;
        [SerializeField] private AudioClip _sfxKill;
        [SerializeField] private AudioClip _sfxCrit;
        [SerializeField] private AudioClip _sfxReward;
        [SerializeField] private AudioClip _sfxChest;

        [Header("音量配置")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _bgmVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float _sfxVolume = 1f;

        [Header("SFX 混音池配置")]
        [SerializeField] private int _sfxSourceCount = 8;

        // ==================== 私有字段 ====================

        /// <summary>BGM 专用 AudioSource</summary>
        private AudioSource _bgmSource;

        /// <summary>SFX 混音池</summary>
        private List<AudioSource> _sfxSources;

        /// <summary>当前播放的 BGM 类型</summary>
        private SoundType _currentBgm = SoundType.BGM_Battle;

        /// <summary>音效片段字典</summary>
        private Dictionary<SoundType, AudioClip> _soundMap;

        // ==================== 属性 ====================

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateAllVolumes();
            }
        }

        public float BGMVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                UpdateBGMVolume();
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                UpdateSFXVolumes();
            }
        }

        /// <summary>
        /// 全局静音开关
        /// </summary>
        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                UpdateAllVolumes();
            }
        }
        private bool _muted = false;

        // ==================== Unity Lifecycle ====================

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void OnDestroy()
        {
            // 清理
            if (_sfxSources != null)
            {
                foreach (var src in _sfxSources)
                {
                    if (src != null)
                        Destroy(src);
                }
                _sfxSources.Clear();
            }
        }

        // ==================== 初始化 ====================

        private void Initialize()
        {
            BuildSoundMap();
            CreateAudioSources();
            SubscribeToGameEvents();

            Debug.Log("[SoundManager] Initialized.");
        }

        /// <summary>
        /// 构建音效片段字典
        /// </summary>
        private void BuildSoundMap()
        {
            _soundMap = new Dictionary<SoundType, AudioClip>
            {
                { SoundType.BGM_Battle, _bgmBattle },
                { SoundType.BGM_Menu,   _bgmMenu   },
                { SoundType.SFX_Click,  _sfxClick  },
                { SoundType.SFX_Upgrade,_sfxUpgrade},
                { SoundType.SFX_Kill,   _sfxKill   },
                { SoundType.SFX_Crit,   _sfxCrit   },
                { SoundType.SFX_Reward, _sfxReward },
                { SoundType.SFX_Chest,  _sfxChest  },
            };
        }

        /// <summary>
        /// 创建音频源
        /// </summary>
        private void CreateAudioSources()
        {
            // BGM Source
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.outputAudioMixerGroup = null;
            UpdateBGMVolume();

            // SFX Sources Pool
            _sfxSources = new List<AudioSource>(_sfxSourceCount);
            for (int i = 0; i < _sfxSourceCount; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;
                src.outputAudioMixerGroup = null;
                _sfxSources.Add(src);
            }
        }

        /// <summary>
        /// 订阅游戏事件以自动播放音效
        /// </summary>
        private void SubscribeToGameEvents()
        {
            if (GameManager.Instance == null) return;

            var bm = GameManager.Instance.BattleManager;
            var pm = GameManager.Instance.PlayerManager;

            if (bm != null)
            {
                bm.OnMonsterKilled += (_, reward) => PlaySFX(SoundType.SFX_Kill);
                // 暴击需要额外事件，见 BattleManager.CritEvent
                bm.OnCrit += () => PlaySFX(SoundType.SFX_Crit);
            }

            if (pm != null)
            {
                pm.OnLevelUp += _ => PlaySFX(SoundType.SFX_Upgrade);
            }
        }

        // ==================== 公开 API ====================

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="soundType">BGM 类型</param>
        public void PlayBGM(SoundType soundType)
        {
            if (!_soundMap.TryGetValue(soundType, out var clip) || clip == null)
            {
                Debug.LogWarning($"[SoundManager] No BGM clip found for {soundType}");
                return;
            }

            _currentBgm = soundType;
            _bgmSource.clip = clip;
            _bgmSource.volume = EffectiveBGMVolume;
            _bgmSource.Play();
        }

        /// <summary>
        /// 重载：使用字符串名称播放 BGM
        /// </summary>
        public void PlayBGM(string bgmName)
        {
            if (Enum.TryParse<SoundType>(bgmName, true, out var soundType))
            {
                PlayBGM(soundType);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] Unknown BGM name: {bgmName}");
            }
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundType">音效类型</param>
        public void PlaySFX(SoundType soundType)
        {
            if (!_soundMap.TryGetValue(soundType, out var clip) || clip == null)
            {
                // SFX 未配置时不警告，静默忽略
                return;
            }

            // 找一个空闲的 AudioSource
            AudioSource src = GetFreeSFXSource();
            if (src == null)
            {
                // 全部占用，复用第一个（策略可调）
                src = _sfxSources[0];
            }

            src.clip = clip;
            src.volume = EffectiveSFXVolume;
            src.Play();
        }

        /// <summary>
        /// 重载：使用字符串名称播放 SFX
        /// </summary>
        public void PlaySFX(string sfxName)
        {
            if (Enum.TryParse<SoundType>(sfxName, true, out var soundType))
            {
                PlaySFX(soundType);
            }
        }

        /// <summary>
        /// 设置主音量（0 ~ 1）
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
        }

        /// <summary>
        /// 设置音乐音量（0 ~ 1）
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            BGMVolume = volume;
        }

        /// <summary>
        /// 设置音效音量（0 ~ 1）
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            SFXVolume = volume;
        }

        /// <summary>
        /// 停止 BGM
        /// </summary>
        public void StopBGM()
        {
            _bgmSource.Stop();
        }

        /// <summary>
        /// 暂停 BGM
        /// </summary>
        public void PauseBGM()
        {
            _bgmSource.Pause();
        }

        /// <summary>
        /// 恢复 BGM
        /// </summary>
        public void ResumeBGM()
        {
            _bgmSource.UnPause();
        }

        /// <summary>
        /// 切换 BGM（战斗 <-> 菜单）
        /// </summary>
        public void ToggleBGM(bool battle)
        {
            PlayBGM(battle ? SoundType.BGM_Battle : SoundType.BGM_Menu);
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 获取一个当前未在播放的 SFX AudioSource
        /// </summary>
        private AudioSource GetFreeSFXSource()
        {
            foreach (var src in _sfxSources)
            {
                if (src != null && !src.isPlaying)
                    return src;
            }
            return null;
        }

        private float EffectiveBGMVolume => _muted ? 0f : _masterVolume * _bgmVolume;
        private float EffectiveSFXVolume => _muted ? 0f : _masterVolume * _sfxVolume;

        private void UpdateBGMVolume()
        {
            if (_bgmSource != null)
                _bgmSource.volume = EffectiveBGMVolume;
        }

        private void UpdateSFXVolumes()
        {
            // SFX 音量在播放时单独设置，不影响正在播放的
        }

        private void UpdateAllVolumes()
        {
            UpdateBGMVolume();
            // SFX 在播放时会读取最新音量，无需批量更新
        }
    }
}
