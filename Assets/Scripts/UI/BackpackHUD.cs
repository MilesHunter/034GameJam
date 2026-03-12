using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包 HUD：用于在 BackpackSystemTest 场景中显示背包中的球和棒库存。
/// 主场景不挂载本组件。
/// </summary>
public class BackpackHUD : MonoBehaviour {
    [Header("Layout")]
    [SerializeField] Vector2 ballLabelOffset = new Vector2(10, -10);
    [SerializeField] Vector2 stickLabelOffset = new Vector2(10, -40);
    [SerializeField] float lineSpacing = 24f;

    Canvas canvas;
    Text ballText;
    readonly List<Text> stickLines = new List<Text>();

    void Awake() {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        ballText = CreateLabel(font, ballLabelOffset, "[Backpack] 球: 0");
    }

    Text CreateLabel(Font font, Vector2 anchoredPos, string defaultText) {
        GameObject go = new GameObject("BackpackLabel");
        go.transform.SetParent(transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(260, 24);

        Text txt = go.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 18;
        txt.color = Color.cyan;
        txt.text = defaultText;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);

        return txt;
    }

    Text CreateStickLine(Font font, int index) {
        Vector2 pos = stickLabelOffset + new Vector2(0, -lineSpacing * index);
        return CreateLabel(font, pos, "L=?: 0");
    }

    void Update() {
        var backpack = Backpack.Instance;
        if (backpack == null) {
            if (ballText != null)
                ballText.text = "[Backpack] (未挂载)";
            return;
        }

        ballText.text = $"[Backpack] 球: {backpack.BallCount}";

        // 收集并排序长度，稳定显示
        var snapshot = new List<KeyValuePair<int, int>>(backpack.GetStickSnapshot());
        snapshot.Sort((a, b) => a.Key.CompareTo(b.Key));

        Font font = ballText.font;

        // 确保足够的行数
        while (stickLines.Count < snapshot.Count) {
            stickLines.Add(CreateStickLine(font, stickLines.Count));
        }

        // 更新显示
        for (int i = 0; i < stickLines.Count; i++) {
            Text line = stickLines[i];
            if (i < snapshot.Count) {
                var kv = snapshot[i];
                line.gameObject.SetActive(true);
                line.text = $"L={kv.Key}: {kv.Value} 根";
            } else {
                line.gameObject.SetActive(false);
            }
        }
    }
}
