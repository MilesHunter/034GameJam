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
