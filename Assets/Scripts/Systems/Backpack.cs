using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包系统：记录玩家拥有的可分配球数量和按长度分类的棒子数量。
/// 默认只在测试场景中挂载；主场景不挂载时，所有调用都会被安全忽略。
/// </summary>
public class Backpack : MonoBehaviour {
    public static Backpack Instance { get; private set; }

    const int MaxPerItem = 256;

    [Header("Initial Values (optional)")]
    [SerializeField] int initialBallCount = 0;
    [SerializeField] List<int> initialStickLengths = new List<int>();
    [SerializeField] List<int> initialStickCounts = new List<int>();

    int ballCount;
    readonly Dictionary<int, int> stickCounts = new Dictionary<int, int>();

    public int BallCount => ballCount;

    public IEnumerable<KeyValuePair<int, int>> GetStickSnapshot() {
        foreach (var kv in stickCounts)
            yield return kv;
    }

    public int GetStickCount(int length) {
        return stickCounts.TryGetValue(length, out int c) ? c : 0;
    }

    void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogWarning("[Backpack] Multiple instances detected, keeping the first one.");
            Destroy(this);
            return;
        }
        Instance = this;

        ballCount = Mathf.Clamp(initialBallCount, 0, MaxPerItem);
        int count = Mathf.Min(initialStickLengths.Count, initialStickCounts.Count);
        for (int i = 0; i < count; i++) {
            int len = Mathf.Max(1, initialStickLengths[i]);
            int num = Mathf.Max(0, initialStickCounts[i]);
            if (num <= 0) continue;
            if (stickCounts.TryGetValue(len, out int existing))
                stickCounts[len] = Mathf.Clamp(existing + num, 0, MaxPerItem);
            else
                stickCounts[len] = Mathf.Clamp(num, 0, MaxPerItem);
        }
    }

    public void Initialize(int initialBalls, IDictionary<int, int> initialSticks) {
        ballCount = Mathf.Clamp(initialBalls, 0, MaxPerItem);
        stickCounts.Clear();
        if (initialSticks == null) return;
        foreach (var kv in initialSticks) {
            int len = Mathf.Max(1, kv.Key);
            int num = Mathf.Max(0, kv.Value);
            if (num <= 0) continue;
            stickCounts[len] = Mathf.Clamp(num, 0, MaxPerItem);
        }
    }

    // --------------------------------------------------------------------
    // 球库存
    // --------------------------------------------------------------------
    public void AddBalls(int amount) {
        if (amount <= 0) return;
        ballCount = Mathf.Clamp(ballCount + amount, 0, MaxPerItem);
    }

    /// <summary>
    /// 尝试消耗指定数量的球库存；不足则返回 false，不修改数量。
    /// </summary>
    public bool TryConsumeBalls(int amount) {
        if (amount <= 0) return true;
        if (ballCount < amount)
            return false;
        ballCount -= amount;
        return true;
    }

    // --------------------------------------------------------------------
    // 棒库存（按长度分类）
    // --------------------------------------------------------------------
    public void AddSticks(int length, int amount) {
        if (amount <= 0) return;
        length = Mathf.Max(1, length);
        if (!stickCounts.TryGetValue(length, out int existing)) existing = 0;
        stickCounts[length] = Mathf.Clamp(existing + amount, 0, MaxPerItem);
    }

    /// <summary>
    /// 尝试消耗指定长度的一根棒子；返回是否成功。
    /// </summary>
    public bool TryConsumeStick(int length) {
        length = Mathf.Max(1, length);
        if (!stickCounts.TryGetValue(length, out int count) || count <= 0)
            return false;
        count--;
        if (count <= 0) stickCounts.Remove(length);
        else stickCounts[length] = count;
        return true;
    }
}
