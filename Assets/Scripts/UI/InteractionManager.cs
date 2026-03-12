using UnityEngine;

public class InteractionManager : MonoBehaviour {
    public static InteractionManager Instance;

    enum BuildTool { Stick, Ball, Select }

    BuildTool currentTool = BuildTool.Stick;
    int currentStickLength = 2;

    Stick stickBeingPlaced;

    Stick pendingStick;
    AllocatableBall pendingBall;
    Vector2 pendingMouseDownWorld;
    bool pendingLeftDown;

    Stick manipulatingStick;
    bool isManipulating;

    AllocatableBall selectedBall;
    Stick selectedStick;

    public bool IsPlacingStick => stickBeingPlaced != null;
    public bool IsManipulatingStick => isManipulating;

    public int CurrentStickLength => currentStickLength;

    void Awake() {
        Instance = this;
    }

    public void SetStickTool(int length) {
        currentTool = BuildTool.Stick;
        currentStickLength = Mathf.Max(1, length);
        ClearSelection();
    }

    public void SetBallTool() {
        currentTool = BuildTool.Ball;
        ClearSelection();
    }

    public void SetSelectTool() {
        currentTool = BuildTool.Select;
    }

    void Update() {
        if (GameManager.Instance != null) {
            if (GameManager.Instance.IsGamePaused)
                return;
            if (GameManager.Instance.Phase != GameManager.GamePhase.Build)
                return;
        }

        if (Input.GetKeyDown(KeyCode.X)) {
            TryDeleteSelection();
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
                    // 在确认放置前，先检查是否与地图墙体冲突
                    if (s.IsPlacementBlockedByWalls()) {
                        // 视为本次放置无效：销毁棒子并归还库存
                        s.CancelPlacement();
                        GameManager.Instance?.ReturnStick();
                        return;
                    }

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
        pendingBall = null;
        foreach (var col in hits) {
            if (pendingStick == null) pendingStick = col.GetComponent<Stick>();
            if (pendingBall == null) pendingBall = col.GetComponent<AllocatableBall>();
        }
    }

    void UpdatePendingLeftPointer() {
        if (Input.GetMouseButtonUp(0)) {
            pendingLeftDown = false;
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            if (currentTool == BuildTool.Ball) {
                TryInstallBallAtPosition(pendingMouseDownWorld);
                return;
            }

            if (currentTool == BuildTool.Select) {
                if (pendingStick != null) {
                    SetSelection(pendingStick);
                    return;
                }
                if (pendingBall != null) {
                    SetSelection(pendingBall);
                    return;
                }
                ClearSelection();
                return;
            }

            // Stick 工具：点击棒=操作棒；否则以最近球为锚点生成新棒
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

            Ball anchor = gm.FindNearestBall(pendingMouseDownWorld);
            if (anchor == null)
                return;

            Stick s = gm.SpawnStick(anchor, currentStickLength);
            if (s != null)
                stickBeingPlaced = s;
            return;
        }

        if (pendingStick == null) return;

        Vector2 now = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if ((now - pendingMouseDownWorld).sqrMagnitude < 0.02f)
            return;

        pendingLeftDown = false;

        if (currentTool != BuildTool.Stick)
            return;

        manipulatingStick = pendingStick;
        if (manipulatingStick.CanDrag) {
            manipulatingStick.BeginDrag(pendingMouseDownWorld);
            isManipulating = true;
        } else if (manipulatingStick.CanRotate) {
            manipulatingStick.BeginRotate();
            isManipulating = true;
        }
    }

    void TryInstallBallAtPosition(Vector2 mouseWorld) {
        var gm = GameManager.Instance;
        if (gm == null)
            return;

        float maxDistSqr = 0.16f; // 半径约 0.4
        Stick bestStick = null;
        Transform bestEnd = null;

        foreach (Stick stick in FindObjectsByType<Stick>(FindObjectsSortMode.None)) {
            if (stick == null) continue;
            foreach (Transform end in stick.GetFreeEnds()) {
                if (end == null) continue;
                float d2 = ((Vector2)end.position - mouseWorld).sqrMagnitude;
                if (d2 < maxDistSqr) {
                    maxDistSqr = d2;
                    bestStick = stick;
                    bestEnd = end;
                }
            }
        }

        if (bestStick == null || bestEnd == null)
            return;

        gm.TryInstallBallOnStickFreeEnd(bestStick);
    }

    void TryDeleteSelection() {
        if (stickBeingPlaced != null || isManipulating)
            return;

        var gm = GameManager.Instance;

        if (selectedBall != null) {
            if (gm != null)
                gm.ReturnAllocatableBall();
            selectedBall.Delete();
            ClearSelection();
            return;
        }

        if (selectedStick != null) {
            if (gm != null)
                gm.ReturnStick();
            selectedStick.Delete();
            ClearSelection();
        }
    }

    void SetSelection(AllocatableBall ball) {
        if (selectedBall != null) {
            var outline = selectedBall.GetComponent<SelectableOutline>();
            outline?.SetSelected(false);
        }
        if (selectedStick != null) {
            var outline = selectedStick.GetComponent<SelectableOutline>();
            outline?.SetSelected(false);
        }

        selectedBall = ball;
        selectedStick = null;
        if (ball == null) {
            ClearSelection();
            return;
        }
        var selectedOutline = ball.GetComponent<SelectableOutline>();
        selectedOutline?.SetSelected(true);
    }

    void SetSelection(Stick stick) {
        if (selectedBall != null) {
            var outline = selectedBall.GetComponent<SelectableOutline>();
            outline?.SetSelected(false);
        }
        if (selectedStick != null) {
            var outline = selectedStick.GetComponent<SelectableOutline>();
            outline?.SetSelected(false);
        }

        selectedStick = stick;
        selectedBall = null;
        if (stick == null) {
            ClearSelection();
            return;
        }
        var selectedOutline = stick.GetComponent<SelectableOutline>();
        selectedOutline?.SetSelected(true);
    }

    void ClearSelection() {
        if (selectedBall != null) {
            var outlineBall = selectedBall.GetComponent<SelectableOutline>();
            outlineBall?.SetSelected(false);
        }

        if (selectedStick != null) {
            var outlineStick = selectedStick.GetComponent<SelectableOutline>();
            outlineStick?.SetSelected(false);
        }

        selectedBall = null;
        selectedStick = null;
    }
}
