using UnityEngine;

public class FunctionBall : Ball {
    public int unlockStickLength;
    
    void OnConnectionUpdate() {
        if(connectedToInitial) 
            GameManager.Instance.UnlockStickLength(unlockStickLength);
    }
}
