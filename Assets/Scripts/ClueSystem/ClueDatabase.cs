using System;
using UnityEngine;

namespace EmployeeHandbook.ClueSystem
{
    public static class ClueDatabase
    {
        public const string DefaultResourceName = "clues";

        public static ClueDatabaseData Load(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                resourceName = DefaultResourceName;

            TextAsset jsonAsset = Resources.Load<TextAsset>(resourceName);
            if (jsonAsset == null)
            {
                Debug.LogWarning("ClueDatabase 找不到 Resources/" + resourceName + ".json。");
                return new ClueDatabaseData();
            }

            try
            {
                ClueDatabaseData data = JsonUtility.FromJson<ClueDatabaseData>(jsonAsset.text);
                return data ?? new ClueDatabaseData();
            }
            catch (Exception exception)
            {
                Debug.LogError("ClueDatabase 解析失败：" + exception.Message);
                return new ClueDatabaseData();
            }
        }

        public static ClueDefinition FindById(ClueDatabaseData data, int clueId)
        {
            if (data == null || data.clues == null)
                return null;

            for (int i = 0; i < data.clues.Length; i++)
            {
                ClueDefinition clue = data.clues[i];
                if (clue != null && clue.clueId == clueId)
                    return clue;
            }

            return null;
        }

        public static ClueDefinition FindByQuery(ClueDatabaseData data, string query)
        {
            if (data == null || data.clues == null || string.IsNullOrWhiteSpace(query))
                return null;

            string normalizedQuery = Normalize(query);
            for (int i = 0; i < data.clues.Length; i++)
            {
                ClueDefinition clue = data.clues[i];
                if (clue != null && clue.MatchesQuery(normalizedQuery))
                    return clue;
            }

            return null;
        }

        public static string Normalize(string value)
        {
            return value != null ? value.Trim() : string.Empty;
        }
    }

    [Serializable]
    public class ClueDatabaseData
    {
        public ClueDefinition[] clues;
    }

    [Serializable]
    public class ClueDefinition
    {
        public int clueId;
        public string name;
        public string description;
        public bool searchable;
        public int unlockClueIdOnSearch;
        public string[] aliases;

        public bool MatchesQuery(string normalizedQuery)
        {
            if (string.Equals(ClueDatabase.Normalize(name), normalizedQuery, StringComparison.OrdinalIgnoreCase))
                return true;

            if (aliases == null)
                return false;

            for (int i = 0; i < aliases.Length; i++)
            {
                if (string.Equals(ClueDatabase.Normalize(aliases[i]), normalizedQuery, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
