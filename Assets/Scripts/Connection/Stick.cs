using UnityEngine;

// 连接棒：受重力，两端磁性吸引附近连接球
// 放置模式：kinematic跟随鼠标，左键落地后物理接管
[RequireComponent(typeof(Rigidbody2D))]
public class Stick : MonoBehaviour {
    [Header("References")]
    public Rigidbody2D rb;
    [SerializeField] Transform endA;
    [SerializeField] Transform endB;

    [Header("Attraction Settings")]
    [SerializeField] float attractionRadius = 1f;
    [SerializeField] float connectionThreshold = 0.3f;
    [SerializeField] float jointBreakForce = 1000f;

    [Header("Cleanup")]
    [SerializeField] float destroyBelowY = -20f;

    public Ball endpointA;
    public Ball endpointB;

    SpriteRenderer sr;
    Color baseColor;

    public Transform EndA => endA;
    public Transform EndB => endB;

    public bool IsEndFree(Transform end) {
        if (end == null) return false;
        if (end == endA) return endpointA == null;
        if (end == endB) return endpointB == null;
        return false;
    }

    public Transform[] GetFreeEnds() {
        bool aFree = endA != null && endpointA == null;
        bool bFree = endB != null && endpointB == null;
        if (aFree && bFree) return new[] { endA, endB };
        if (aFree) return new[] { endA };
        if (bFree) return new[] { endB };
        return System.Array.Empty<Transform>();
    }

    public bool AttachAtEnd(Transform end, Ball ball) {
        if (end == null || ball == null) return false;
        if (end == endA) {
            if (endpointA != null) return false;
            AttachEndpointToBall(isA: true, ball: ball);
            return true;
        }
        if (end == endB) {
            if (endpointB != null) return false;
            AttachEndpointToBall(isA: false, ball: ball);
            return true;
        }
        return false;
    }

    // 哪一端是自由端（用于菜单中"添加球"功能）
    public Transform FreeEnd {
        get {
            if (endpointA == null && endA != null) return endA;
            if (endpointB == null && endB != null) return endB;
            return null;
        }
    }

    public void AttachToFreeEnd(Ball ball) {
        if (ball == null) return;
        if (endpointA == null && endA != null) { AttachEndpointToBall(isA: true, ball: ball); return; }
        if (endpointB == null && endB != null) { AttachEndpointToBall(isA: false, ball: ball); }
    }

    // 是否两端都已连接
    public bool FullyConnected => endpointA != null && endpointB != null;

    readonly Joint2D[] joints = new Joint2D[2];

    // ── 放置模式 ──────────────────────────────────────────────
    bool isBeingPlaced;
    Ball placementAnchor; // 已锚定的球（endA侧）

    Vector2 lastPlacementDir = Vector2.up;

    bool isManipulating;
    bool isRotating;
    Vector2 dragOffset;
    Ball rotateAnchor;
    bool rotateAnchorIsA;

    bool storedKinematic;
    float storedGravityScale;

    // 关节附近的体积阻挡：用于在物理模式下模拟多个棒子在同一球上互相“卡住”的效果。
    // 这里用一圈小的圆形碰撞体近似，挂在每一端对应的棒子上，让物理引擎自行处理挤压。
    const float pivotRingRadius = 0.3f;       // 小圆心到球心的大致半径
    const float pivotColliderRadius = 0.014f; // 小圆半径再缩小一半，让最小夹角进一步减小

    CircleCollider2D pivotColliderA;
    CircleCollider2D pivotColliderB;

    // 由InteractionManager或WorldButton调用
    public void Initialize(Transform endATransform, Transform endBTransform) {
        endA = endATransform;
        endB = endBTransform;
    }

    void Awake() {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        CacheRenderer();
        UpdateVisual();
    }

    void OnEnable() {
        CacheRenderer();
        UpdateVisual();
    }

    void CacheRenderer() {
        if (sr != null) return;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            baseColor = sr.color;
    }

