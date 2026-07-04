using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeHandbook.OfficeGames
{
    public class WordFillQuestionView : MonoBehaviour
    {
        [Header("Prompt")]
        [SerializeField] private Text promptText;
        [SerializeField] private TMP_Text promptTmpText;
        [SerializeField] private bool usePlaceholderWhenPromptTextMissing = true;

        [Header("Input")]
        [SerializeField] private InputField answerInput;
        [SerializeField] private TMP_InputField answerTmpInput;

        private string[] acceptedAnswers = Array.Empty<string>();

        private void Awake()
        {
            AutoFindReferences();
        }

        public void Setup(WordFillQuestionData question)
        {
            AutoFindReferences();

            if (question == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            acceptedAnswers = question.answers ?? Array.Empty<string>();
            SetPrompt(question.prompt);
            SetAnswer(string.Empty);
        }

        public bool IsCorrect()
        {
            string normalizedInput = NormalizeAnswer(GetAnswer());
            if (string.IsNullOrEmpty(normalizedInput))
                return false;

            for (int i = 0; i < acceptedAnswers.Length; i++)
            {
                if (normalizedInput == NormalizeAnswer(acceptedAnswers[i]))
                    return true;
            }

            return false;
        }

        public void SetInteractable(bool interactable)
        {
            AutoFindReferences();

            if (answerInput != null)
                answerInput.interactable = interactable;

            if (answerTmpInput != null)
                answerTmpInput.interactable = interactable;
        }

        private void SetPrompt(string prompt)
        {
            bool hasPromptText = false;

            if (promptText != null)
            {
                promptText.text = prompt;
                hasPromptText = true;
            }

            if (promptTmpText != null)
            {
                promptTmpText.text = prompt;
                hasPromptText = true;
            }

            if (!hasPromptText && usePlaceholderWhenPromptTextMissing)
                SetPlaceholder(prompt);
        }

        private void SetPlaceholder(string prompt)
        {
            if (answerTmpInput != null && answerTmpInput.placeholder is TMP_Text tmpPlaceholder)
                tmpPlaceholder.text = prompt;

            if (answerInput != null && answerInput.placeholder is Text placeholder)
                placeholder.text = prompt;
        }

        private string GetAnswer()
        {
            if (answerTmpInput != null)
                return answerTmpInput.text;

            if (answerInput != null)
                return answerInput.text;

            return string.Empty;
        }

        private void SetAnswer(string value)
        {
            if (answerTmpInput != null)
                answerTmpInput.text = value;

            if (answerInput != null)
                answerInput.text = value;
        }

        private void AutoFindReferences()
        {
            if (answerTmpInput == null)
                answerTmpInput = GetComponentInChildren<TMP_InputField>(true);

            if (answerInput == null)
                answerInput = GetComponentInChildren<InputField>(true);
        }

        private static string NormalizeAnswer(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("　", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .ToLowerInvariant();
        }
    }
}
