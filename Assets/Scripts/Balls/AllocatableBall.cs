using UnityEngine;
using System.Collections.Generic;

public class AllocatableBall : Ball
{
    public void Delete()
    {
        // 断开所有连接后销毁
        foreach (Stick stick in connectedSticks) {
            if (stick.endpointA == this) stick.endpointA = null;
            if (stick.endpointB == this) stick.endpointB = null;
        }
        Destroy(gameObject);
    }
}
