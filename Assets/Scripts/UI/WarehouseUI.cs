using UnityEngine;
using System.Collections.Generic;

public class WarehouseUI : MonoBehaviour {
    public static WarehouseUI Instance;

    [SerializeField] float slotWidth = 1.5f;
    [SerializeField] float slotHeight = 0.3f;
    [SerializeField] float slotSpacing = 0.2f;
    [SerializeField] float rowY = 3f;

    readonly List<WarehouseSlot> slots = new List<WarehouseSlot>();

    public bool IsOpen { get; private set; }

    void Awake() {
        Instance = this;
    }

    public void Open() {
        if (IsOpen) return;
        IsOpen = true;
        RebuildSlots();
    }

    public void Close() {
        if (!IsOpen) return;
        IsOpen = false;
        ClearSlots();
    }

    void ClearSlots() {
        foreach (var slot in slots) {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slots.Clear();
    }

    void RebuildSlots() {
        ClearSlots();
        var gm = GameManager.Instance;
        if (gm == null) return;

        int index = 0;
        foreach (var kv in gm.GetWarehouseSnapshot()) {
            if (kv.Value <= 0) continue;

            float x = index * (slotWidth + slotSpacing);
            Vector3 pos = new Vector3(x, rowY, 0f);

            GameObject slotGo = new GameObject($"WarehouseSlot_L{kv.Key}");
            slotGo.transform.SetParent(transform, false);
            slotGo.transform.position = pos;

            SpriteRenderer sr = slotGo.AddComponent<SpriteRenderer>();
            sr.sprite = GameManager.MakeRectSprite();
            sr.color = new Color(0.4f, 0.7f, 0.9f);
            sr.sortingOrder = 10;
            slotGo.transform.localScale = new Vector3(slotWidth * kv.Key * 0.25f, slotHeight, 1f);

            BoxCollider2D col = slotGo.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            WarehouseSlot slot = slotGo.AddComponent<WarehouseSlot>();
            slot.stickLength = kv.Key;
            slots.Add(slot);

            index++;
        }
    }

    public bool TryHandleClick(Vector2 worldPos) {
        if (!IsOpen) return false;

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        foreach (var hit in hits) {
            WarehouseSlot slot = hit.GetComponent<WarehouseSlot>();
            if (slot == null) continue;

            var gm = GameManager.Instance;
            if (gm == null) return false;

            Stick stick;
            if (gm.TryWithdrawStick(slot.stickLength, worldPos, out stick)) {
                RebuildSlots();
                return true;
            }
        }

        return false;
    }
}
