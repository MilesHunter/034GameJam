using UnityEngine;
using System.Collections;

public class DisasterSystem : MonoBehaviour {
    [SerializeField] float disasterInterval = 60f;
    [SerializeField] GameObject[] disasterPrefabs;
    [SerializeField] float spawnDistance = 15f;
    [SerializeField] float spawnRange = 10f;

    IEnumerator Start() {
        while(true) {
            yield return new WaitForSeconds(disasterInterval);
            TriggerRandomDisaster();
        }
    }

    void TriggerRandomDisaster() {
        if(disasterPrefabs.Length == 0) return;
        
        int type = Random.Range(0, disasterPrefabs.Length);
        Vector2 spawnPos = GetSpawnPosition();
        Instantiate(disasterPrefabs[type], spawnPos, Quaternion.identity);
    }

    // 新增的GetSpawnPosition方法
    Vector2 GetSpawnPosition() {
        Camera cam = Camera.main;
        if(!cam) return Vector2.zero;

        // 计算屏幕外生成位置
        Vector2 viewportEdge = Random.value > 0.5f 
            ? new Vector2(Random.value, Random.Range(0, 2)) 
            : new Vector2(Random.Range(0, 2), Random.value);
        
        viewportEdge.y = viewportEdge.y > 1 ? 1.1f : -0.1f;
        viewportEdge.x = viewportEdge.x > 1 ? 1.1f : -0.1f;
        
        Vector2 worldPos = cam.ViewportToWorldPoint(viewportEdge);
        
        // 添加随机偏移
        worldPos += Random.insideUnitCircle * spawnRange;
        
        return worldPos;
    }
}