    // ----------------------------------------------------------------
    void Update() {
        if (transform.position.y < destroyBelowY) { Destroy(gameObject); return; }

        if (isBeingPlaced) {
            UpdatePlacementPose();
            return;
        }

        if (isManipulating) {
            UpdateManipulationPose();
            return;
        }
    }

    // ----------------------------------------------------------------
    // 物理帧：磁性吸引
    // （放置/拖拽/旋转状态下不自动吸附，以免干扰操作）
    // ----------------------------------------------------------------
    void FixedUpdate() {
        // 正在放置或手动操作时，不进行自动连接
        if (isBeingPlaced || isManipulating)
            return;

        // 对两端执行磁性吸引逻辑
        TryAttract(endA, ref endpointA, 0);
        TryAttract(endB, ref endpointB, 1);
    }

    // ----------------------------------------------------------------
    // 放置模式：棒子跟随鼠标，endA锚定到 placementAnchor
    // ----------------------------------------------------------------
    public void StartPlacement(Ball anchorBall) {
        isBeingPlaced = true;
        placementAnchor = anchorBall;
        rb.isKinematic = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (anchorBall != null)
            AttachEndpointToBall(isA: true, ball: anchorBall);

        Vector2 anchorPos = anchorBall != null
            ? (Vector2)anchorBall.transform.position
            : (Vector2)transform.position;
        Vector2 cursor = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 diff = cursor - anchorPos;
        if (diff.sqrMagnitude > 0.0001f)
            lastPlacementDir = diff.normalized;
        else
            lastPlacementDir = Vector2.right;
    }

    // 确认当前摆放姿态，但保持刚体仍处于 Build 模式下的 kinematic 状态
    public void ConfirmPlacementPose() {
        isBeingPlaced = false;
        placementAnchor = null;
    }

    /// <summary>
    /// 当前摆放姿态是否被地图中的墙体阻挡。
    /// 规则：如果任意端点位于阻止建造的 MapWall 内部，则视为非法放置。
    /// 如果场景中没有配置 MapWall（或没有 "MapWall" Layer），则始终视为合法。
    /// </summary>
    public bool IsPlacementBlockedByWalls() {
        // 若端点尚未初始化，则不做限制
        bool hasEndA = endA != null;
        bool hasEndB = endB != null;

        if (!hasEndA && !hasEndB)
            return false;

        // 依赖 MapWall 提供的静态检测方法
        if (hasEndA && MapWall.IsPointBlocked(endA.position))
            return true;

        if (hasEndB && MapWall.IsPointBlocked(endB.position))
            return true;

        return false;
    }

