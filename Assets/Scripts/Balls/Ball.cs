using System.Collections.Generic;
using UnityEngine;

public abstract class Ball : MonoBehaviour {
    [Header("Base Settings")]
    public bool isFixed;
    public bool connectedToInitial;
    public bool hasBeenConnected; // 是否曾经连接过（用于自动清理判断）
    public int connectionLimit = 24;
    public int currentConnections;
    public List<Stick> connectedSticks = new List<Stick>();

    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColor;

    void OnEnable() {
        EnsureRigidbody();
        ApplyRigidbodyDefaults();
        CacheRenderer();
        UpdateVisual();
    }

    void EnsureRigidbody() {
        if (rb != null) return;
        if (!TryGetComponent(out rb))
            rb = gameObject.AddComponent<Rigidbody2D>();
    }

    void ApplyRigidbodyDefaults() {
        if (rb == null) return;

        if (isFixed) {
            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        } else {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.None;
        }
    }

    void CacheRenderer() {
        if (sr != null) return;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            baseColor = sr.color;
    }

    public Rigidbody2D Rigidbody {
        get {
            EnsureRigidbody();
            return rb;
        }
    }

    public bool CanConnect() => currentConnections < connectionLimit;

    public void AddConnection(Stick stick) {
        if (connectedSticks.Contains(stick)) return;
        currentConnections++;
        hasBeenConnected = true;
        connectedSticks.Add(stick);
        IgnoreCollisionsWithOtherConnectedSticks(stick);
        GameManager.Instance?.OnConnectionChanged();
        UpdateVisual();
    }

    void IgnoreCollisionsWithOtherConnectedSticks(Stick stick) {
        if (stick == null) return;
        if (!stick.TryGetComponent(out Collider2D stickCol)) return;

        foreach (Stick other in connectedSticks) {
            if (other == null || other == stick) continue;
            if (!other.TryGetComponent(out Collider2D otherCol)) continue;
            Physics2D.IgnoreCollision(stickCol, otherCol, true);
        }
    }

    public void RemoveConnection(Stick stick) {
        if (!connectedSticks.Remove(stick)) return;
        currentConnections--;
        GameManager.Instance?.OnConnectionChanged();
        UpdateVisual();
    }

    /// <summary>
    /// 返回除指定 Stick 之外，本球还连接了多少根其它 Stick。
    /// 用于判定某个端点是否已经参与了更大的结构。
    /// </summary>
    public int GetOtherStickCount(Stick self) {
        int count = 0;
        for (int i = 0; i < connectedSticks.Count; i++) {
            Stick s = connectedSticks[i];
            if (s == null || s == self) continue;
            count++;
        }
        return count;
    }

    /// <summary>
    /// 在以当前球为中心的坐标系中，计算指定 Stick 的方向角（度）。
    /// 约定：从本球指向 Stick 另一端的方向作为角度参考。
    /// </summary>
    public float GetStickAngleOnThisBall(Stick stick) {
        if (stick == null)
            return 0f;

        Vector2 center = transform.position;

        // 找出与本球相连的端点，以及另一端的位置
        Transform otherEnd = null;
        if (stick.endpointA == this) {
            otherEnd = stick.EndB;
        } else if (stick.endpointB == this) {
            otherEnd = stick.EndA;
        }

        Vector2 dir;
        if (otherEnd != null) {
            dir = (Vector2)otherEnd.position - center;
        } else {
            // 兜底：若未能识别端点，则使用棒子中心方向
            dir = (Vector2)stick.transform.position - center;
        }

        if (dir.sqrMagnitude < 0.0001f)
            return 0f;

        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 判断以 candidateAngleDeg 为方向的新棒子或现有棒子，
    /// 与本球上其它已连接棒子之间的最小夹角是否大于给定阈值。
    /// ignoreStick 用于在旋转自身时忽略当前这根棒子。
    /// </summary>
    public bool IsAngleAvailable(float candidateAngleDeg, Stick ignoreStick, float minDeltaDeg = 15f) {
        for (int i = 0; i < connectedSticks.Count; i++) {
            Stick s = connectedSticks[i];
            if (s == null || s == ignoreStick) continue;

            float existingAngle = GetStickAngleOnThisBall(s);
            float delta = Mathf.Abs(Mathf.DeltaAngle(existingAngle, candidateAngleDeg));
            if (delta < minDeltaDeg)
                return false;
        }

        return true;
    }

    // 由 GameManager 在 BFS 连通性检查后调用
    public virtual void OnConnectedToInitialChanged(bool newValue) {
        connectedToInitial = newValue;
        UpdateVisual();
    }

    void UpdateVisual() {
        if (sr == null) return;
        Color c = baseColor;

        if (currentConnections == 0)
            c *= 0.7f;
        else if (currentConnections == 1)
            c *= 1.0f;
        else
            c *= 1.2f;

        sr.color = c;
    }
}
