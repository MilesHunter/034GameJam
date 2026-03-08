using UnityEngine;

public class ConnectionCleaner : MonoBehaviour {
    void Update() {
        CleanOrphanedObjects();
    }

    void CleanOrphanedObjects() {
        // 清理未连接的分配球
        foreach(AllocatableBall ball in FindObjectsOfType<AllocatableBall>()) {
            if(ball.currentConnections == 0) 
                Destroy(ball.gameObject);
        }

        // 清理两端未连接的棒
        foreach(Stick stick in FindObjectsOfType<Stick>()) {
            if(!stick.endpointA && !stick.endpointB) 
                Destroy(stick.gameObject);
        }
    }
}
