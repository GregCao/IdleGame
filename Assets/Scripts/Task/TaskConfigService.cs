using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IdleGame.Task
{
    /// <summary>
    /// 任务配置服务
    /// 负责从 Resources/CSV 或 ScriptableObject 加载任务配置
    /// 提供任务解锁判断和配置缓存
    /// </summary>
    public class TaskConfigService : Singleton<TaskConfigService>
    {
        /// <summary>CSV 配置文件路径（相对于 Resources）</summary>
        private const string CSV_PATH = "Config/TaskConfig";

        /// <summary>ScriptableObject 配置路径（相对于 Resources）</summary>
        private const string SO_PATH = "Task/TaskConfigSO";

        /// <summary>缓存的任务配置表</summary>
        private Dictionary<int, TaskConfig> _taskConfigCache;

        /// <summary>ScriptableObject 配置数据</summary>
        private TaskConfigSO _taskConfigSO;

        /// <summary>是否已初始化</summary>
        private bool _isInitialized = false;

        /// <summary>
        /// 初始化配置服务
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            LoadAllConfigs();
            _isInitialized = true;
        }

        /// <summary>
        /// 加载所有任务配置
        /// 优先从 ScriptableObject 加载，回退到 CSV
        /// </summary>
        private void LoadAllConfigs()
        {
            // 优先尝试从 ScriptableObject 加载
            _taskConfigSO = Resources.Load<TaskConfigSO>(SO_PATH);

            if (_taskConfigSO != null && _taskConfigSO.taskConfigs != null && _taskConfigSO.taskConfigs.Count > 0)
            {
                LoadFromScriptableObject();
                Debug.Log($"[TaskConfigService] 从 ScriptableObject 加载了 {_taskConfigCache.Count} 个任务配置");
            }
            else
            {
                // 回退到 CSV 加载
                LoadFromCSV();
                Debug.Log($"[TaskConfigService] 从 CSV 加载了 {_taskConfigCache.Count} 个任务配置");
            }
        }

        /// <summary>
        /// 从 ScriptableObject 加载配置
        /// </summary>
        private void LoadFromScriptableObject()
        {
            _taskConfigCache = new Dictionary<int, TaskConfig>();

            foreach (var config in _taskConfigSO.taskConfigs)
            {
                if (config.isEnabled)
                {
                    _taskConfigCache[config.taskId] = config;
                }
            }
        }

        /// <summary>
        /// 从 CSV 文件加载配置
        /// </summary>
        private void LoadFromCSV()
        {
            _taskConfigCache = new Dictionary<int, TaskConfig>();

            TextAsset csvFile = Resources.Load<TextAsset>(CSV_PATH);

            if (csvFile == null)
            {
                Debug.LogWarning($"[TaskConfigService] CSV 配置文件不存在: {CSV_PATH}，使用默认配置");
                LoadDefaultConfigs();
                return;
            }

            try
            {
                ParseCSV(csvFile.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TaskConfigService] 解析 CSV 失败: {e.Message}");
                LoadDefaultConfigs();
            }
        }

        /// <summary>
        /// 解析 CSV 内容
        /// CSV 格式：taskId,taskName,taskType,difficulty,targetCount,description,icon,refreshCycle,unlockLevel,awards
        /// awards 格式：type1,count1,weight1|type2,count2,weight2
        /// </summary>
        private void ParseCSV(string csvContent)
        {
            string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= 1)
            {
                Debug.LogWarning("[TaskConfigService] CSV 文件为空或只有表头");
                return;
            }

            // 跳过表头
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrEmpty(line))
                    continue;

                TaskConfig config = ParseCSVLine(lines[i]);

                if (config != null && config.isEnabled)
                {
                    _taskConfigCache[config.taskId] = config;
                }
            }
        }

        /// <summary>
        /// 解析单行 CSV 数据
        /// </summary>
        private TaskConfig ParseCSVLine(string line)
        {
            string[] fields = ParseCSVFields(line);

            if (fields.Length < 9)
            {
                Debug.LogWarning($"[TaskConfigService] CSV 字段数不足: {line}");
                return null;
            }

            TaskConfig config = new TaskConfig();

            try
            {
                config.taskId = int.Parse(fields[0]);
                config.taskName = fields[1];
                config.taskType = (TaskType)Enum.Parse(typeof(TaskType), fields[2]);
                config.difficulty = (TaskDifficulty)Enum.Parse(typeof(TaskDifficulty), fields[3]);
                config.targetCount = int.Parse(fields[4]);
                config.description = fields[5];
                config.icon = fields[6];
                config.refreshCycle = fields[7];
                config.unlockLevel = int.Parse(fields[8]);
                config.isEnabled = true;

                // 解析奖励
                config.awards = new List<AwardConfig>();

                if (fields.Length > 9 && !string.IsNullOrEmpty(fields[9]))
                {
                    string[] awards = fields[9].Split('|');

                    foreach (string awardStr in awards)
                    {
                        string[] awardFields = awardStr.Split(',');

                        if (awardFields.Length >= 2)
                        {
                            AwardConfig award = new AwardConfig
                            {
                                awardType = awardFields[0],
                                itemId = int.Parse(awardFields[1]),
                                count = int.Parse(awardFields[2]),
                                weight = awardFields.Length > 3 ? int.Parse(awardFields[3]) : 1
                            };

                            config.awards.Add(award);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TaskConfigService] 解析 CSV 行失败: {e.Message}, 行: {line}");
                return null;
            }

            return config;
        }

        /// <summary>
        /// 解析 CSV 字段（处理引号）
        /// </summary>
        private string[] ParseCSVFields(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            fields.Add(currentField.Trim());
            return fields.ToArray();
        }

        /// <summary>
        /// 加载默认配置
        /// </summary>
        private void LoadDefaultConfigs()
        {
            if (_taskConfigSO != null && _taskConfigSO.defaultConfigs != null)
            {
                _taskConfigCache = new Dictionary<int, TaskConfig>();

                foreach (var config in _taskConfigSO.defaultConfigs)
                {
                    if (config.isEnabled)
                    {
                        _taskConfigCache[config.taskId] = config;
                    }
                }
            }
            else
            {
                // 硬编码默认配置作为最终回退
                _taskConfigCache = GetBuiltInDefaultConfigs();
            }
        }

        /// <summary>
        /// 内置默认配置
        /// </summary>
        private Dictionary<int, TaskConfig> GetBuiltInDefaultConfigs()
        {
            return new Dictionary<int, TaskConfig>
            {
                {
                    1001, new TaskConfig
                    {
                        taskId = 1001,
                        taskName = "初出茅庐",
                        taskType = TaskType.KillMonster,
                        difficulty = TaskDifficulty.Easy,
                        targetCount = 10,
                        description = "击杀10只怪物",
                        icon = "task_kill",
                        refreshCycle = TaskRefreshCycle.Daily,
                        unlockLevel = 1,
                        isEnabled = true,
                        awards = new List<AwardConfig>
                        {
                            new AwardConfig { awardType = "Gold", itemId = 0, count = 100 }
                        }
                    }
                },
                {
                    1002, new TaskConfig
                    {
                        taskId = 1002,
                        taskName = "江湖新秀",
                        taskType = TaskType.KillMonster,
                        difficulty = TaskDifficulty.Normal,
                        targetCount = 50,
                        description = "击杀50只怪物",
                        icon = "task_kill",
                        refreshCycle = TaskRefreshCycle.Daily,
                        unlockLevel = 5,
                        isEnabled = true,
                        awards = new List<AwardConfig>
                        {
                            new AwardConfig { awardType = "Gold", itemId = 0, count = 500 }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 根据任务ID获取配置
        /// </summary>
        public TaskConfig GetTaskConfig(int taskId)
        {
            if (!_isInitialized)
                Initialize();

            return _taskConfigCache.ContainsKey(taskId) ? _taskConfigCache[taskId] : null;
        }

        /// <summary>
        /// 获取所有已缓存的任务配置
        /// </summary>
        public List<TaskConfig> GetAllTaskConfigs()
        {
            if (!_isInitialized)
                Initialize();

            return new List<TaskConfig>(_taskConfigCache.Values);
        }

        /// <summary>
        /// 根据类型获取任务配置列表
        /// </summary>
        public List<TaskConfig> GetTaskConfigsByType(TaskType type)
        {
            if (!_isInitialized)
                Initialize();

            List<TaskConfig> result = new List<TaskConfig>();

            foreach (var config in _taskConfigCache.Values)
            {
                if (config.taskType == type)
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 根据难度获取任务配置列表
        /// </summary>
        public List<TaskConfig> GetTaskConfigsByDifficulty(TaskDifficulty difficulty)
        {
            if (!_isInitialized)
                Initialize();

            List<TaskConfig> result = new List<TaskConfig>();

            foreach (var config in _taskConfigCache.Values)
            {
                if (config.difficulty == difficulty)
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 根据刷新周期获取任务配置列表
        /// </summary>
        public List<TaskConfig> GetTaskConfigsByRefreshCycle(string refreshCycle)
        {
            if (!_isInitialized)
                Initialize();

            List<TaskConfig> result = new List<TaskConfig>();

            foreach (var config in _taskConfigCache.Values)
            {
                if (config.refreshCycle == refreshCycle)
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 检查任务是否可解锁
        /// </summary>
        public bool IsTaskUnlockable(int taskId, int playerLevel)
        {
            TaskConfig config = GetTaskConfig(taskId);

            if (config == null)
                return false;

            return config.isEnabled && config.unlockLevel <= playerLevel;
        }

        /// <summary>
        /// 检查任务是否可解锁（通过配置）
        /// </summary>
        public bool IsTaskUnlockable(TaskConfig config, int playerLevel)
        {
            if (config == null)
                return false;

            return config.isEnabled && config.unlockLevel <= playerLevel;
        }

        /// <summary>
        /// 获取指定玩家等级可解锁的所有任务
        /// </summary>
        public List<TaskConfig> GetUnlockableTasks(int playerLevel)
        {
            if (!_isInitialized)
                Initialize();

            List<TaskConfig> result = new List<TaskConfig>();

            foreach (var config in _taskConfigCache.Values)
            {
                if (IsTaskUnlockable(config, playerLevel))
                    result.Add(config);
            }

            return result;
        }

        /// <summary>
        /// 获取已缓存配置数量
        /// </summary>
        public int GetCachedConfigCount()
        {
            return _taskConfigCache != null ? _taskConfigCache.Count : 0;
        }

        /// <summary>
        /// 重新加载配置（用于配置热更新）
        /// </summary>
        public void ReloadConfigs()
        {
            _isInitialized = false;
            _taskConfigCache?.Clear();
            Resources.UnloadAsset(_taskConfigSO);
            _taskConfigSO = null;
            Initialize();
            Debug.Log("[TaskConfigService] 配置已重新加载");
        }

        /// <summary>
        /// 导出配置到 CSV（用于调试）
        /// </summary>
        public string ExportToCSV()
        {
            List<TaskConfig> configs = GetAllTaskConfigs();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("taskId,taskName,taskType,difficulty,targetCount,description,icon,refreshCycle,unlockLevel,awards");

            foreach (var config in configs)
            {
                string awardsStr = "";

                if (config.awards != null && config.awards.Count > 0)
                {
                    List<string> awardStrs = new List<string>();

                    foreach (var award in config.awards)
                    {
                        awardStrs.Add($"{award.awardType},{award.itemId},{award.count},{award.weight}");
                    }

                    awardsStr = string.Join("|", awardStrs);
                }

                sb.AppendLine($"{config.taskId},{config.taskName},{config.taskType},{config.difficulty},{config.targetCount},{config.description},{config.icon},{config.refreshCycle},{config.unlockLevel},{awardsStr}");
            }

            return sb.ToString();
        }
    }
}
