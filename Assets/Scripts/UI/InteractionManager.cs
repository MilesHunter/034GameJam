using UnityEngine;

public class InteractionManager : MonoBehaviour {
    public static InteractionManager Instance;

    Stick stickBeingPlaced;

    Stick pendingStick;
    Vector2 pendingMouseDownWorld;
    bool pendingLeftDown;

    Stick manipulatingStick;
    bool isManipulating;

    public bool IsPlacingStick => stickBeingPlaced != null;
    public bool IsManipulatingStick => isManipulating;

    void Awake() {
        Instance = this;
    }

    void Update() {
        if (GameManager.Instance != null) {
            if (GameManager.Instance.IsGamePaused)
                return;
            if (GameManager.Instance.Phase != GameManager.GamePhase.Build)
                return;
        }

        var warehouse = WarehouseUI.Instance;
        if (warehouse != null && warehouse.IsOpen && Input.GetMouseButtonDown(0)) {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (warehouse.TryHandleClick(mouseWorld))
                return;
        }

        if (stickBeingPlaced != null) {
            if (Input.GetMouseButtonDown(0)) {
                Stick s = stickBeingPlaced;
                stickBeingPlaced = null;

                if (s != null) {
                    s.ConfirmPlacementPose();

                    Ball anchor = s.endpointA != null ? s.endpointA : s.endpointB;
                    manipulatingStick = s;
                    if (anchor != null)
                        s.BeginRotateFromAnchor(anchor);
                    else if (s.CanRotate)
                        s.BeginRotate();
                    isManipulating = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                stickBeingPlaced.CancelPlacement();
                GameManager.Instance?.ReturnStick();
                stickBeingPlaced = null;
            }

            return;
        }

        if (isManipulating) {
            if (manipulatingStick == null) {
                isManipulating = false;
                return;
            }

            if (Input.GetMouseButtonUp(0)) {
                GameManager.Instance?.AutoSpawnBallOnStickFreeEnd(manipulatingStick);
                manipulatingStick.EndManipulation();
                manipulatingStick = null;
                isManipulating = false;
            }

            return;
        }

        if (Input.GetMouseButtonDown(1)) {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);

            Stick hitStick = null;
            foreach (var col in hits) {
                if (hitStick == null) hitStick = col.GetComponent<Stick>();
            }

            if (hitStick != null && !hitStick.FullyConnected) {
                GameManager.Instance?.StoreStick(hitStick);
                return;
            }

            if (hitStick == null && warehouse != null) {
                if (warehouse.IsOpen)
                    warehouse.Close();
                else
                    warehouse.Open();
                return;
            }
        }

        if (Input.GetMouseButtonDown(0))
            BeginLeftPointer();

        if (pendingLeftDown)
            UpdatePendingLeftPointer();
    }

    void BeginLeftPointer() {
        pendingLeftDown = true;
        pendingMouseDownWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 mouse = pendingMouseDownWorld;
        Collider2D[] hits = Physics2D.OverlapPointAll(mouse);

        pendingStick = null;
        foreach (var col in hits) {
            if (pendingStick == null) pendingStick = col.GetComponent<Stick>();
        }
    }

    void UpdatePendingLeftPointer() {
        if (Input.GetMouseButtonUp(0)) {
            pendingLeftDown = false;

            if (pendingStick != null) {
                manipulatingStick = pendingStick;
                if (manipulatingStick.CanDrag) {
                    manipulatingStick.BeginDrag(pendingMouseDownWorld);
                    isManipulating = true;
                } else if (manipulatingStick.CanRotate) {
                    manipulatingStick.BeginRotate();
                    isManipulating = true;
                }
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null)
                return;

            Ball anchor = gm.FindNearestBall(pendingMouseDownWorld);
            if (anchor == null)
                return;

            Stick s = gm.SpawnRandomStick(anchor);
            if (s != null)
                stickBeingPlaced = s;
            return;
        }

        if (pendingStick == null) return;

        Vector2 now = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if ((now - pendingMouseDownWorld).sqrMagnitude < 0.02f)
            return;

        pendingLeftDown = false;

        manipulatingStick = pendingStick;
        if (manipulatingStick.CanDrag) {
            manipulatingStick.BeginDrag(pendingMouseDownWorld);
            isManipulating = true;
        } else if (manipulatingStick.CanRotate) {
            manipulatingStick.BeginRotate();
            isManipulating = true;
        }
    }
}
