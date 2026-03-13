using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadialMenu : MonoBehaviour {
    public static bool IsOpen { get; private set; }

    [Header("Layout")]
    [SerializeField] float radius = 120f;
    [SerializeField] float iconSize = 36f;

    Canvas canvas;
    RectTransform root;
    readonly List<Button> buttons = new List<Button>();
    Image background;

    struct ItemConfig {
        public ItemType type;
        public int stickLength;
    }

    enum ItemType {
        Ball,
        StickLength,
        Delete,
        NoneMode,
    }

    readonly List<ItemConfig> items = new List<ItemConfig>();

    void Awake() {
        EnsureEventSystem();
        CreateCanvas();
        BuildItemsFromGameState();
        BuildButtons();
        SetOpen(false);
    }

    void EnsureEventSystem() {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void CreateCanvas() {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject rootGo = new GameObject("RadialRoot");
        rootGo.transform.SetParent(canvas.transform, false);
        root = rootGo.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(radius * 2f + iconSize, radius * 2f + iconSize);

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(root, false);
        background = bgGo.AddComponent<Image>();
        background.sprite = GameManager.MakeCircleSprite();
        background.color = new Color(0f, 0f, 0f, 0.4f);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        float bgSize = radius * 2f + iconSize * 1.5f;
        bgRt.sizeDelta = new Vector2(bgSize, bgSize);
    }

    void BuildItemsFromGameState() {
        items.Clear();

        items.Add(new ItemConfig { type = ItemType.Ball, stickLength = 0 });

        List<int> lengths = new List<int>();
        if (GameManager.Instance != null && GameManager.Instance.availableStickLengths != null) {
            lengths.AddRange(GameManager.Instance.availableStickLengths);
        }

        if (lengths.Count == 0) {
            lengths.Add(2);
        }

        lengths.Sort();
        foreach (int len in lengths) {
            if (len <= 0) continue;
            items.Add(new ItemConfig { type = ItemType.StickLength, stickLength = len });
        }

        items.Add(new ItemConfig { type = ItemType.Delete, stickLength = 0 });
        items.Add(new ItemConfig { type = ItemType.NoneMode, stickLength = 0 });
    }

    void BuildButtons() {
        if (background != null)
            background.transform.SetAsFirstSibling();

        foreach (var btn in buttons) {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        buttons.Clear();

        if (items.Count == 0)
            return;

        float angleStep = 360f / items.Count;
        float startAngle = -90f;

        for (int i = 0; i < items.Count; i++) {
            ItemConfig config = items[i];
            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

            GameObject go = new GameObject($"RadialItem_{i}");
            go.transform.SetParent(root, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = pos;

            Image img = go.AddComponent<Image>();
            img.sprite = GetSpriteForItem(config);
            img.color = GetColorForItem(config);
            img.type = Image.Type.Sliced;

            Button btn = go.AddComponent<Button>();
            var localConfig = config;
            btn.onClick.AddListener(() => OnItemClicked(localConfig));

            buttons.Add(btn);
        }
    }

    Sprite GetSpriteForItem(ItemConfig config) {
        switch (config.type) {
            case ItemType.Ball:
                return GameManager.MakeCircleSprite();
            case ItemType.StickLength:
            case ItemType.Delete:
            case ItemType.NoneMode:
                return GameManager.MakeRectSprite();
            default:
                return GameManager.MakeRectSprite();
        }
    }

    Color GetColorForItem(ItemConfig config) {
        switch (config.type) {
            case ItemType.Ball:
                return new Color(0.8f, 0.4f, 0.4f, 0.9f);
            case ItemType.StickLength:
                return new Color(0.6f, 0.6f, 0.9f, 0.9f);
            case ItemType.Delete:
                return new Color(0.9f, 0.3f, 0.3f, 0.9f);
            case ItemType.NoneMode:
                return new Color(0.3f, 0.3f, 0.3f, 0.8f);
            default:
                return Color.white;
        }
    }

    void Update() {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
            return;

        if (Input.GetMouseButtonDown(2)) {
            SetOpen(!IsOpen);
        }
    }

    void OnItemClicked(ItemConfig config) {
        var im = InteractionManager.Instance;
        if (im == null) {
            SetOpen(false);
            return;
        }

        switch (config.type) {
            case ItemType.Ball:
                im.SetBallTool();
                break;
            case ItemType.StickLength:
                im.SetStickTool(config.stickLength);
                break;
            case ItemType.Delete:
                im.SetDeleteTool();
                break;
            case ItemType.NoneMode:
                im.SetSelectTool();
                break;
        }

        SetOpen(false);
    }

    void SetOpen(bool open) {
        IsOpen = open;
        if (root == null)
            return;

        if (open) {
            BuildItemsFromGameState();
            BuildButtons();
        }

        root.gameObject.SetActive(open);
    }
}
