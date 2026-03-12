#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor 工具：一键生成 BackpackSystemTest 测试场景。
/// 场景内容：
/// - 主摄像机 + CameraController
/// - GameManager（初始资源）
/// - ConnectionCleaner / InteractionManager / WarehouseUI
/// - Backpack + BackpackHUD，用于观察背包与资源的联动
/// 不影响现有主场景。
/// </summary>
public static class BackpackSystemTestBuilder {

    [MenuItem("034GameJam/Build Backpack System Test Scene")]
    public static void BuildBackpackTestScene() {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 摄像机
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<CameraController>();

        // GameManager
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.allocatableBallCount = 5;
        gm.stickCount = 8;
        gm.availableStickLengths.Clear();
        gm.availableStickLengths.Add(2);
        gm.availableStickLengths.Add(4);

        // 地面
        CreateGround();

        // 初始球
        GameObject initBallGo = CreateFixedBall<InitialBall>(Vector2.zero, "InitialBall", Color.green);
        InitialBall initBall = initBallGo.GetComponent<InitialBall>();
        gm.initialBall = initBall;

        // 若需要，可放一个 ProductionBall 演示资源增长
        GameObject prodGo = CreateFixedBall<ProductionBall>(new Vector2(-4, 0), "ProductionBall", new Color(0.5f, 0.5f, 0.2f));
        ProductionBall prodBall = prodGo.GetComponent<ProductionBall>();
        prodBall.productionInterval = 6f;
        prodBall.ballOutput = 1;
        prodBall.stickOutput = 1;

        // 基础系统
        GameObject cleanerGo = new GameObject("ConnectionCleaner");
        cleanerGo.AddComponent<ConnectionCleaner>();

        GameObject imGo = new GameObject("InteractionManager");
        imGo.AddComponent<InteractionManager>();

        GameObject whGo = new GameObject("WarehouseUI");
        whGo.AddComponent<WarehouseUI>();

        // Backpack + HUD
        GameObject backpackGo = new GameObject("Backpack");
        Backpack backpack = backpackGo.AddComponent<Backpack>();

        // 与 GameManager 初始资源对齐：按长度分配 stickCount
        var sticks = new Dictionary<int, int>();
        foreach (int len in gm.availableStickLengths)
            sticks[len] = 0;
        int remaining = gm.stickCount;
        int idx = 0;
        var lens = gm.availableStickLengths;
        while (remaining > 0 && lens.Count > 0) {
            int len = lens[idx % lens.Count];
            sticks[len] = sticks[len] + 1;
            remaining--;
            idx++;
        }
        backpack.Initialize(gm.allocatableBallCount, sticks);

        GameObject hudGo = new GameObject("BackpackHUD");
        hudGo.AddComponent<BackpackHUD>();

        GameObject toolbarGo = new GameObject("BuildToolbar");
        toolbarGo.AddComponent<BuildToolbar>();

        // 保存场景
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, "Assets/Scenes/BackpackSystemTest.unity");
        if (saved)
            Debug.Log("[BackpackSystemTestBuilder] 场景已生成并保存至 Assets/Scenes/BackpackSystemTest.unity");
        else
            Debug.LogWarning("[BackpackSystemTestBuilder] 场景生成完成，但保存失败，请手动保存。");
    }

    static void CreateGround() {
        GameObject ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0, -8, 0);

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = TestSceneBuilder_CreateRectSprite();
        sr.color = new Color(0.4f, 0.3f, 0.2f);
        ground.transform.localScale = new Vector3(40, 2, 1);

        BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    static GameObject CreateFixedBall<T>(Vector2 pos, string name, Color color) where T : Ball {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.layer = LayerMask.NameToLayer("Ball");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = TestSceneBuilder_CreateCircleSprite();
        sr.color = color;
        go.transform.localScale = Vector3.one * 0.8f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;

        T ball = go.AddComponent<T>();
        ball.isFixed = true;
        ball.connectionLimit = 24;
        return go;
    }

    // 为避免依赖 TestSceneBuilder 内部方法，在此复制最小 Sprite 生成逻辑
    static Sprite TestSceneBuilder_CreateCircleSprite() {
        int res = 64;
        Texture2D tex = new Texture2D(res, res);
        Vector2 center = new Vector2(res / 2f, res / 2f);
        float radius = res / 2f - 1;
        Color[] pixels = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * res + x] = dist <= radius ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    static Sprite TestSceneBuilder_CreateRectSprite() {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}

#endif
