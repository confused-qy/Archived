using System.IO;
using UnityEngine;

namespace EmployeeHandbook.DailyTasks
{
    /// <summary>
    /// 负责把 PlayerState 保存到本机 JSON 文件。
    /// </summary>
    public static class SaveManager
    {
        private const string SaveFileName = "daily_task_save.json";

        private static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
        }

        public static void Save(PlayerState state)
        {
            if (state == null)
            {
                Debug.LogWarning("保存失败：PlayerState 为空。");
                return;
            }

            string json = JsonUtility.ToJson(state, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("保存成功：" + SavePath);
        }

        public static PlayerState Load()
        {
            if (!HasSave())
            {
                Debug.Log("没有找到存档，将返回空状态。路径：" + SavePath);
                return null;
            }

            string json = File.ReadAllText(SavePath);
            PlayerState state = JsonUtility.FromJson<PlayerState>(json);

            if (state == null)
            {
                Debug.LogWarning("读取存档失败：JSON 无法解析。路径：" + SavePath);
                return null;
            }

            if (state.completedTaskIds == null)
                state.completedTaskIds = new System.Collections.Generic.List<string>();

            Debug.Log("读取存档成功：第 " + state.currentDay + " 天，经验 " + state.experience);
            return state;
        }

        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public static void DeleteSave()
        {
            if (!HasSave())
            {
                Debug.Log("没有可删除的存档。路径：" + SavePath);
                return;
            }

            File.Delete(SavePath);
            Debug.Log("已删除存档：" + SavePath);
        }
    }
}
