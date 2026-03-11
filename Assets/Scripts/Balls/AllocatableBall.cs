using UnityEngine;
using System.Collections.Generic;

public class AllocatableBall : Ball {
    public void Delete() {
        var sticksCopy = new List<Stick>(connectedSticks);
        foreach (Stick stick in sticksCopy) {
            stick.DisconnectBall(this);
        }
        Destroy(gameObject);
    }
}
