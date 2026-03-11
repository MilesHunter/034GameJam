#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor 工具：一键生成测试场景
/// 菜单路径：034GameJam / Build Test Scene
/// </summary>
public static class TestSceneBuilder {

    [MenuItem("034GameJam/Build Test Scene")]
    public static void BuildTestScene() {
        // 新建或清空场景
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 确认 Ball Layer 存在（Layer 8）
        int ballLayer = LayerMask.NameToLayer("Ball");
        if (ballLayer < 0) {
            Debug.LogError("[TestSceneBuilder] 'Ball' Layer 不存在！请在 Project Settings > Tags and Layers 中添加 Ball Layer（建议放在 User Layer 8）。");
        }

        // ============================================================
        // 1. 摄像机
        // ============================================================
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<CameraController>();

        // ============================================================
        // 2. GameManager
        // ============================================================
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.allocatableBallCount = 5;
        gm.stickCount = 10;

        // ============================================================
        // 3. 地面（静态矩形平台，用 BoxCollider2D）
        // ============================================================
        CreateGround();

        // ============================================================
        // 4. 初始固定连接球（场景中心）
        // ============================================================
        GameObject initBallGo = CreateFixedBall<InitialBall>(Vector2.zero, "InitialBall", Color.green);
        InitialBall initBall = initBallGo.GetComponent<InitialBall>();
        gm.initialBall = initBall;

        // ============================================================
        // 5. 生产类固定连接球（左侧）
        // ============================================================
        GameObject prodGo = CreateFixedBall<ProductionBall>(new Vector2(-4, 0), "ProductionBall", new Color(0.5f, 0.5f, 0.2f));
        ProductionBall prodBall = prodGo.GetComponent<ProductionBall>();
        prodBall.productionInterval = 5f;
        prodBall.ballOutput = 2;
        prodBall.stickOutput = 3;

        // ============================================================
        // 6. 功能类固定连接球（右侧）
        // ============================================================
        GameObject funcGo = CreateFixedBall<FunctionBall>(new Vector2(4, 0), "FunctionBall", new Color(0.3f, 0.5f, 0.5f));
        FunctionBall funcBall = funcGo.GetComponent<FunctionBall>();
        funcBall.unlockStickLength = 4;


        CreateStick(new Vector2(0, 3f), 2f, "Stick_A");
        CreateStick(new Vector2(-3f, 3f), 2f, "Stick_B");
        CreateStick(new Vector2(3f, 3f), 2f, "Stick_C");
        CreateStick(new Vector2(-6f, 3f), 2f, "Stick_D");


        GameObject cleanerGo = new GameObject("ConnectionCleaner");
        cleanerGo.AddComponent<ConnectionCleaner>();


        GameObject imGo = new GameObject("InteractionManager");
        imGo.AddComponent<InteractionManager>();

        GameObject whGo = new GameObject("WarehouseUI");
        whGo.AddComponent<WarehouseUI>();

        // ============================================================
        // 保存场景
        // ============================================================
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, "Assets/Scenes/TestScene.unity");
        if (saved)
            Debug.Log("[TestSceneBuilder] 测试场景已生成并保存至 Assets/Scenes/TestScene.unity");
        else
            Debug.LogWarning("[TestSceneBuilder] 场景生成完成，但保存失败，请手动保存。");
    }

    // ----------------------------------------------------------------
    // 辅助方法
    // ----------------------------------------------------------------

    static void CreateGround() {
        GameObject ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0, -8, 0);

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRectSprite();
        sr.color = new Color(0.4f, 0.3f, 0.2f);
        ground.transform.localScale = new Vector3(40, 2, 1);

        BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
        col.size = Vector2.one; // 由 localScale 控制实际大小
    }

    // 创建一个固定球（无 Rigidbody，有静态 CircleCollider2D）
    static GameObject CreateFixedBall<T>(Vector2 pos, string name, Color color) where T : Ball {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.layer = LayerMask.NameToLayer("Ball");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = color;
        go.transform.localScale = Vector3.one * 0.8f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;

        T ball = go.AddComponent<T>();
        ball.isFixed = true;
        ball.connectionLimit = 24;
        return go;
    }

    // 创建可分配球（有 Rigidbody2D，受重力）
    static GameObject CreateAllocatableBall(Vector2 pos) {
        GameObject go = new GameObject("AllocatableBall");
        go.transform.position = pos;
        go.layer = LayerMask.NameToLayer("Ball");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(0.9f, 0.5f, 0.5f);
        go.transform.localScale = Vector3.one * 0.6f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;

        AllocatableBall ball = go.AddComponent<AllocatableBall>();
        ball.isFixed = false;
        ball.connectionLimit = 24;
        return go;
    }

    // 创建连接棒
    static GameObject CreateStick(Vector2 pos, float length, string name) {
        // 父对象（携带 Rigidbody2D 和 Stick 脚本）
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRectSprite();
        sr.color = new Color(0.6f, 0.6f, 0.9f);
        go.transform.localScale = new Vector3(0.3f, length, 1f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 0.5f;
        rb.gravityScale = 1f;
        rb.drag = 0.2f;
        rb.angularDrag = 0.5f;

        Stick stick = go.AddComponent<Stick>();
        stick.rb = rb;

        // 端点 A（上端）
        GameObject endAGo = new GameObject("EndA");
        endAGo.transform.SetParent(go.transform, false);
        endAGo.transform.localPosition = new Vector3(0, 0.5f, 0); // 局部坐标上端

        // 端点 B（下端）
        GameObject endBGo = new GameObject("EndB");
        endBGo.transform.SetParent(go.transform, false);
        endBGo.transform.localPosition = new Vector3(0, -0.5f, 0);

        // 用 SerializedObject 注入私有字段
        SerializedObject so = new SerializedObject(stick);
        so.FindProperty("endA").objectReferenceValue = endAGo.transform;
        so.FindProperty("endB").objectReferenceValue = endBGo.transform;
         so.FindProperty("attractionRadius").floatValue = 1f;
         so.FindProperty("connectionThreshold").floatValue = 0.3f;
         so.ApplyModifiedProperties();

        return go;
    }

    // ----------------------------------------------------------------
    // 程序化 Sprite 生成
    // ----------------------------------------------------------------

    static Sprite CreateCircleSprite() {
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

    static Sprite CreateRectSprite() {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
#endif
