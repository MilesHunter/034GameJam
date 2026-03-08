using System.Collections.Generic;
using UnityEngine;

public abstract class Ball : MonoBehaviour {
    [Header("Base Settings")]
    public bool isFixed;
    public bool connectedToInitial;
    public int connectionLimit = 24;
    public int currentConnections;
    public List<Stick> connectedSticks = new List<Stick>();
    
    public bool CanConnect() => currentConnections < connectionLimit;
    
    public void AddConnection(Stick stick) {
        currentConnections++;
        connectedSticks.Add(stick);
    }
    
    public void RemoveConnection(Stick stick) {
        currentConnections--;
        connectedSticks.Remove(stick);
    }
}
