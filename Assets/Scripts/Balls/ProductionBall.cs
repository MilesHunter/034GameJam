using UnityEngine;
using System.Collections;

// 生产类固定连接球：与初始球连通时定期产出球和连接棒
public class ProductionBall : Ball {
    [Header("Production Settings")]
    public float productionInterval = 5f;
    public int ballOutput = 2;
    public int stickOutput = 3;

    Coroutine productionCoroutine;

    public override void OnConnectedToInitialChanged(bool newValue) {
        base.OnConnectedToInitialChanged(newValue);

        // 视觉状态：连接=亮黄，未连接=暗黄灰
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = newValue ? Color.yellow : new Color(0.5f, 0.5f, 0.2f);

        if (newValue && productionCoroutine == null) {
            productionCoroutine = StartCoroutine(ProductionRoutine());
        } else if (!newValue && productionCoroutine != null) {
            StopCoroutine(productionCoroutine);
            productionCoroutine = null;
        }
    }

    IEnumerator ProductionRoutine() {
        while (connectedToInitial) {
            yield return new WaitForSeconds(productionInterval);
            if (connectedToInitial && GameManager.Instance != null) {
                GameManager.Instance.AddAllocatableBalls(ballOutput);
                GameManager.Instance.AddGenericSticks(stickOutput);
                Debug.Log($"[ProductionBall] 产出 {ballOutput} 球, {stickOutput} 棒");
            }
        }
        productionCoroutine = null;
    }
}
