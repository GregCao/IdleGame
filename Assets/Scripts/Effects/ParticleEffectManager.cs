using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;

namespace IdleGame.Effects
{
    /// <summary>
    /// 粒子特效类型枚举
    /// </summary>
    public enum EffectType
    {
        CoinBurst,    // 金币爆发
        Upgrade,      // 升级光效
        EnhanceBeam, // 强化光柱
        EquipGlow,    // 装备发光
    }

    /// <summary>
    /// 粒子特效管理器 - 单例模式，使用对象池
    /// 管理所有游戏粒子特效的播放和回收
    /// </summary>
    public class ParticleEffectManager : Singleton<ParticleEffectManager>
    {
        // ==================== Inspector 配置 ====================

        [Header("预制体引用")]
        [SerializeField] private ParticleSystem _coinBurstPrefab;
        [SerializeField] private ParticleSystem _upgradePrefab;
        [SerializeField] private ParticleSystem _enhanceBeamPrefab;
        [SerializeField] private ParticleSystem _equipGlowPrefab;

        [Header("对象池配置")]
        [SerializeField] private int _coinPoolSize = 10;
        [SerializeField] private int _upgradePoolSize = 5;
        [SerializeField] private int _enhancePoolSize = 5;
        [SerializeField] private int _equipGlowPoolSize = 10;

        [Header("特效参数")]
        [Tooltip("金币爆发粒子数量范围（min, max）")]
        [SerializeField] private Vector2Int _coinBurstCountRange = new Vector2Int(5, 10);

        // ==================== 私有字段 ====================

        /// <summary>各类型特效的对象池</summary>
        private Dictionary<EffectType, Queue<ParticleSystem>> _effectPools;

        /// <summary>所有已创建的对象（用于 Clear）</summary>
        private Dictionary<EffectType, List<ParticleSystem>> _allEffects;

        /// <summary>特效预制体字典</summary>
        private Dictionary<EffectType, ParticleSystem> _prefabs;

        private Transform _poolRoot;

        // ==================== Unity Lifecycle ====================

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void OnDestroy()
        {
            Clear();
        }

        // ==================== 初始化 ====================

        /// <summary>
        /// 初始化对象池
        /// </summary>
        public void Initialize()
        {
            BuildPrefabsMap();
            CreatePoolRoot();
            PrewarmPools();

            Debug.Log("[ParticleEffectManager] Initialized.");
        }

        private void BuildPrefabsMap()
        {
            _prefabs = new Dictionary<EffectType, ParticleSystem>
            {
                { EffectType.CoinBurst,    _coinBurstPrefab    },
                { EffectType.Upgrade,      _upgradePrefab      },
                { EffectType.EnhanceBeam, _enhanceBeamPrefab  },
                { EffectType.EquipGlow,   _equipGlowPrefab    },
            };
        }

