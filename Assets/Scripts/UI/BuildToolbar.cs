using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 建造工具栏：屏幕下方显示球与三种长度的棒子数量，并提供工具切换。
/// - 左侧：球按钮（切换到球工具）
/// - 右侧：三种预设长度的棒按钮（切换到对应长度的棒工具）
/// 数量显示来自 Backpack（若存在），否则退回 GameManager 的基础字段。
/// </summary>
public class BuildToolbar : MonoBehaviour {
    [Header("Stick Length Presets")]
    [SerializeField] int[] stickLengths = new int[] { 2, 4, 6 };

    Canvas canvas;
    RectTransform root;

    Button selectButton;
    Button ballButton;
    Text ballCountText;

    readonly List<Button> stickButtons = new List<Button>();
    readonly List<Text> stickCountTexts = new List<Text>();

    Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    Color selectedColor = new Color(0.4f, 0.4f, 0.8f, 0.9f);

    void Awake() {
        EnsureEventSystem();
        CreateCanvas();

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 18);

        // 底部容器
        GameObject rootGo = new GameObject("BuildToolbarRoot");
        rootGo.transform.SetParent(canvas.transform, false);
        root = rootGo.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0);
        root.anchorMax = new Vector2(0.5f, 0);
        root.pivot = new Vector2(0.5f, 0);
        root.anchoredPosition = new Vector2(0, 10);
        root.sizeDelta = new Vector2(600, 60);

        HorizontalLayoutGroup hlg = rootGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.LowerCenter;
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(8, 8, 4, 4);

        // 选择按钮
        CreateSelectButton(font);

        // 球按钮
        CreateBallButton(font);

        // 棒按钮
        for (int i = 0; i < stickLengths.Length; i++) {
            CreateStickButton(font, stickLengths[i]);
        }

        // 默认选中第一个棒长度
        if (stickButtons.Count > 0) {
            OnStickButtonClicked(0);
        }
    }

    void EnsureEventSystem() {
        if (FindObjectOfType<EventSystem>() == null) {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    void CreateCanvas() {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
    }

    void CreateSelectButton(Font font) {
        GameObject go = new GameObject("SelectButton");
        go.transform.SetParent(root, false);

        Image img = go.AddComponent<Image>();
        img.color = normalColor;

        selectButton = go.AddComponent<Button>();
        selectButton.onClick.AddListener(OnSelectButtonClicked);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120, 40);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        Text txt = labelGo.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "选中/X删除";

        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    void CreateBallButton(Font font) {
        GameObject go = new GameObject("BallButton");
        go.transform.SetParent(root, false);

        Image img = go.AddComponent<Image>();
        img.color = normalColor;

        ballButton = go.AddComponent<Button>();
        ballButton.onClick.AddListener(OnBallButtonClicked);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120, 40);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        Text txt = labelGo.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = "球: 0";

        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        ballCountText = txt;
    }

    void CreateStickButton(Font font, int length) {
        GameObject go = new GameObject($"StickButton_L{length}");
        go.transform.SetParent(root, false);

        Image img = go.AddComponent<Image>();
        img.color = normalColor;

        Button btn = go.AddComponent<Button>();
        int index = stickButtons.Count;
        btn.onClick.AddListener(() => OnStickButtonClicked(index));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140, 40);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        Text txt = labelGo.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = $"L={length}: 0";

        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        stickButtons.Add(btn);
        stickCountTexts.Add(txt);
    }

    void OnBallButtonClicked() {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.SetBallTool();
        UpdateButtonVisuals(ballSelected: true, selectedStickIndex: -1);
    }

    void OnStickButtonClicked(int index) {
        if (index < 0 || index >= stickLengths.Length) return;
        int length = stickLengths[index];
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.SetStickTool(length);
        UpdateButtonVisuals(ballSelected: false, selectedStickIndex: index);
    }

    void UpdateButtonVisuals(bool ballSelected, int selectedStickIndex) {
        if (selectButton != null) {
            var imgSel = selectButton.GetComponent<Image>();
            if (imgSel) imgSel.color = normalColor;
        }

        if (ballButton != null) {
            var img = ballButton.GetComponent<Image>();
            if (img) img.color = ballSelected ? selectedColor : normalColor;
        }

        for (int i = 0; i < stickButtons.Count; i++) {
            var img = stickButtons[i].GetComponent<Image>();
            if (img) img.color = (i == selectedStickIndex) ? selectedColor : normalColor;
        }
    }

    void OnSelectButtonClicked() {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.SetSelectTool();

        if (selectButton != null) {
            var imgSel = selectButton.GetComponent<Image>();
            if (imgSel) imgSel.color = selectedColor;
        }

        if (ballButton != null) {
            var img = ballButton.GetComponent<Image>();
            if (img) img.color = normalColor;
        }

        for (int i = 0; i < stickButtons.Count; i++) {
            var img = stickButtons[i].GetComponent<Image>();
            if (img) img.color = normalColor;
        }
    }

    void Update() {
        UpdateCounts();
    }

    void UpdateCounts() {
        var gm = GameManager.Instance;
        var backpack = Backpack.Instance;

        int ballCount = 0;
        if (backpack != null)
            ballCount = backpack.BallCount;
        else if (gm != null)
            ballCount = gm.allocatableBallCount;

        if (ballCountText != null)
            ballCountText.text = $"球: {ballCount}";

        for (int i = 0; i < stickLengths.Length && i < stickCountTexts.Count; i++) {
            int length = stickLengths[i];
            int count = 0;
            if (backpack != null)
                count = backpack.GetStickCount(length);
            else if (gm != null)
                count = gm.stickCount; // 无 Backpack 情况下只显示总数

            stickCountTexts[i].text = $"L={length}: {count}";
        }
    }
}
