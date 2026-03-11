using UnityEngine;

// 陨石：飞向场景中心区域，冲撞建筑
[RequireComponent(typeof(Rigidbody2D))]
public class Meteor : MonoBehaviour {
    [SerializeField] float speed = 8f;
    [SerializeField] float lifetime = 15f;

    void Start() {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // 自身控制运动方向
        // 飞向场景中心附近的随机点
        Vector2 target = Random.insideUnitCircle * 5f;
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.velocity = dir * speed;
        // 朝飞行方向旋转
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Destroy(gameObject, lifetime);
    }
}
