using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单 UI：标题 / 开始游戏 / 教程 / 退出。
/// 挂载到任意 GameObject 即可，无需预设 Canvas。
/// </summary>
public class MainMenuUI : MonoBehaviour {
    [Header("Scene")]
    [SerializeField] string gameSceneName = "TestScene 1";

    [Header("Tutorial Sprite")]
    [SerializeField] Sprite teachingSprite;

    Canvas canvas;
    GameObject tutorialOverlay;

    void Awake() {
        EnsureEventSystem();
        canvas = BuildCanvas();
        BuildBackground();
        BuildTitle();
        BuildButtons();
        BuildTutorialOverlay();
    }

    void EnsureEventSystem() {
        if (FindObjectOfType<EventSystem>() == null) {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    Canvas BuildCanvas() {
        var c = gameObject.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
        return c;
    }

    // 深色背景遮罩
    void BuildBackground() {
        var go = new GameObject("Background");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.10f, 0.92f);
    }

    // 游戏标题
    void BuildTitle() {
        var go = new GameObject("Title");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.65f);
        rt.anchorMax = new Vector2(0.5f, 0.65f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600f, 100f);
        var txt = go.AddComponent<Text>();
        txt.text      = "034 Game Jam";
        txt.fontSize  = 54;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = new Color(0.95f, 0.92f, 0.85f, 1f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // 三个按钮
    void BuildButtons() {
        float[] yOffsets = { 0f, -70f, -140f };
        string[] labels  = { "开始游戏", "教程", "退出游戏" };

        for (int i = 0; i < 3; i++) {
            int idx = i;
            var btn = CreateButton(labels[i], new Vector2(0f, 0.42f),
                                   new Vector2(0f, yOffsets[i]));
            btn.onClick.AddListener(() => OnButtonClick(idx));
        }
    }

    Button CreateButton(string label, Vector2 anchor, Vector2 offset) {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(220f, 52f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        var cb  = new ColorBlock {
            normalColor      = new Color(0.18f, 0.18f, 0.22f),
            highlightedColor = new Color(0.28f, 0.28f, 0.36f),
            pressedColor     = new Color(0.12f, 0.12f, 0.16f),
            selectedColor    = new Color(0.18f, 0.18f, 0.22f),
            disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };
        btn.colors    = cb;
        btn.targetGraphic = img;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 22;
        txt.color     = new Color(0.92f, 0.92f, 0.92f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btn;
    }

    void OnButtonClick(int idx) {
        switch (idx) {
            case 0: SceneManager.LoadScene(gameSceneName); break;
            case 1: tutorialOverlay.SetActive(true);        break;
            case 2: Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;
        }
    }

    // 教程遮罩（点击关闭）
    void BuildTutorialOverlay() {
        tutorialOverlay = new GameObject("TutorialOverlay");
        tutorialOverlay.transform.SetParent(canvas.transform, false);
        var rt = tutorialOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // 半透明黑底
        var bg = tutorialOverlay.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);

        // 教程图片
        var imgGo = new GameObject("TeachingImage");
        imgGo.transform.SetParent(tutorialOverlay.transform, false);
        var imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.5f, 0.5f);
        imgRt.anchorMax = new Vector2(0.5f, 0.5f);
        imgRt.pivot     = new Vector2(0.5f, 0.5f);
        imgRt.anchoredPosition = Vector2.zero;
        imgRt.sizeDelta = new Vector2(800f, 500f);
        var teachImg = imgGo.AddComponent<Image>();
        if (teachingSprite != null)
            teachImg.sprite = teachingSprite;
        teachImg.preserveAspect = true;

        // 关闭按钮
        var closeBtn = CreateCloseButton(tutorialOverlay.transform);
        closeBtn.onClick.AddListener(() => tutorialOverlay.SetActive(false));

        tutorialOverlay.SetActive(false);
    }

    Button CreateCloseButton(Transform parent) {
        var go = new GameObject("CloseBtn");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(44f, 44f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.7f, 0.2f, 0.2f, 0.9f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtGo = new GameObject("X");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<Text>();
        txt.text      = "✕";
        txt.fontSize  = 22;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btn;
    }
}
