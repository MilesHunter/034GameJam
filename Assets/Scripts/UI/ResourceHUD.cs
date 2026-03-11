using UnityEngine;
using UnityEngine.UI;

// 左上角 HUD：显示球/棒库存数量和物理暂停状态
public class ResourceHUD : MonoBehaviour {
    Text ballText;
    Text stickText;
    Text pauseText;

    void Awake() {
        // ScreenSpace Overlay Canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 18);

        ballText  = CreateLabel(font, new Vector2(10, -10), "球: 0");
        stickText = CreateLabel(font, new Vector2(10, -38), "棒: 0");
        pauseText = CreateCenterLabel(font);
    }

    Text CreateLabel(Font font, Vector2 anchoredPos, string defaultText) {
        GameObject go = new GameObject("HUDLabel");
        go.transform.SetParent(transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(200, 30);

        Text txt = go.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.text = defaultText;

        // 阴影提升可读性
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);

        return txt;
    }

    Text CreateCenterLabel(Font font) {
        GameObject go = new GameObject("PauseLabel");
        go.transform.SetParent(transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300, 50);

        Text txt = go.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 28;
        txt.color = new Color(1f, 1f, 0.3f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = "";

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.9f);
        shadow.effectDistance = new Vector2(2, -2);

        return txt;
    }

    void Update() {
        var gm = GameManager.Instance;
        if (gm == null) return;

        ballText.text  = $"球: {gm.allocatableBallCount}";
        stickText.text = $"棒: {gm.stickCount}";

        bool paused = UnityEngine.Physics2D.simulationMode == SimulationMode2D.Script;
        pauseText.text = paused ? "[ 已暂停 ]" : "";
    }
}
