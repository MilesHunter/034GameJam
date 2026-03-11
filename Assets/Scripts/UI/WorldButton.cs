using UnityEngine;
using System;

// 世界空间可点击按钮（无文字，仅形状颜色区分功能）
// 由 InteractionManager 通过 OverlapPoint 射线检测触发
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class WorldButton : MonoBehaviour {
    public Action OnClick;

    // 由 InteractionManager 调用，不使用 OnMouseDown 避免冲突
    public void Click() => OnClick?.Invoke();
}
