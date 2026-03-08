using UnityEngine;
using System.Collections;

public class ProductionBall : Ball {
    public float productionInterval = 5f;
    public int ballOutput = 2;
    public int stickOutput = 3;

    IEnumerator ProductionRoutine() {
        while(connectedToInitial) {
            yield return new WaitForSeconds(productionInterval);
            GameManager.Instance.allocatableBallCount += ballOutput;
            GameManager.Instance.stickCount += stickOutput;
        }
    }
}
