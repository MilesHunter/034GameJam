using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    const int MaxInventoryPerItem = 256;

    public enum GamePhase { Build, Simulate }
    public GamePhase Phase { get; private set; } = GamePhase.Build;

    [Header("Resources")]
    public int allocatableBallCount = 5;
    public int stickCount = 10;

    [Tooltip("初始可用的连接棒长度列表；若为空则回退到单个长度 2。RadialMenu 和 BuildToolbar 会基于此构建选项。")]
    [SerializeField] int[] initialStickLengths = new int[] { 2, 4, 6 };

    public List<int> availableStickLengths = new List<int>();

    readonly Dictionary<int, int> stickWarehouse = new Dictionary<int, int>();

    [Header("Stick Refresh")]
    [SerializeField] bool autoRefreshSticks = true;
    [SerializeField] float stickRefreshMinSeconds = 2f;
    [SerializeField] float stickRefreshMaxSeconds = 4f;

    float stickRefreshTimer;

    public bool IsPhysicsPaused { get; private set; }
    public bool IsGamePaused { get; private set; }

    [Header("References")]
    public InitialBall initialBall;

    [System.Serializable]
    public class StickPrefabConfig {
        public int length;
        public Stick prefab;
    }

    [Header("Prefabs")]
    [SerializeField] StickPrefabConfig[] stickPrefabs;

    [Tooltip("可分配连接球预制体（必须带有 AllocatableBall、Collider、SpriteRenderer 等）。为空时使用代码临时生成。")]
    [SerializeField] AllocatableBall allocatableBallPrefab;

    void Awake() {
        Instance = this;

        if (availableStickLengths.Count == 0) {
            if (initialStickLengths != null && initialStickLengths.Length > 0) {
                foreach (int len in initialStickLengths) {
                    if (len <= 0) continue;
                    if (!availableStickLengths.Contains(len))
                        availableStickLengths.Add(len);
                }
            }

            if (availableStickLengths.Count == 0)
                availableStickLengths.Add(2);
        }

        allocatableBallCount = Mathf.Clamp(allocatableBallCount, 0, MaxInventoryPerItem);
        stickCount = Mathf.Clamp(stickCount, 0, MaxInventoryPerItem);

        ResetStickRefreshTimer();
        EnterBuild();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R))
            RestartScene();

        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (InteractionManager.Instance != null && InteractionManager.Instance.IsPlacingStick)
                return;
            if (InteractionManager.Instance != null && InteractionManager.Instance.IsManipulatingStick)
                return;
            ToggleGamePause();
        }

        if (IsGamePaused)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
            TogglePhase();

        if (autoRefreshSticks)
            TickStickRefresh();
    }

    void TickStickRefresh() {
        stickRefreshTimer -= Time.deltaTime;
        if (stickRefreshTimer > 0f) return;
        AddGenericSticks(1);
        ResetStickRefreshTimer();
    }

    void ResetStickRefreshTimer() {
        float min = Mathf.Max(0.1f, stickRefreshMinSeconds);
        float max = Mathf.Max(min, stickRefreshMaxSeconds);
        stickRefreshTimer = Random.Range(min, max);
    }

    // ----------------------------------------------------------------
    // 资源辅助方法：统一管理库存并在存在 Backpack 时同步
    // ----------------------------------------------------------------

    public void AddAllocatableBalls(int amount) {
        if (amount == 0) return;
        allocatableBallCount += amount;
        allocatableBallCount = Mathf.Clamp(allocatableBallCount, 0, MaxInventoryPerItem);
        if (Backpack.Instance != null && amount > 0)
            Backpack.Instance.AddBalls(amount);
    }

    public bool TryConsumeAllocatableBalls(int amount) {
        if (amount <= 0) return true;
        if (allocatableBallCount < amount)
            return false;
        allocatableBallCount -= amount;
        if (Backpack.Instance != null)
            Backpack.Instance.TryConsumeBalls(amount);
        return true;
    }

    public void AddGenericSticks(int amount) {
        if (amount == 0) return;
        stickCount += amount;
        stickCount = Mathf.Clamp(stickCount, 0, MaxInventoryPerItem);

        if (Backpack.Instance != null && amount > 0) {
            int defaultLength = GetDefaultStickLength();
            if (defaultLength > 0)
                Backpack.Instance.AddSticks(defaultLength, amount);
        }
    }

    public bool TryConsumeGenericStick() {
        if (stickCount <= 0) return false;
        stickCount--;
        return true;
    }

    int GetDefaultStickLength() {
        if (availableStickLengths == null || availableStickLengths.Count == 0)
            return 1;
        int min = availableStickLengths[0];
        for (int i = 1; i < availableStickLengths.Count; i++)
            if (availableStickLengths[i] < min) min = availableStickLengths[i];
        return min > 0 ? min : 1;
    }

    public void SetPhysicsPaused(bool paused) {
        if (IsPhysicsPaused == paused)
            return;

        IsPhysicsPaused = paused;
        Physics2D.simulationMode = IsPhysicsPaused
            ? SimulationMode2D.Script
            : SimulationMode2D.FixedUpdate;
    }

    void TogglePhase() {
        if (Phase == GamePhase.Build)
            EnterSimulate();
        else
            EnterBuild();
    }

    void EnterBuild() {
        Phase = GamePhase.Build;
        SetPhysicsPaused(true);

        foreach (Stick stick in FindObjectsByType<Stick>(FindObjectsSortMode.None)) {
            if (stick == null || stick.rb == null) continue;
            var rb = stick.rb;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        foreach (Ball ball in FindObjectsByType<Ball>(FindObjectsSortMode.None)) {
            if (ball == null) continue;
            var rb = ball.Rigidbody;
            if (rb == null) continue;
            if (!ball.isFixed) {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    void EnterSimulate() {
        Phase = GamePhase.Simulate;
        SetPhysicsPaused(false);

        foreach (Stick stick in FindObjectsByType<Stick>(FindObjectsSortMode.None)) {
            if (stick == null || stick.rb == null) continue;
            var rb = stick.rb;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }

        foreach (Ball ball in FindObjectsByType<Ball>(FindObjectsSortMode.None)) {
            if (ball == null) continue;
            var rb = ball.Rigidbody;
            if (rb == null) continue;
            if (!ball.isFixed) {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
            }
        }
    }

    void ToggleGamePause() {
        IsGamePaused = !IsGamePaused;
        Time.timeScale = IsGamePaused ? 0f : 1f;
        AudioListener.pause = IsGamePaused;
    }

    void RestartScene() {
        IsGamePaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void UnlockStickLength(int length) {
        if (!availableStickLengths.Contains(length))
            availableStickLengths.Add(length);
    }

    public void StoreStick(Stick stick) {
        if (stick == null) return;

        if (Phase != GamePhase.Build) return;

        if (stick.FullyConnected) return;

        int length = Mathf.RoundToInt(stick.transform.localScale.y);
        if (length <= 0) length = 1;

        int current;
        stickWarehouse.TryGetValue(length, out current);
        stickWarehouse[length] = current + 1;

        if (Backpack.Instance != null)
            Backpack.Instance.AddSticks(length, 1);

        stick.Delete();
    }

    public bool HasWarehouseSticks() {
        foreach (var kv in stickWarehouse) {
            if (kv.Value > 0) return true;
        }
        return false;
    }

    public IEnumerable<KeyValuePair<int, int>> GetWarehouseSnapshot() {
        foreach (var kv in stickWarehouse)
            yield return kv;
    }

    public bool TryWithdrawStick(int length, Vector2 spawnPos, out Stick stick) {
        stick = null;
        int count;
        if (!stickWarehouse.TryGetValue(length, out count) || count <= 0)
            return false;

        count -= 1;
        if (count <= 0) stickWarehouse.Remove(length);
        else stickWarehouse[length] = count;

        if (Backpack.Instance != null)
            Backpack.Instance.TryConsumeStick(length);

        Stick prefabInstanceForLength = GetStickPrefabForLength(length);
        if (prefabInstanceForLength != null) {
            Stick instance = Instantiate(prefabInstanceForLength, spawnPos, Quaternion.identity);
            instance.gameObject.name = $"WarehouseStick_L{length}";
            stick = instance;
            return true;
        }

        GameObject go = new GameObject($"WarehouseStick_L{length}");
        go.transform.position = spawnPos;
        go.transform.localScale = new Vector3(0.3f, length, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeRectSprite();
        sr.color = new Color(0.6f, 0.6f, 0.9f);
        sr.sortingOrder = 1;
        go.AddComponent<SelectableOutline>();
        go.AddComponent<SelectableOutline>();

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 0.5f;
        rb.gravityScale = 1f;
        rb.drag = 0.2f;
        rb.angularDrag = 0.5f;

        stick = go.AddComponent<Stick>();
        stick.rb = rb;

        GameObject endAGo = new GameObject("EndA");
        endAGo.transform.SetParent(go.transform, false);
        endAGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        GameObject endBGo = new GameObject("EndB");
        endBGo.transform.SetParent(go.transform, false);
        endBGo.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        stick.Initialize(endAGo.transform, endBGo.transform);

        return true;
    }

    public void OnConnectionChanged() {
        if (initialBall == null) return;
        RecalculateConnectivity();
    }

    void RecalculateConnectivity() {
        var visited = new HashSet<Ball>();
        var queue = new Queue<Ball>();
        queue.Enqueue(initialBall);
        visited.Add(initialBall);

        while (queue.Count > 0) {
            Ball current = queue.Dequeue();
            foreach (Stick stick in current.connectedSticks) {
                Ball other = (stick.endpointA == current) ? stick.endpointB : stick.endpointA;
                if (other != null && !visited.Contains(other)) {
                    visited.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        foreach (Ball ball in FindObjectsByType<Ball>(FindObjectsSortMode.None)) {
            bool nowConnected = visited.Contains(ball);
            if (ball.connectedToInitial != nowConnected)
                ball.OnConnectedToInitialChanged(nowConnected);
        }
    }

    // ----------------------------------------------------------------
    // 运行时生成：连接棒
    // ----------------------------------------------------------------
    // 返回处于放置模式的 Stick；若库存不足则返回 null
    public Stick SpawnStick(Ball anchorBall, int length) {
        if (!TryConsumeGenericStick())
            return null;

        if (Backpack.Instance != null)
            Backpack.Instance.TryConsumeStick(length);

        Stick prefabInstanceForLength = GetStickPrefabForLength(length);
        if (prefabInstanceForLength != null) {
            Vector3 pos = anchorBall != null
                ? anchorBall.transform.position
                : Vector3.zero;

            Stick instance = Instantiate(prefabInstanceForLength, pos, Quaternion.identity);
            instance.gameObject.name = $"Stick_L{length}";
            instance.StartPlacement(anchorBall);
            return instance;
        }

        GameObject go = new GameObject($"Stick_L{length}");
        go.transform.position = anchorBall.transform.position;
        go.transform.localScale = new Vector3(0.3f, length, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeRectSprite();
        sr.color = new Color(0.6f, 0.6f, 0.9f);
        sr.sortingOrder = 1;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 0.5f;
        rb.gravityScale = 1f;
        rb.drag = 0.2f;
        rb.angularDrag = 0.5f;

        Stick stick = go.AddComponent<Stick>();
        stick.rb = rb;

        // 端点子对象（局部坐标 ±0.5 在 localScale 下对应世界 ±length/2）
        GameObject endAGo = new GameObject("EndA");
        endAGo.transform.SetParent(go.transform, false);
        endAGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        GameObject endBGo = new GameObject("EndB");
        endBGo.transform.SetParent(go.transform, false);
        endBGo.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        stick.Initialize(endAGo.transform, endBGo.transform);
        stick.StartPlacement(anchorBall);
        return stick;
    }

    public Stick SpawnRandomStick(Ball anchorBall) {
        if (anchorBall == null) return null;
        if (availableStickLengths == null || availableStickLengths.Count == 0) return null;
        int len = availableStickLengths[Random.Range(0, availableStickLengths.Count)];
        return SpawnStick(anchorBall, len);
    }

    Stick GetStickPrefabForLength(int length) {
        if (stickPrefabs == null)
            return null;
        for (int i = 0; i < stickPrefabs.Length; i++) {
            var cfg = stickPrefabs[i];
            if (cfg != null && cfg.prefab != null && cfg.length == length)
                return cfg.prefab;
        }
        return null;
    }

    public Ball FindNearestBall(Vector2 mousePos, float maxDistance = 2.5f) {
        Ball nearest = null;
        float best = maxDistance * maxDistance;

        foreach (Ball ball in FindObjectsByType<Ball>(FindObjectsSortMode.None)) {
            float d = ((Vector2)ball.transform.position - mousePos).sqrMagnitude;
            if (d < best) {
                best = d;
                nearest = ball;
            }
        }

        return nearest;
    }

    static bool TryAttachFreeEndToNearbyBall(Stick stick, Transform freeEnd) {
        const float nearbyBallAttachRadius = 0.35f;
        int ballMask = LayerMask.GetMask("Ball");
        Collider2D[] hits = Physics2D.OverlapCircleAll(freeEnd.position, nearbyBallAttachRadius, ballMask);

        Ball nearest = null;
        float best = float.MaxValue;
        foreach (Collider2D col in hits) {
            if (!col.TryGetComponent(out Ball candidate))
                continue;
            if (!candidate.CanConnect())
                continue;
            if (candidate == stick.endpointA || candidate == stick.endpointB)
                continue;

            float d = ((Vector2)candidate.transform.position - (Vector2)freeEnd.position).sqrMagnitude;
            if (d < best) {
                best = d;
                nearest = candidate;
            }
        }

        if (nearest == null)
            return false;

        stick.AttachToFreeEnd(nearest);
        return true;
    }

    static bool TryAttachFreeEndToNearbyStickEnd(Stick stick, Transform freeEnd) {
        if (stick == null || freeEnd == null) return false;

        const float nearbyEndAttachRadius = 0.35f;
        float best = nearbyEndAttachRadius * nearbyEndAttachRadius;

        Stick bestStick = null;
        Transform bestEnd = null;

        foreach (Stick other in FindObjectsByType<Stick>(FindObjectsSortMode.None)) {
            if (other == null || other == stick) continue;

            foreach (Transform otherEnd in other.GetFreeEnds()) {
                if (otherEnd == null) continue;

                float d = ((Vector2)otherEnd.position - (Vector2)freeEnd.position).sqrMagnitude;
                if (d < best) {
                    best = d;
                    bestStick = other;
                    bestEnd = otherEnd;
                }
            }
        }

        if (bestStick == null || bestEnd == null)
            return false;

        Vector2 spawnPos = ((Vector2)freeEnd.position + (Vector2)bestEnd.position) * 0.5f;

        AllocatableBall ball;
        if (Instance != null && Instance.allocatableBallPrefab != null) {
            ball = Instantiate(Instance.allocatableBallPrefab, spawnPos, Quaternion.identity);
        } else {
            GameObject go = new GameObject("AutoBall");
            go.transform.position = spawnPos;
            go.layer = LayerMask.NameToLayer("Ball");
            go.transform.localScale = Vector3.one * 0.8f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeCircleSprite();
            sr.color = new Color(0.9f, 0.5f, 0.5f);
            sr.sortingOrder = 1;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;

            ball = go.AddComponent<AllocatableBall>();
            ball.isFixed = false;
            ball.connectionLimit = 24;
        }

        bool attachedA = stick.AttachAtEnd(freeEnd, ball);
        bool attachedB = bestStick.AttachAtEnd(bestEnd, ball);
        if (!attachedA || !attachedB) {
            if (ball != null)
                Destroy(ball.gameObject);
            return false;
        }

        return true;
    }

    public void TryAttachStickFreeEndsToExistingConnections(Stick stick) {
        if (stick == null) return;

        foreach (Transform end in stick.GetFreeEnds()) {
            if (end == null) continue;
            if (!stick.IsEndFree(end)) continue;
            if (TryAttachFreeEndToNearbyBall(stick, end)) continue;
            TryAttachFreeEndToNearbyStickEnd(stick, end);
        }
    }

    public void AutoSpawnBallOnStickFreeEnd(Stick stick) {
        if (stick == null) return;
        Transform freeEnd = stick.FreeEnd;
        if (freeEnd == null) return;

        if (TryAttachFreeEndToNearbyBall(stick, freeEnd))
            return;

        if (TryAttachFreeEndToNearbyStickEnd(stick, freeEnd))
            return;

        AllocatableBall ball;
        if (Instance != null && Instance.allocatableBallPrefab != null) {
            ball = Instantiate(Instance.allocatableBallPrefab, freeEnd.position, Quaternion.identity);
        } else {
            GameObject go = new GameObject("AutoBall");
            go.transform.position = freeEnd.position;
            go.layer = LayerMask.NameToLayer("Ball");
            go.transform.localScale = Vector3.one * 0.6f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeCircleSprite();
            sr.color = new Color(0.9f, 0.5f, 0.5f);
            sr.sortingOrder = 1;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;

            ball = go.AddComponent<AllocatableBall>();
            ball.isFixed = false;
            ball.connectionLimit = 24;
        }

        stick.AttachToFreeEnd(ball);
    }

    // ----------------------------------------------------------------
    // 运行时生成：可分配连接球（放在棒子自由端附近）
    // ----------------------------------------------------------------
    public bool SpawnAllocatableBall(Stick stick) {
        Transform freeEnd = stick.FreeEnd;
        if (freeEnd == null) return false;
        if (!TryConsumeAllocatableBalls(1)) return false;

        Vector2 spawnPos = (Vector2)freeEnd.position + Vector2.up * 0.3f;

        if (allocatableBallPrefab != null) {
            Instantiate(allocatableBallPrefab, spawnPos, Quaternion.identity);
            return true;
        }

        GameObject go = new GameObject("AllocatableBall");
        go.transform.position = spawnPos;
        go.layer = LayerMask.NameToLayer("Ball");
        go.transform.localScale = Vector3.one * 0.6f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite();
        sr.color = new Color(0.9f, 0.5f, 0.5f);
        sr.sortingOrder = 1;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 0.5f;
        rb.gravityScale = 1f;
        rb.drag = 0.2f;
        rb.angularDrag = 0.5f;

        AllocatableBall ball = go.AddComponent<AllocatableBall>();
        ball.isFixed = false;
        ball.connectionLimit = 24;
        return true;
    }

    /// <summary>
    /// 在棒子的自由端安装一个新的可分配球，并消耗一个球库存。
    /// 返回是否安装成功（例如库存不足或无自由端时返回 false）。
    /// </summary>
    public bool TryInstallBallOnStickFreeEnd(Stick stick) {
        if (stick == null) return false;
        Transform freeEnd = stick.FreeEnd;
        if (freeEnd == null) return false;

        if (!TryConsumeAllocatableBalls(1))
            return false;

        AllocatableBall ball;
        if (allocatableBallPrefab != null) {
            ball = Instantiate(allocatableBallPrefab, freeEnd.position, Quaternion.identity);
        } else {
            GameObject go = new GameObject("AllocatableBall");
            go.transform.position = freeEnd.position;
            go.layer = LayerMask.NameToLayer("Ball");
            go.transform.localScale = Vector3.one * 0.6f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeCircleSprite();
            sr.color = new Color(0.9f, 0.5f, 0.5f);
            sr.sortingOrder = 1;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 0.5f;
            rb.gravityScale = 1f;
            rb.drag = 0.2f;
            rb.angularDrag = 0.5f;

            go.AddComponent<SelectableOutline>();
            ball = go.AddComponent<AllocatableBall>();
            ball.isFixed = false;
            ball.connectionLimit = 24;
        }

        stick.AttachToFreeEnd(ball);
        return true;
    }

    // ----------------------------------------------------------------
    // 库存归还
    // ----------------------------------------------------------------
    public void ReturnStick() => AddGenericSticks(1);

    public void ReturnAllocatableBall() => AddAllocatableBalls(1);

    // ----------------------------------------------------------------
    // 程序化 Sprite（运行时）
    // ----------------------------------------------------------------
    static Sprite _circleSprite;
    static Sprite _rectSprite;

    public static Sprite MakeCircleSprite() {
        if (_circleSprite != null) return _circleSprite;
        int res = 64;
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(res / 2f, res / 2f);
        float r = res / 2f - 1;
        Color[] px = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                px[y * res + x] = Vector2.Distance(new Vector2(x, y), c) <= r ? Color.white : Color.clear;
        tex.SetPixels(px);
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        return _circleSprite;
    }

    public static Sprite MakeRectSprite() {
        if (_rectSprite != null) return _rectSprite;
        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _rectSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return _rectSprite;
    }
}