    public void FinishPlacement() {
        isBeingPlaced = false;
        placementAnchor = null;
        rb.isKinematic = false;
        rb.gravityScale = 1f;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void CancelPlacement() {
        FinishPlacement();
        if (endpointA != null) DisconnectBall(endpointA);
        if (endpointB != null) DisconnectBall(endpointB);
        // 交还库存由 InteractionManager 负责
        Destroy(gameObject);
    }

    void UpdatePlacementPose() {
        if (placementAnchor == null) { FinishPlacement(); return; }

        Vector2 cursor = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 anchor = placementAnchor.transform.position;
        Vector2 diff = cursor - anchor;

        float halfLen = transform.localScale.y * 0.5f;

        const float minDirDistance = 0.05f;
        Vector2 dir;
        if (diff.sqrMagnitude > minDirDistance * minDirDistance) {
            dir = diff.normalized;
            lastPlacementDir = dir;
        } else {
            dir = lastPlacementDir;
        }

        transform.position = (Vector3)(anchor + dir * halfLen);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public bool CanDrag => endpointA == null && endpointB == null;

    /// <summary>
    /// 是否允许当前棒子在建造阶段被旋转。
    /// 规则：
    /// - 至少有一端连接到球时才允许旋转；
    /// - 当两端都连接到球且两个球上都还有其它棒子时，不允许旋转；
    ///   其余情况（只有一端有球，或两端球中至多一个参与其它连接）允许旋转。
    /// </summary>
    public bool CanRotate {
        get {
            if (endpointA == null && endpointB == null)
                return false;

            int otherA = endpointA != null ? endpointA.GetOtherStickCount(this) : 0;
            int otherB = endpointB != null ? endpointB.GetOtherStickCount(this) : 0;

            if (endpointA != null && endpointB != null && otherA > 0 && otherB > 0)
                return false;

            return true;
        }
    }

    public void BeginDrag(Vector2 cursorWorld) {
        if (!CanDrag) return;
        BeginManipulationCommon();
        isRotating = false;
        dragOffset = (Vector2)transform.position - cursorWorld;
    }

    public void BeginRotate() {
        if (!CanRotate) return;
        BeginManipulationCommon();
        isRotating = true;

        // 根据两端球的连接情况选择旋转锚点：
        // - 若只有一端有球，则围绕该端旋转；
        // - 若两端都有球且只有一端参与其它连接，则围绕参与其它连接的一端旋转；
        // - 若两端都有球但都未参与其它连接，则默认围绕 A 端旋转。
        if (endpointA != null && endpointB == null) {
            rotateAnchor = endpointA;
            rotateAnchorIsA = true;
        } else if (endpointB != null && endpointA == null) {
            rotateAnchor = endpointB;
            rotateAnchorIsA = false;
        } else if (endpointA != null && endpointB != null) {
            int otherA = endpointA.GetOtherStickCount(this);
            int otherB = endpointB.GetOtherStickCount(this);

            if (otherA > 0 && otherB == 0) {
                rotateAnchor = endpointA;
                rotateAnchorIsA = true;
            } else if (otherB > 0 && otherA == 0) {
                rotateAnchor = endpointB;
                rotateAnchorIsA = false;
            } else {
                // 两端都没有其它连接或都一样多，默认围绕 A 端
                rotateAnchor = endpointA;
                rotateAnchorIsA = true;
            }
        }
    }

    public void EndManipulation() {
        if (!isManipulating) return;
        isManipulating = false;
        isRotating = false;
        rotateAnchor = null;

        rb.isKinematic = storedKinematic;
        rb.gravityScale = storedGravityScale;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void BeginRotateFromAnchor(Ball anchor) {
        if (anchor == null || !CanRotate) return;
        BeginManipulationCommon();
        isRotating = true;
        rotateAnchor = anchor;
        rotateAnchorIsA = (endpointA == anchor);
    }

    void BeginManipulationCommon() {
        isManipulating = true;
        storedKinematic = rb.isKinematic;
        storedGravityScale = rb.gravityScale;

        rb.isKinematic = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void UpdateManipulationPose() {
        Vector2 cursor = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!isRotating) {
            transform.position = (Vector3)(cursor + dragOffset);
            return;
        }

        if (rotateAnchor == null) {
            EndManipulation();
            return;
        }

        Vector2 anchor = rotateAnchor.transform.position;
        Vector2 dir = rotateAnchorIsA ? (anchor - cursor) : (cursor - anchor);
        if (dir.sqrMagnitude < 0.0025f)
            return;
        dir.Normalize();

        float anchorLocalY = rotateAnchorIsA ? 0.5f : -0.5f;
        float anchorOffset = anchorLocalY * transform.localScale.y;
        // 先根据鼠标计算候选方向与角度，并暂时应用到 Transform，
        // 再通过 Ball.GetStickAngleOnThisBall 计算与其它棒子的真实夹角，
        // 若不满足最小 15 度要求，则回滚到上一帧姿态。
        float directionAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        transform.position = (Vector3)(anchor - dir * anchorOffset);
        float angle = directionAngle - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Ball anchorBall = rotateAnchor;
        if (anchorBall != null) {
            float candidateAngle = anchorBall.GetStickAngleOnThisBall(this);
            if (!anchorBall.IsAngleAvailable(candidateAngle, this, 15f)) {
                transform.position = oldPos;
                transform.rotation = oldRot;
                return;
            }
        }

        // 在建造模式下旋转时，如果另一端的球没有参与其它连接，
        // 让它跟随棒子一起转动，保持建筑逻辑上的刚性一体。
        Ball otherBall = null;
        Transform otherEnd = null;
        if (rotateAnchorIsA) {
            otherBall = endpointB;
            otherEnd = endB;
        } else {
            otherBall = endpointA;
            otherEnd = endA;
        }

        if (otherBall != null && otherEnd != null) {
            int otherConnections = otherBall.GetOtherStickCount(this);
            if (otherConnections == 0) {
                otherBall.transform.position = otherEnd.position;
            }
        }

    }

    // ----------------------------------------------------------------
    // 吸引与连接
    // ----------------------------------------------------------------
    void TryAttract(Transform end, ref Ball endpoint, int jointIndex) {
        if (end == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(end.position, attractionRadius, LayerMask.GetMask("Ball"));

        Ball closest = null;
        float minDist = float.MaxValue;

        foreach (var col in hits) {
            Ball ball = col.GetComponent<Ball>();
            if (ball == null || !ball.CanConnect()) continue;
            if (ball == endpointA || ball == endpointB) continue;

            float dist = Vector2.Distance(end.position, ball.transform.position);
            if (dist < minDist) { minDist = dist; closest = ball; }
        }

        if (closest == null) return;

        if (minDist < connectionThreshold) {
            EstablishConnection(end, ref endpoint, jointIndex, closest);
            return;
        }

        return;
    }

    void EstablishConnection(Transform end, ref Ball endpoint, int jointIndex, Ball ball) {
        if (end != null && ball != null) {
            // 角度限制：以“本球指向另一端”的方向作为角度参考，
            // 与 Ball.GetStickAngleOnThisBall 保持一致。
            Transform otherEnd = end == endA ? endB : endA;
            if (otherEnd != null) {
                Vector2 center = ball.transform.position;
                Vector2 dir = (Vector2)otherEnd.position - center;
                if (dir.sqrMagnitude > 0.0001f) {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    if (!ball.IsAngleAvailable(angle, this, 15f)) {
                        return; // 角度过小，拒绝建立新连接
                    }
                }
            }
        }

        endpoint = ball;
        ball.AddConnection(this);

        AlignEndAndBall(end, ball);
        CreateJointToBall(end, jointIndex, ball);
        SetupPivotColliderForEndpoint(end, ball);
        UpdateVisual();
    }

    void AttachEndpointToBall(bool isA, Ball ball) {
        if (ball == null) return;
        int idx = isA ? 0 : 1;
        if (joints[idx] != null) return;

        Transform thisEnd = isA ? endA : endB;
        Transform otherEndLocal = isA ? endB : endA;
        if (otherEndLocal != null) {
            Vector2 center = ball.transform.position;
            Vector2 dir = (Vector2)otherEndLocal.position - center;
            if (dir.sqrMagnitude > 0.0001f) {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (!ball.IsAngleAvailable(angle, this, 15f)) {
                    return; // 角度过小，拒绝建立新连接
                }
            }
        }

        if (isA) endpointA = ball; else endpointB = ball;
        ball.AddConnection(this);
        if (thisEnd == null) return;

        AlignEndAndBall(thisEnd, ball);
        CreateJointToBall(thisEnd, idx, ball);
        SetupPivotColliderForEndpoint(thisEnd, ball);
        UpdateVisual();
    }

    void SetupPivotColliderForEndpoint(Transform end, Ball ball) {
        if (end == null || ball == null)
            return;

        // 以“球心 → 此棒另一端”方向为基准，在球周围 pivotRingRadius 处放一个小圆。
        Transform otherEnd = end == endA ? endB : endA;
        if (otherEnd == null)
            return;

        Vector2 center = ball.transform.position;
        Vector2 dir = (Vector2)otherEnd.position - center;
        if (dir.sqrMagnitude < 0.0001f)
            return;
        dir.Normalize();

        Vector2 pivotWorld = center + dir * pivotRingRadius;

        bool isA = (end == endA);
        CircleCollider2D col = isA ? pivotColliderA : pivotColliderB;
        if (col == null) {
            GameObject go = new GameObject(isA ? "PivotColliderA" : "PivotColliderB");
            go.transform.SetParent(transform, worldPositionStays: false);
            col = go.AddComponent<CircleCollider2D>();
            col.radius = pivotColliderRadius;

            // 避免与本棒子的主碰撞体和当前球发生碰撞，只与其他棒子的 pivot 发生碰撞。
            if (TryGetComponent(out Collider2D mainCol)) {
                Physics2D.IgnoreCollision(col, mainCol, true);
            }
            if (ball.TryGetComponent(out Collider2D ballCol)) {
                Physics2D.IgnoreCollision(col, ballCol, true);
            }

            if (isA) pivotColliderA = col; else pivotColliderB = col;
        }

        // 放到正确的世界坐标上，再回写本地变换。
        col.transform.position = pivotWorld;
        col.enabled = true;
    }

    void CreateJointToBall(Transform end, int jointIndex, Ball ball) {
        Rigidbody2D connected = ball != null ? ball.Rigidbody : null;

        HingeJoint2D hinge = gameObject.AddComponent<HingeJoint2D>();
        hinge.connectedBody = connected;
        hinge.anchor = transform.InverseTransformPoint(end.position);
        hinge.autoConfigureConnectedAnchor = true;

        hinge.breakForce = jointBreakForce;
        hinge.breakTorque = jointBreakForce;
        hinge.enableCollision = false;
        joints[jointIndex] = hinge;
    }

    void AlignEndAndBall(Transform end, Ball ball) {
        if (end == null || ball == null) return;
        Vector2 ballPos = ball.transform.position;
        Vector2 endPos = end.position;
        Vector2 delta = ballPos - endPos;
        transform.position += (Vector3)delta;
    }

    float GetBallWorldRadius(Ball ball) {
        if (ball == null) return 0.5f;
        CircleCollider2D col = ball.GetComponent<CircleCollider2D>();
        float r = col != null ? col.radius : 0.5f;
        float s = ball.transform.lossyScale.x;
        return Mathf.Abs(r * s);
    }

    // ----------------------------------------------------------------
    // 关节断裂回调
    // ----------------------------------------------------------------
    void OnJointBreak2D(Joint2D brokenJoint) {
        for (int i = 0; i < joints.Length; i++) {
            if (joints[i] != brokenJoint) continue;
            Ball ball = i == 0 ? endpointA : endpointB;
            ball?.RemoveConnection(this);
            if (i == 0) endpointA = null; else endpointB = null;
            joints[i] = null;
            break;
        }
    }

    // ----------------------------------------------------------------
    // 公开接口
    // ----------------------------------------------------------------
    public void DisconnectBall(Ball ball) {
        if (ball == null) return;
        for (int i = 0; i < 2; i++) {
            Ball ep = i == 0 ? endpointA : endpointB;
            if (ep != ball) continue;
            ep.RemoveConnection(this);
            if (i == 0) endpointA = null; else endpointB = null;
            if (joints[i] != null) { Destroy(joints[i]); joints[i] = null; }
            break;
        }
        UpdateVisual();
    }

    // 删除棒子并断开所有连接（归还库存由外部负责）
    public void Delete() {
        DisconnectBall(endpointA);
        DisconnectBall(endpointB);
        Destroy(gameObject);
    }

    void UpdateVisual() {
        if (sr == null) return;
        int connected = 0;
        if (endpointA != null) connected++;
        if (endpointB != null) connected++;

        Color c = baseColor;
        if (connected == 0)
            c *= 0.7f;
        else if (connected == 1)
            c *= 1.0f;
        else
            c *= 1.4f; // 两端都连接的棒子颜色更深，便于一眼看出已被固定

        sr.color = c;
    }

    void OnDrawGizmosSelected() {
        if (endA) { Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(endA.position, attractionRadius); }
        if (endB) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(endB.position, attractionRadius); }
    }
}
