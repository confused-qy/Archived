using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmployeeHandbook
{
    /// <summary>Owns the small amount of persistent state used by the prototype.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int currentStage = 1;
        [SerializeField] private int compliance;
        [SerializeField] private int autonomy;
        [SerializeField] private int completedTaskCount;
        [SerializeField] private List<string> discoveredClues = new List<string>();

        public int CurrentStage => currentStage;
        public int Compliance => compliance;
        public int Autonomy => autonomy;
        public int CompletedTaskCount => completedTaskCount;
        public IReadOnlyList<string> DiscoveredClues => discoveredClues;

        public event Action StateChanged;
        public event Action<string> ClueDiscovered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void AddCompliance(int amount)
        {
            compliance += amount;
            StateChanged?.Invoke();
        }

        public void AddAutonomy(int amount)
        {
            autonomy += amount;
            StateChanged?.Invoke();
        }

        public void CompleteTask()
        {
            completedTaskCount++;
            StateChanged?.Invoke();
        }

        public void UnlockClue(string clue)
        {
            if (string.IsNullOrWhiteSpace(clue) || discoveredClues.Contains(clue))
                return;

            discoveredClues.Add(clue);
            ClueDiscovered?.Invoke(clue);
            StateChanged?.Invoke();
        }

        public void SetStage(int stage)
        {
            currentStage = Mathf.Max(1, stage);
            StateChanged?.Invoke();
        }

        /// <summary>Useful when replaying the scene in the editor.</summary>
        public void ResetProgress()
        {
            currentStage = 1;
            compliance = 0;
            autonomy = 0;
            completedTaskCount = 0;
            discoveredClues.Clear();
            StateChanged?.Invoke();
        }
    }
}
