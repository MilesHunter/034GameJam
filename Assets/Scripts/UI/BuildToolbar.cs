using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildToolbar : MonoBehaviour {
    [Header("Stick Length Presets")]
    [SerializeField] int[] stickLengths = new int[] { 2, 4, 6 };

    [Header("Inventory Limit Per Item")]
    [SerializeField] int maxPerItem = 256;

    Canvas canvas;
    RectTransform root;

    Image ballFillImage;
    readonly List<Image> stickFillImages = new List<Image>();

    void Awake() {
        EnsureEventSystem();
        CreateCanvas();
        CreateRootAndSlots();
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
        canvas.sortingOrder = 150;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
    }

    void CreateRootAndSlots() {
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

        CreateBallSlot();
        for (int i = 0; i < stickLengths.Length; i++) {
            CreateStickSlot(stickLengths[i]);
        }
    }

    void CreateBallSlot() {
        GameObject slotGo = new GameObject("BallSlot");
        slotGo.transform.SetParent(root, false);
        RectTransform slotRt = slotGo.AddComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(140, 40);

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        Image icon = iconGo.AddComponent<Image>();
        icon.sprite = GameManager.MakeCircleSprite();
        icon.color = new Color(0.9f, 0.5f, 0.5f, 0.9f);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(20f, 0f);
        iconRt.sizeDelta = new Vector2(24f, 24f);

        GameObject barBgGo = new GameObject("BarBG");
        barBgGo.transform.SetParent(slotGo.transform, false);
        Image barBg = barBgGo.AddComponent<Image>();
        barBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        RectTransform bgRt = barBgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(0f, 0.5f);
        bgRt.pivot = new Vector2(0f, 0.5f);
        bgRt.anchoredPosition = new Vector2(40f, 0f);
        bgRt.sizeDelta = new Vector2(90f, 8f);

        GameObject barFillGo = new GameObject("BarFill");
        barFillGo.transform.SetParent(barBgGo.transform, false);
        Image barFill = barFillGo.AddComponent<Image>();
        barFill.sprite = GameManager.MakeRectSprite();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.color = new Color(0.9f, 0.5f, 0.5f, 0.9f);
        RectTransform fillRt = barFillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        ballFillImage = barFill;
    }

    void CreateStickSlot(int length) {
        GameObject slotGo = new GameObject($"StickSlot_L{length}");
        slotGo.transform.SetParent(root, false);
        RectTransform slotRt = slotGo.AddComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(160, 40);

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        Image icon = iconGo.AddComponent<Image>();
        icon.sprite = GameManager.MakeRectSprite();
        icon.color = new Color(0.6f, 0.6f, 0.9f, 0.9f);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);

        float baseWidth = 20f;
        float width = baseWidth + length * 4f;
        iconRt.anchoredPosition = new Vector2(20f, 0f);
        iconRt.sizeDelta = new Vector2(width, 10f);

        GameObject barBgGo = new GameObject("BarBG");
        barBgGo.transform.SetParent(slotGo.transform, false);
        Image barBg = barBgGo.AddComponent<Image>();
        barBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        RectTransform bgRt = barBgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(0f, 0.5f);
        bgRt.pivot = new Vector2(0f, 0.5f);
        bgRt.anchoredPosition = new Vector2(40f, 0f);
        bgRt.sizeDelta = new Vector2(100f, 8f);

        GameObject barFillGo = new GameObject("BarFill");
        barFillGo.transform.SetParent(barBgGo.transform, false);
        Image barFill = barFillGo.AddComponent<Image>();
        barFill.sprite = GameManager.MakeRectSprite();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.color = new Color(0.6f, 0.6f, 0.9f, 0.9f);
        RectTransform fillRt = barFillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        stickFillImages.Add(barFill);
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

        float ballFill = maxPerItem > 0 ? Mathf.Clamp01(ballCount / (float)maxPerItem) : 0f;
        if (ballFillImage != null)
            ballFillImage.fillAmount = ballFill;

        for (int i = 0; i < stickLengths.Length && i < stickFillImages.Count; i++) {
            int length = stickLengths[i];
            int count = 0;
            if (backpack != null)
                count = backpack.GetStickCount(length);
            else if (gm != null)
                count = gm.stickCount;

            float fill = maxPerItem > 0 ? Mathf.Clamp01(count / (float)maxPerItem) : 0f;
            stickFillImages[i].fillAmount = fill;
        }
    }
}
