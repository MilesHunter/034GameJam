using UnityEngine;
using System.Collections;

public class DisasterSystem : MonoBehaviour {
    [SerializeField] float disasterInterval = 30f;
    [SerializeField] float spawnRange = 3f;

    IEnumerator Start() {
        while (true) {
            yield return new WaitForSeconds(disasterInterval);
            SpawnMeteor();
        }
    }

    void SpawnMeteor() {
        Vector2 pos = GetOffScreenSpawnPosition();

        GameObject go = new GameObject("Meteor");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GameManager.MakeCircleSprite();
        sr.color = new Color(1f, 0.4f, 0.1f);
        sr.sortingOrder = 5;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = 5f;

        go.AddComponent<Meteor>();
        Debug.Log($"[DisasterSystem] 触发灾害: Meteor at {pos}");
    }

    Vector2 GetOffScreenSpawnPosition() {
        Camera cam = Camera.main;
        if (!cam) return Vector2.zero;

        int edge = Random.Range(0, 4); // 0=上 1=下 2=左 3=右
        Vector2 viewportPos = edge switch {
            0 => new Vector2(Random.value, 1.1f),
            1 => new Vector2(Random.value, -0.1f),
            2 => new Vector2(-0.1f, Random.value),
            _ => new Vector2(1.1f, Random.value),
        };

        Vector2 worldPos = cam.ViewportToWorldPoint(viewportPos);
        worldPos += Random.insideUnitCircle * spawnRange;
        return worldPos;
    }
}
