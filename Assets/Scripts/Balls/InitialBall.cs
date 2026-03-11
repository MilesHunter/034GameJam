using UnityEngine;

// 初始固定连接球：永久有效，作为玩家初始连接点
public class InitialBall : Ball {
    void Awake() {
        isFixed = true;
        connectedToInitial = true;
    }

    // 初始球本身不可被删除，不可被移动
}