        private void CreatePoolRoot()
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("[ParticleEffectPool]");
                go.transform.SetParent(transform);
                _poolRoot = go.transform;
            }
        }

        private void PrewarmPools()
        {
            _effectPools = new Dictionary<EffectType, Queue<ParticleSystem>>();
            _allEffects = new Dictionary<EffectType, List<ParticleSystem>>();

            // 初始化各类型池
            PrewarmPool(EffectType.CoinBurst, _coinPoolSize);
            PrewarmPool(EffectType.Upgrade, _upgradePoolSize);
            PrewarmPool(EffectType.EnhanceBeam, _enhancePoolSize);
            PrewarmPool(EffectType.EquipGlow, _equipGlowPoolSize);
        }

        private void PrewarmPool(EffectType type, int count)
        {
            if (!_prefabs.TryGetValue(type, out var prefab) || prefab == null)
            {
                _effectPools[type] = new Queue<ParticleSystem>();
                _allEffects[type] = new List<ParticleSystem>();
                return;
            }

            var pool = new Queue<ParticleSystem>(count);
            var all = new List<ParticleSystem>(count);

            for (int i = 0; i < count; i++)
            {
                var ps = CreateEffectInstance(type, prefab);
                if (ps != null)
                {
                    pool.Enqueue(ps);
                    all.Add(ps);
                }
            }

            _effectPools[type] = pool;
            _allEffects[type] = all;
        }

        /// <summary>
        /// 创建特效实例
        /// </summary>
        private ParticleSystem CreateEffectInstance(EffectType type, ParticleSystem prefab)
        {
            var go = Instantiate(prefab.gameObject, _poolRoot);
            go.name = $"Effect_{type}";
            var ps = go.GetComponent<ParticleSystem>();
            go.SetActive(false);
            return ps;
        }

        // ==================== 公开 API ====================

        /// <summary>
        /// 播放特效
        /// </summary>
        /// <param name="effectType">特效类型</param>
        /// <param name="position">世界坐标位置</param>
        public void PlayEffect(EffectType effectType, Vector3 position)
        {
            PlayEffect(effectType, position, Quaternion.identity);
        }

        /// <summary>
        /// 播放特效（带旋转）
        /// </summary>
        public void PlayEffect(EffectType effectType, Vector3 position, Quaternion rotation)
        {
            var ps = GetFromPool(effectType);
            if (ps == null)
            {
                Debug.LogError($"[ParticleEffectManager] Failed to play effect: {effectType}. Pool empty or prefab missing.");
                return;
            }

            // 配置特效参数
            ConfigureEffect(ps, effectType);

            // 设置位置和旋转
            ps.transform.position = position;
            ps.transform.rotation = rotation;

            // 激活并播放
            ps.gameObject.SetActive(true);
            ps.Play();

            // 播放完毕后回收（利用 main.duration + startLifetime）
            float duration = GetEffectDuration(ps);
            DOVirtual.DelayedCall(duration + 0.1f, () => ReturnToPool(effectType, ps));
        }

        /// <summary>
        /// 播放特效（使用 Transform 作为锚点）
        /// </summary>
        public void PlayEffect(EffectType effectType, Transform anchor)
        {
            if (anchor == null) return;
            PlayEffect(effectType, anchor.position, anchor.rotation);
        }

        /// <summary>
        /// 清理所有特效，归还所有对象到池中
        /// </summary>
        public void Clear()
        {
            if (_allEffects == null) return;

            foreach (var kvp in _allEffects)
            {
                foreach (var ps in kvp.Value)
                {
                    if (ps == null) continue;
                    ps.Stop();
                    ps.gameObject.SetActive(false);
                }
            }
        }

        // ==================== 特效配置 ====================

        /// <summary>
        /// 根据特效类型配置粒子参数
        /// </summary>
        private void ConfigureEffect(ParticleSystem ps, EffectType effectType)
        {
            switch (effectType)
            {
                case EffectType.CoinBurst:
                    ConfigureCoinBurst(ps);
                    break;
                case EffectType.Upgrade:
                    ConfigureUpgrade(ps);
                    break;
                case EffectType.EnhanceBeam:
                    ConfigureEnhanceBeam(ps);
                    break;
                case EffectType.EquipGlow:
                    ConfigureEquipGlow(ps);
                    break;
            }
        }

        /// <summary>
        /// 配置金币爆发特效
        /// </summary>
        private void ConfigureCoinBurst(ParticleSystem ps)
        {
            // 随机数量 5-10
            int count = UnityEngine.Random.Range(_coinBurstCountRange.x, _coinBurstCountRange.y + 1);

            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 200f;
            main.startSize = 0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 发射球形爆发
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)count));

            // 金色
            var color = ps.main.startColor;
            color.mode = ParticleSystemGradientMode.RandomColor;
            color.colorMin = new Color(1f, 0.9f, 0.2f); // 浅金
            color.colorMax = new Color(1f, 0.7f, 0.1f); // 深金

            // 向上漂移
            var force = ps.force;
            force.enabled = true;
            force.y = 2f;

            // 渐隐
            var trails = ps.trails;
            trails.enabled = false;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var colorGradient = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.3f, 1f),
                new Color(1f, 0.8f, 0.1f, 0f)
            );
            colorOverLifetime.color = colorGradient;
        }

        /// <summary>
        /// 配置升级光效（中心爆发，多彩）
        /// </summary>
        private void ConfigureUpgrade(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 150f;
            main.startSize = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            // 中心爆发
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 80));

            // 多彩
            var color = ps.main.startColor;
            color.mode = ParticleSystemGradientMode.RandomColor;
            color.colorMin = new Color(0.2f, 1f, 0.5f, 1f);  // 绿
            color.colorMax = new Color(1f, 0.5f, 1f, 1f);    // 粉紫

            // 向外爆开
            var force = ps.force;
            force.enabled = true;
            force.y = 1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                Color.white,
                new Color(1f, 1f, 0.5f, 0f)
            );

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        }

        /// <summary>
        /// 配置强化光柱（从下往上）
        /// </summary>
        private void ConfigureEnhanceBeam(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 400f;
            main.startSize = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;
            main.duration = 0.5f;
            main.loop = false;

            // 锥形向上
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.3f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 120));

            // 蓝白色光柱
            var color = ps.main.startColor;
            color.mode = ParticleSystemGradientMode.RandomColor;
            color.colorMin = new Color(0.6f, 0.8f, 1f, 1f);  // 浅蓝
            color.colorMax = new Color(1f, 1f, 1f, 1f);      // 白色

            // 向上加速
            var force = ps.force;
            force.enabled = true;
            force.y = 10f;
            force.z = 0f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Color(0.7f, 0.9f, 1f, 1f),
                new Color(0.8f, 0.9f, 1f, 0f)
            );
        }

        /// <summary>
        /// 配置装备发光（持续环形光晕）
        /// </summary>
        private void ConfigureEquipGlow(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 50f;
            main.startSize = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 60;
            main.loop = false;

            // 环形
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;
            shape.arc = 360f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 30));
            emission.rateOverTime = 0f;

            // 品质色（橙金）
            var color = ps.main.startColor;
            color.mode = ParticleSystemGradientMode.RandomColor;
            color.colorMin = new Color(1f, 0.8f, 0.3f, 1f);  // 橙
            color.colorMax = new Color(1f, 0.95f, 0.5f, 1f); // 金

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.5f, 1f),
                new Color(1f, 0.7f, 0.2f, 0f)
            );

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2f));
        }

        /// <summary>
        /// 获取特效持续时间
        /// </summary>
        private float GetEffectDuration(ParticleSystem ps)
        {
            if (ps == null) return 0.5f;
            var main = ps.main;
            return main.startLifetime.constantMax + main.duration;
        }

        // ==================== 对象池 ====================

        /// <summary>
        /// 从池中获取特效实例
        /// </summary>
        private ParticleSystem GetFromPool(EffectType type)
        {
            if (!_effectPools.TryGetValue(type, out var pool) || pool == null)
                return null;

            if (pool.Count > 0)
            {
                var ps = pool.Dequeue();
                if (ps != null)
                    return ps;
            }

            // 池空，动态创建
            if (_prefabs.TryGetValue(type, out var prefab) && prefab != null)
            {
                var ps = CreateEffectInstance(type, prefab);
                if (ps != null)
                {
                    if (!_allEffects.ContainsKey(type))
                        _allEffects[type] = new List<ParticleSystem>();
                    _allEffects[type].Add(ps);
                }
                return ps;
            }

            return null;
        }

        /// <summary>
        /// 归还特效到池中
        /// </summary>
        private void ReturnToPool(EffectType type, ParticleSystem ps)
        {
            if (ps == null) return;

            ps.Stop();
            ps.gameObject.SetActive(false);

            if (!_effectPools.TryGetValue(type, out var pool))
            {
                pool = new Queue<ParticleSystem>();
                _effectPools[type] = pool;
            }

            pool.Enqueue(ps);
        }
    }
}
