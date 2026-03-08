using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;
    public Dictionary<int, int> availableStickLengths; // 可用连接棒长度
    public int allocatableBallCount; // 可分配球数量
    public int stickCount; // 连接棒数量
    
    void Awake() => Instance = this;
    
    public void UnlockStickLength(int length) {
        if(!availableStickLengths.ContainsKey(length))
            availableStickLengths.Add(length, 0);
        availableStickLengths[length]++;
    }
}
