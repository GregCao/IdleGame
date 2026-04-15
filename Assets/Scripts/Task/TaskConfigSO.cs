using System.Collections.Generic;
using UnityEngine;

namespace IdleGame.Task
{
    /// <summary>
    /// 任务配置 ScriptableObject
    /// 用于在 Unity 编辑器中配置任务数据
    /// </summary>
    [CreateAssetMenu(fileName = "TaskConfig", menuName = "IdleGame/任务配置", order = 1)]
    public class TaskConfigSO : ScriptableObject
    {
        /// <summary>
        /// 任务配置列表
        /// </summary>
        [Header("任务配置列表")]
        public List<TaskConfig> taskConfigs;

        /// <summary>
        /// 默认任务配置（当无法从 CSV 加载时使用）
        /// </summary>
        [Header("默认配置（备用）")]
        public List<TaskConfig> defaultConfigs;

        /// <summary>
        /// 获取所有任务配置
        /// </summary>
        public List<TaskConfig> GetAllConfigs()
        {
            return taskConfigs != null && taskConfigs.Count > 0 ? taskConfigs : defaultConfigs;
        }

        /// <summary>
        /// 根据任务ID获取配置
        /// </summary>
        public TaskConfig GetConfigById(int taskId)
        {
            if (taskConfigs == null) return null;

            foreach (var config in taskConfigs)
            {
                if (config.taskId == taskId)
                    return config;
            }

            return null;
        }

        /// <summary>
        /// 根据任务类型获取所有配置
        /// </summary>
        public List<TaskConfig> GetConfigsByType(TaskType type)
        {
            var result = new List<TaskConfig>();

            if (taskConfigs == null) return result;

            foreach (var config in taskConfigs)
            {
                if (config.taskType == type && config.isEnabled)
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 根据难度获取所有配置
        /// </summary>
        public List<TaskConfig> GetConfigsByDifficulty(TaskDifficulty difficulty)
        {
            var result = new List<TaskConfig>();

            if (taskConfigs == null) return result;

            foreach (var config in taskConfigs)
            {
                if (config.difficulty == difficulty && config.isEnabled)
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 获取已启用的所有任务
        /// </summary>
        public List<TaskConfig> GetEnabledConfigs()
        {
            var result = new List<TaskConfig>();

            if (taskConfigs == null) return result;

            foreach (var config in taskConfigs)
            {
                if (config.isEnabled)
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 获取可解锁的任务（根据玩家等级）
        /// </summary>
        public List<TaskConfig> GetUnlockableConfigs(int playerLevel)
        {
            var result = new List<TaskConfig>();

            if (taskConfigs == null) return result;

            foreach (var config in taskConfigs)
            {
                if (config.isEnabled && config.unlockLevel <= playerLevel)
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 验证配置完整性
        /// </summary>
        public bool ValidateConfigs()
        {
            if (taskConfigs == null || taskConfigs.Count == 0)
                return false;

            foreach (var config in taskConfigs)
            {
                if (string.IsNullOrEmpty(config.taskName))
                {
                    Debug.LogWarning($"[TaskConfigSO] 任务ID {config.taskId} 缺少任务名称");
                    return false;
                }

                if (config.targetCount <= 0)
                {
                    Debug.LogWarning($"[TaskConfigSO] 任务ID {config.taskId} 目标数量必须大于0");
                    return false;
                }

                if (config.awards == null || config.awards.Count == 0)
                {
                    Debug.LogWarning($"[TaskConfigSO] 任务ID {config.taskId} 没有配置奖励");
                    return false;
                }
            }

            return true;
        }
    }
}
