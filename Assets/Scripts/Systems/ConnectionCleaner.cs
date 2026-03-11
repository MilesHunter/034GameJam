using UnityEngine;

// 清理孤立对象：无连接的可分配球（棒子自己通过 destroyBelowY 管理生命周期）
public class ConnectionCleaner : MonoBehaviour {
    [SerializeField] float cleanInterval = 0.5f;

    float timer;

    void Update() {
        timer += Time.deltaTime;
        if (timer < cleanInterval) return;
        timer = 0f;
        CleanOrphanedObjects();
    }

    void CleanOrphanedObjects() {
        // 清理无连接且曾经放置过的可分配球
        foreach (AllocatableBall ball in FindObjectsByType<AllocatableBall>(FindObjectsSortMode.None)) {
            if (ball.currentConnections == 0 && ball.hasBeenConnected)
                Destroy(ball.gameObject);
        }
    }
}
