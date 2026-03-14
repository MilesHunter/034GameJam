using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 在屏幕右上角显示 BGM / SFX 音量滑条。
/// 依赖 AudioManager 单例（若不存在则只调节 AudioListener.volume）。
/// </summary>
public class VolumeControlUI : MonoBehaviour {
    [Header("Panel Layout")]
    [SerializeField] Vector2 panelAnchoredPosition = new Vector2(-10f, -10f);
    [SerializeField] float panelWidth  = 180f;
    [SerializeField] float panelHeight = 80f;

    void Awake() {
        EnsureEventSystem();
        BuildUI();
    }

    void EnsureEventSystem() {
        if (FindObjectOfType<EventSystem>() == null) {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildUI() {
        // ── Canvas ──────────────────────────────────────────────────
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // ── Panel (右上角) ──────────────────────────────────────────
        var panelGo = new GameObject("VolumePanel");
        panelGo.transform.SetParent(canvas.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot     = new Vector2(1f, 1f);
        panelRt.anchoredPosition = panelAnchoredPosition;
        panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);

        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);

        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding     = new RectOffset(8, 8, 6, 6);
        vlg.spacing     = 4f;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── BGM 行 ──────────────────────────────────────────────────
        var bgmFill = CreateSliderRow(panelGo.transform, "BGM", 0.5f,
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(v); });

        // ── SFX 行 ──────────────────────────────────────────────────
        var sfxFill = CreateSliderRow(panelGo.transform, "SFX", 1f,
            v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v); });
    }

    // 创建一行：标签 + 滑条
    static Image CreateSliderRow(Transform parent, string label, float initValue,
                                 System.Action<float> onChange) {
        var row = new GameObject($"Row_{label}");
        row.transform.SetParent(parent, false);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, 22f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // 标签
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(row.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.sizeDelta = new Vector2(32f, 0f);
        var txt = labelGo.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 11;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 滑条背景
        var trackGo = new GameObject("Track");
        trackGo.transform.SetParent(row.transform, false);
        var trackRt = trackGo.AddComponent<RectTransform>();
        trackRt.sizeDelta = new Vector2(120f, 0f);
        var trackImg = trackGo.AddComponent<Image>();
        trackImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // 填充
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(trackGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(initValue, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.75f, 0.4f, 1f);

        // 把点击/拖拽注册到 trackGo
        var handler = trackGo.AddComponent<SliderHandler>();
        handler.fillRect  = fillRt;
        handler.trackRect = trackRt;
        handler.onChange  = onChange;
        handler.SetValue(initValue);

        return fillImg;
    }

    // 内部轻量拖拽处理器
    class SliderHandler : MonoBehaviour,
        IPointerDownHandler, IDragHandler {

        public RectTransform fillRect;
        public RectTransform trackRect;
        public System.Action<float> onChange;
        float currentValue;

        public void SetValue(float v) {
            currentValue = Mathf.Clamp01(v);
            if (fillRect != null)
                fillRect.anchorMax = new Vector2(currentValue, 1f);
        }

        void Notify(PointerEventData ev) {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                trackRect, ev.position, ev.pressEventCamera, out local);
            float t = Mathf.Clamp01((local.x + trackRect.rect.width * 0.5f) / trackRect.rect.width);
            SetValue(t);
            onChange?.Invoke(t);
        }

        public void OnPointerDown(PointerEventData ev) => Notify(ev);
        public void OnDrag(PointerEventData ev)        => Notify(ev);
    }
}
