using UnityEngine;

// 功能类固定连接球：与初始球连通时解锁新的连接棒长度，断开后依然生效
public class FunctionBall : Ball {
    [Header("Function Settings")]
    public int unlockStickLength = 4;

    bool effectApplied = false;

    public override void OnConnectedToInitialChanged(bool newValue) {
        base.OnConnectedToInitialChanged(newValue);

        // 视觉状态：连接=亮青色，未连接=暗灰青
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = newValue ? new Color(0.2f, 0.9f, 0.9f) : new Color(0.3f, 0.5f, 0.5f);

        if (newValue && !effectApplied) {
            effectApplied = true;
            GameManager.Instance?.UnlockStickLength(unlockStickLength);
            Debug.Log($"[FunctionBall] 解锁连接棒长度: {unlockStickLength}");
        }
    }
}
