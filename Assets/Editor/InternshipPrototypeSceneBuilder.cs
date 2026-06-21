#if UNITY_EDITOR
using System.Collections.Generic;
using EmployeeHandbook;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class InternshipPrototypeSceneBuilder
{
    private const string DataFolder = "Assets/Data";
    private const string TaskFolder = "Assets/Data/Stage1Tasks";
    private const string ScenePath = "Assets/Scenes/InternshipPrototype.unity";

    private static readonly Color Background = Hex("17202A");
    private static readonly Color Panel = Hex("202D3A");
    private static readonly Color PanelLight = Hex("2B3C4D");
    private static readonly Color Accent = Hex("4DA3FF");
    private static readonly Color Suspicious = Hex("C44B62");
    private static readonly Color TextColor = Hex("E8EEF4");
    private static Font font;

    [MenuItem("Tools/Employee Handbook/Build Stage 1 Prototype")]
    public static void Build()
    {
        EnsureFolders();
        List<WorkTaskData> normalTasks = CreateTaskAssets();
        WorkTaskData suspiciousTask = AssetDatabase.LoadAssetAtPath<WorkTaskData>(TaskFolder + "/SuspiciousEmail.asset");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject systems = new GameObject("Game Systems");
        systems.AddComponent<GameManager>();
        TaskSystem taskSystem = systems.AddComponent<TaskSystem>();
        SetObjectList(taskSystem, "stageOneTasks", normalTasks);
        SetObject(taskSystem, "suspiciousEmailTask", suspiciousTask);

        CreateEventSystem();
        Canvas canvas = CreateCanvas();
        RectTransform desktop = CreatePanel("Desktop", canvas.transform, Background, Vector2.zero, Vector2.one);

        CreateSidebar(desktop);
        RectTransform workArea = CreatePanel("Main Work Area", desktop, Panel, new Vector2(.16f, .08f), new Vector2(.76f, 1f));
        RectTransform clueArea = CreatePanel("Discovered Clues", desktop, PanelLight, new Vector2(.76f, .08f), Vector2.one);
        RectTransform statusBar = CreatePanel("Status Bar", desktop, Hex("10171F"), Vector2.zero, new Vector2(1f, .08f));

        Text taskTitle = CreateText("Current Task Title", workArea, "Current Task", 30, FontStyle.Bold,
            new Vector2(.06f, .75f), new Vector2(.94f, .94f), TextAnchor.MiddleLeft);
        Text taskDescription = CreateText("Current Task Description", workArea, "Task description", 21, FontStyle.Normal,
            new Vector2(.06f, .48f), new Vector2(.94f, .76f), TextAnchor.UpperLeft);

        WarningDialog warning = CreateWarningDialog(desktop);
        ButtonInteraction mainButton = CreateActionButton("Main Action Button", workArea, "Complete", Accent,
            new Vector2(.06f, .31f), new Vector2(.45f, .43f), warning);

        RectTransform suspiciousPanel = CreatePanel("Hidden Email", workArea, Hex("3A2630"),
            new Vector2(.51f, .11f), new Vector2(.94f, .43f));
        CreateText("Hidden Email Header", suspiciousPanel, "UNLISTED MESSAGE", 18, FontStyle.Bold,
            new Vector2(.06f, .68f), new Vector2(.94f, .94f), TextAnchor.MiddleLeft).color = Hex("FF8FA3");
        CreateText("Hidden Email Preview", suspiciousPanel, "Sender: archive@internal\nSubject: C-017 recovery fragment", 16,
            FontStyle.Normal, new Vector2(.06f, .34f), new Vector2(.94f, .7f), TextAnchor.UpperLeft);
        ButtonInteraction suspiciousButton = CreateActionButton("Abnormal Button", suspiciousPanel, "OPEN ANYWAY", Suspicious,
            new Vector2(.06f, .07f), new Vector2(.7f, .33f), warning);

        CreateText("Clues Header", clueArea, "DISCOVERED CLUES", 21, FontStyle.Bold,
            new Vector2(.08f, .86f), new Vector2(.92f, .97f), TextAnchor.MiddleLeft);
        Text clueText = CreateText("Clue List", clueArea, "No fragments recovered.", 17, FontStyle.Normal,
            new Vector2(.08f, .1f), new Vector2(.92f, .86f), TextAnchor.UpperLeft);
        CluePanelUI cluePanel = clueArea.gameObject.AddComponent<CluePanelUI>();
        SetObject(cluePanel, "clueText", clueText);

        Text compliance = CreateText("Compliance", statusBar, "Compliance: 0", 18, FontStyle.Normal,
            new Vector2(.02f, .1f), new Vector2(.21f, .9f), TextAnchor.MiddleLeft);
        Text autonomy = CreateText("Autonomy", statusBar, "Autonomy: 0", 18, FontStyle.Normal,
            new Vector2(.23f, .1f), new Vector2(.42f, .9f), TextAnchor.MiddleLeft);
        Text stage = CreateText("Stage", statusBar, "Stage 1: Internship Trial Period", 18, FontStyle.Bold,
            new Vector2(.55f, .1f), new Vector2(.98f, .9f), TextAnchor.MiddleRight);

        PrototypeUIController controller = desktop.gameObject.AddComponent<PrototypeUIController>();
        SetObject(controller, "taskSystem", taskSystem);
        SetObject(controller, "warningDialog", warning);
        SetObject(controller, "taskTitleText", taskTitle);
        SetObject(controller, "taskDescriptionText", taskDescription);
        SetObject(controller, "mainActionButton", mainButton);
        SetObject(controller, "suspiciousEmailPanel", suspiciousPanel.gameObject);
        SetObject(controller, "suspiciousActionButton", suspiciousButton);
        SetObject(controller, "complianceText", compliance);
        SetObject(controller, "autonomyText", autonomy);
        SetObject(controller, "stageText", stage);

        // Keep the modal above every desktop panel in the UGUI draw order.
        warning.transform.SetAsLastSibling();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Selection.activeGameObject = desktop.gameObject;
        Debug.Log("Stage 1 prototype created at " + ScenePath);
    }

    private static void CreateSidebar(RectTransform parent)
    {
        RectTransform sidebar = CreatePanel("Sidebar", parent, Hex("111A23"), new Vector2(0, .08f), new Vector2(.16f, 1));
        CreateText("Company", sidebar, "NULLPOINT\nINTERN PORTAL", 22, FontStyle.Bold,
            new Vector2(.1f, .82f), new Vector2(.9f, .97f), TextAnchor.MiddleLeft);
        string[] labels = { "MAIL", "FILES", "CALENDAR", "TASKS" };
        for (int i = 0; i < labels.Length; i++)
        {
            float top = .76f - i * .12f;
            CreateButton(labels[i], sidebar, labels[i], PanelLight, new Vector2(.1f, top - .075f), new Vector2(.9f, top));
        }
    }

    private static WarningDialog CreateWarningDialog(RectTransform parent)
    {
        RectTransform overlay = CreatePanel("Warning Dialog", parent, new Color(0, 0, 0, .78f), Vector2.zero, Vector2.one);
        RectTransform box = CreatePanel("Dialog Box", overlay, Hex("283542"), new Vector2(.29f, .3f), new Vector2(.71f, .7f));
        Text title = CreateText("Title", box, "Warning", 28, FontStyle.Bold,
            new Vector2(.08f, .72f), new Vector2(.92f, .92f), TextAnchor.MiddleLeft);
        Text message = CreateText("Message", box, "Warning message", 19, FontStyle.Normal,
            new Vector2(.08f, .33f), new Vector2(.92f, .72f), TextAnchor.UpperLeft);
        Button continueButton = CreateButton("Continue Button", box, "CONTINUE", Suspicious,
            new Vector2(.52f, .1f), new Vector2(.9f, .26f));
        Button returnButton = CreateButton("Return Button", box, "RETURN", PanelLight,
            new Vector2(.1f, .1f), new Vector2(.48f, .26f));

        WarningDialog dialog = overlay.gameObject.AddComponent<WarningDialog>();
        SetObject(dialog, "titleText", title);
        SetObject(dialog, "messageText", message);
        SetObject(dialog, "continueButton", continueButton);
        SetObject(dialog, "returnButton", returnButton);
        overlay.SetAsLastSibling();
        return dialog;
    }

    private static ButtonInteraction CreateActionButton(string name, Transform parent, string label, Color color,
        Vector2 min, Vector2 max, WarningDialog warning)
    {
        Button button = CreateButton(name, parent, label, color, min, max);
        ButtonInteraction interaction = button.gameObject.AddComponent<ButtonInteraction>();
        SetObject(interaction, "button", button);
        SetObject(interaction, "buttonLabel", button.GetComponentInChildren<Text>());
        SetObject(interaction, "warningDialog", warning);
        return interaction;
    }

    private static List<WorkTaskData> CreateTaskAssets()
    {
        var tasks = new List<WorkTaskData>
        {
            CreateTask("01_ReviewOnboarding", "Review onboarding email",
                "Read the approved onboarding message from Human Resources.", "MARK AS READ", 1),
            CreateTask("02_DownloadAttachment", "Download attachment",
                "Download the workplace conduct checklist attached to your inbox.", "DOWNLOAD", 1),
            CreateTask("03_ArchiveEmployeeFile", "Archive employee file",
                "Move the completed employee record into the approved archive.", "ARCHIVE FILE", 1),
            CreateTask("04_SubmitDailyReport", "Submit daily report",
                "Send today's activity summary to your assigned supervisor.", "SUBMIT REPORT", 1),
            CreateTask("05_ConfirmCalendar", "Confirm review calendar",
                "Acknowledge the scheduled end-of-trial evaluation meeting.", "CONFIRM", 1)
        };

        WorkTaskData suspicious = CreateTask("SuspiciousEmail", "Hidden archive email",
            "An unlisted message references a deleted recovery record.", "OPEN MESSAGE", 0, 1,
            "Recovered fragment: Sample ID C-017. Current cycle: 17.");
        suspicious.requiresWarning = true;
        suspicious.warningTitle = "Work Scope Warning";
        suspicious.warningMessage =
            "This email is outside your assigned work scope. Viewing it may affect your internship evaluation. Continue?";
        EditorUtility.SetDirty(suspicious);
        AssetDatabase.SaveAssets();
        return tasks;
    }

    private static WorkTaskData CreateTask(string fileName, string title, string description, string buttonText,
        int compliance, int autonomy = 0, string clue = "")
    {
        string path = TaskFolder + "/" + fileName + ".asset";
        WorkTaskData task = AssetDatabase.LoadAssetAtPath<WorkTaskData>(path);
        if (task == null)
        {
            task = ScriptableObject.CreateInstance<WorkTaskData>();
            AssetDatabase.CreateAsset(task, path);
        }

        task.title = title;
        task.description = description;
        task.buttonText = buttonText;
        task.complianceChange = compliance;
        task.autonomyChange = autonomy;
        task.optionalClueUnlock = clue;
        task.requiresWarning = false;
        task.warningTitle = "Work Scope Warning";
        task.warningMessage = string.Empty;
        EditorUtility.SetDirty(task);
        return task;
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Desktop Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = .5f;
        return canvas;
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect, min, max);
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style,
        Vector2 min, Vector2 max, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect, min, max, new Vector2(8, 4), new Vector2(-8, -4));
        Text text = go.GetComponent<Text>();
        text.font = GetFont();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = TextColor;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect, min, max);
        Image image = go.GetComponent<Image>();
        image.color = color;
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, .15f);
        colors.pressedColor = Color.Lerp(color, Color.black, .2f);
        button.colors = colors;
        CreateText("Label", go.transform, label, 17, FontStyle.Bold, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
        return button;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin = default,
        Vector2 offsetMax = default)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Font GetFont()
    {
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectList(Object target, string propertyName, List<WorkTaskData> values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty list = serialized.FindProperty(propertyName);
        list.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(TaskFolder))
            AssetDatabase.CreateFolder(DataFolder, "Stage1Tasks");
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(item => item.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
