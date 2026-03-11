using UnityEngine;

// 摄像机控制器：WASD 或鼠标右键拖动视角
public class CameraController : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] float moveSpeed = 10f;

    [Header("Zoom")]
    [SerializeField] float zoomSpeed = 2f;
    [SerializeField] float minOrthoSize = 3f;
    [SerializeField] float maxOrthoSize = 30f;

    Camera cam;
    Vector3 dragOrigin;
    bool isDragging;

    void Awake() {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update() {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
            return;

        HandleKeyboardMove();
        HandleMouseDrag();
        HandleZoom();
    }

    void HandleKeyboardMove() {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S
        transform.Translate(new Vector3(h, v, 0) * moveSpeed * Time.unscaledDeltaTime);
    }

    void HandleMouseDrag() {
        if (Input.GetMouseButtonDown(1)) {
            // 放置棒子模式下右键取消放置，不启动摄像机拖拽
            if (InteractionManager.Instance != null && InteractionManager.Instance.IsPlacingStick)
                return;
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }
        if (Input.GetMouseButtonUp(1)) isDragging = false;

        if (isDragging && Input.GetMouseButton(1)) {
            Vector3 currentPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = dragOrigin - currentPos;
            transform.position += delta;
        }
    }

    void HandleZoom() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;
        if (cam.orthographic) {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minOrthoSize, maxOrthoSize);
        }
    }
}
