using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildToolbar : MonoBehaviour {
    [Header("Stick Length Presets")]
    [SerializeField] int[] stickLengths = new int[] { 2, 4, 6 };

    [Header("Inventory Limit Per Item")]
    [SerializeField] int maxPerItem = 256;

    [Header("Layout (可运行前在 Inspector 调节)")]
    [Tooltip("工具栏相对屏幕底部中央的像素偏移")]
    [SerializeField] Vector2 toolbarPosition = new Vector2(0f, 14f);
    [Tooltip("各槽位之间的水平间距")]
    [SerializeField] float itemSpacing = 20f;
    [Tooltip("槽位宽度")]
    [SerializeField] float slotWidth = 88f;

    Canvas canvas;
    RectTransform root;

    Image ballFillImage;
    readonly List<Image> stickFillImages = new List<Image>();

    // 连接棒图标颜色 (148, 150, 148)
    static readonly Color StickIconColor = new Color(148 / 255f, 150 / 255f, 148 / 255f, 1f);
    static readonly Color BallIconColor  = new Color(0.9f, 0.5f, 0.5f, 0.9f);

    // 每个槽位尺寸（SlotW 由 slotWidth 字段控制）
    const float SlotH  = 76f;
    // 图标区域（顶部）
    const float IconSize   = 24f;   // 圆形球图标边长
    const float StickIconW = 6f;    // 棒图标宽（细矩形）
    const float StickIconH = 30f;   // 棒图标高
    const float IconOffsetFromTop = 10f;
    // 进度条（底部）
    const float BarW = 68f;
    const float BarH = 10f;
    const float BarOffsetFromBottom = 10f;

    void Awake() {
        EnsureEventSystem();
        CreateCanvas();
        CreateRootAndSlots();
    }

    void EnsureEventSystem() {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void CreateCanvas() {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
    }

    void CreateRootAndSlots() {
        GameObject rootGo = new GameObject("BuildToolbarRoot");
        rootGo.transform.SetParent(canvas.transform, false);
        root = rootGo.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot     = new Vector2(0.5f, 0f);
        root.anchoredPosition = toolbarPosition;
        root.sizeDelta = new Vector2(500f, SlotH);

        HorizontalLayoutGroup hlg = rootGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing  = itemSpacing;
        hlg.padding  = new RectOffset(16, 16, 0, 0);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        CreateBallSlot();
        for (int i = 0; i < stickLengths.Length; i++)
            CreateStickSlot(stickLengths[i]);
    }

    // ── 通用槽位容器 ──────────────────────────────────────────────────
    RectTransform CreateSlotContainer(string name) {
        GameObject slotGo = new GameObject(name);
        slotGo.transform.SetParent(root, false);
        RectTransform rt = slotGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(slotWidth, SlotH);
        return rt;
    }

    // 在槽位顶部居中放置图标（icon 用 top-center 锚）
    RectTransform CreateIconRect(Transform parent, string name, float w, float h) {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -IconOffsetFromTop);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    // 在槽位底部居中放置进度条背景（bottom-center 锚）
    Image CreateBar(Transform parent, Color fillColor, out Image fill) {
        // 背景
        GameObject bgGo = new GameObject("BarBG");
        bgGo.transform.SetParent(parent, false);
        Image barBg = bgGo.AddComponent<Image>();
        barBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0f);
        bgRt.anchorMax = new Vector2(0.5f, 0f);
        bgRt.pivot     = new Vector2(0.5f, 0f);
        bgRt.anchoredPosition = new Vector2(0f, BarOffsetFromBottom);
        bgRt.sizeDelta = new Vector2(BarW, BarH);

        // 填充
        GameObject fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        fill = fillGo.AddComponent<Image>();
        fill.sprite = GameManager.MakeRectSprite();
        fill.type   = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.color  = fillColor;
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        return barBg;
    }

    // ── 球槽位 ────────────────────────────────────────────────────────
    void CreateBallSlot() {
        RectTransform slot = CreateSlotContainer("BallSlot");

        // 圆形图标
        RectTransform iconRt = CreateIconRect(slot, "Icon", IconSize, IconSize);
        Image icon = iconRt.gameObject.AddComponent<Image>();
        icon.sprite = GameManager.MakeCircleSprite();
        icon.color  = BallIconColor;

        // 进度条
        Image fill;
        CreateBar(slot, BallIconColor, out fill);
        ballFillImage = fill;
    }

    // ── 棒槽位 ────────────────────────────────────────────────────────
    void CreateStickSlot(int length) {
        RectTransform slot = CreateSlotContainer($"StickSlot_L{length}");

        // 细矩形图标，旋转 45°
        RectTransform iconRt = CreateIconRect(slot, "Icon", StickIconW, StickIconH);
        Image icon = iconRt.gameObject.AddComponent<Image>();
        icon.sprite = GameManager.MakeRectSprite();
        icon.color  = StickIconColor;
        iconRt.localEulerAngles = new Vector3(0f, 0f, 45f);

        // 进度条
        Image fill;
        CreateBar(slot, StickIconColor, out fill);
        stickFillImages.Add(fill);
    }

    // ── 每帧刷新数量 ──────────────────────────────────────────────────
    void Update() => UpdateCounts();

    void UpdateCounts() {
        var gm      = GameManager.Instance;
        var backpack = Backpack.Instance;

        int ballCount = backpack != null ? backpack.BallCount
                      : (gm != null ? gm.allocatableBallCount : 0);
        if (ballFillImage != null)
            ballFillImage.fillAmount = maxPerItem > 0 ? Mathf.Clamp01(ballCount / (float)maxPerItem) : 0f;

        for (int i = 0; i < stickLengths.Length && i < stickFillImages.Count; i++) {
            int count = backpack != null ? backpack.GetStickCount(stickLengths[i])
                      : (gm != null ? gm.stickCount : 0);
            stickFillImages[i].fillAmount = maxPerItem > 0 ? Mathf.Clamp01(count / (float)maxPerItem) : 0f;
        }
    }
}
