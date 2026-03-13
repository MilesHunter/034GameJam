using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图中的物理墙：
/// - 使用 PolygonCollider2D 描述形状；
/// - 自动生成填充 Mesh，使多边形区域在 Game 视图中完全实心可见；
/// - 可选地阻止在墙内部放置结构（棒子等）。
/// 
/// 用法：
/// 1. 在场景中创建一个空物体，添加 MapWall 组件；
/// 2. 确保物体上有 PolygonCollider2D（Reset 会自动补齐）；
/// 3. （推荐）把物体的 Layer 设置为 "MapWall"，用于放置合法性检测；
/// 4. 调整 collider 点即可自由绘制墙体形状；
/// 5. MapWall 会根据 PolygonCollider2D 自动生成填充 Mesh。 
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class MapWall : MonoBehaviour {
    [Header("Blocking")]
    [Tooltip("是否阻止在墙内部放置棒子等建筑结构。")]
    public bool blocksBuilding = true;

    [Header("Visual")]
    [Tooltip("墙体填充颜色。修改后会自动更新 Mesh 材质颜色。")]
    public Color fillColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Gizmo")]
    [Tooltip("在 Scene 视图中绘制轮廓，方便编辑。")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(1f, 1f, 1f, 0.25f);

    static int wallLayerMask = -1;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh mesh;

    void Reset() {
        // 确保存在多边形碰撞体，并默认作为实体墙
        var poly = GetComponent<PolygonCollider2D>();
        if (poly == null)
            poly = gameObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = false;

        // 如果项目中存在 "MapWall" Layer，则自动应用，方便统一管理
        int layer = LayerMask.NameToLayer("MapWall");
        if (layer != -1)
            gameObject.layer = layer;

        EnsureRenderComponents();
        RebuildMesh();
    }

    void Awake() {
        EnsureRenderComponents();
        RebuildMesh();
    }

    void OnValidate() {
#if UNITY_EDITOR
        // 在编辑器中修改 collider 或颜色时，延迟一帧重建 Mesh，
        // 避免在 OnValidate 调用链中触发 Unity 内部的 SendMessage 限制。
        UnityEditor.EditorApplication.delayCall += () => {
            if (this == null) return;
            EnsureRenderComponents();
            RebuildMesh();
            UpdateMaterialColor();
        };
#endif
    }

    void EnsureRenderComponents() {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (meshRenderer == null)
            return;

        var currentMat = meshRenderer.sharedMaterial;
        if (currentMat == null) {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) {
                var mat = new Material(shader) { name = "MapWall_Material" };
                mat.color = fillColor;
                meshRenderer.sharedMaterial = mat;
            }
        }
    }

    void UpdateMaterialColor() {
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            meshRenderer.sharedMaterial.color = fillColor;
    }

    void RebuildMesh() {
        var poly = GetComponent<PolygonCollider2D>();
        if (poly == null) {
            if (meshFilter != null)
                meshFilter.sharedMesh = null;
            return;
        }

        var points = poly.points;
        if (points == null || points.Length < 3) {
            if (meshFilter != null)
                meshFilter.sharedMesh = null;
            return;
        }

        if (mesh == null) {
            mesh = new Mesh { name = "MapWall_Mesh" };
        }

        // 顶点：使用 collider 的局部坐标 + offset
        int count = points.Length;
        var vertices = new Vector3[count];
        for (int i = 0; i < count; i++) {
            Vector2 p = points[i] + poly.offset;
            vertices[i] = new Vector3(p.x, p.y, 0f);
        }

        int[] triangles = Triangulate(points);

        // UV：简单按包围盒归一化，方便使用任意贴图/纯色
        var uvs = new Vector2[count];
        if (count > 0) {
            float minX = vertices[0].x, maxX = vertices[0].x;
            float minY = vertices[0].y, maxY = vertices[0].y;
            for (int i = 1; i < count; i++) {
                Vector3 v = vertices[i];
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }
            float sizeX = Mathf.Max(0.0001f, maxX - minX);
            float sizeY = Mathf.Max(0.0001f, maxY - minY);
            for (int i = 0; i < count; i++) {
                Vector3 v = vertices[i];
                uvs[i] = new Vector2((v.x - minX) / sizeX, (v.y - minY) / sizeY);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (meshFilter != null)
            meshFilter.sharedMesh = mesh;
    }

    static int[] Triangulate(Vector2[] polyPoints) {
        int n = polyPoints.Length;
        if (n < 3)
            return System.Array.Empty<int>();

        var indices = new List<int>(n);
        if (Area(polyPoints) > 0f) {
            for (int i = 0; i < n; i++) indices.Add(i);
        } else {
            for (int i = 0; i < n; i++) indices.Add((n - 1) - i);
        }

        int nv = n;
        int count = 2 * nv;
        var result = new List<int>();
        int v = nv - 1;

        while (nv > 2) {
            if ((count--) <= 0)
                break; // 避免异常多边形导致死循环

            int u = v;
            if (u >= nv) u = 0;
            v = u + 1;
            if (v >= nv) v = 0;
            int w = v + 1;
            if (w >= nv) w = 0;

            if (Snip(polyPoints, indices, u, v, w, nv)) {
                int a = indices[u];
                int b = indices[v];
                int c = indices[w];
                result.Add(a);
                result.Add(b);
                result.Add(c);
                indices.RemoveAt(v);
                nv--;
                count = 2 * nv;
            }
        }

        return result.ToArray();
    }

    static float Area(Vector2[] points) {
        int n = points.Length;
        float a = 0f;
        for (int p = n - 1, q = 0; q < n; p = q++) {
            Vector2 pVal = points[p];
            Vector2 qVal = points[q];
            a += pVal.x * qVal.y - qVal.x * pVal.y;
        }
        return a * 0.5f;
    }

    static bool Snip(Vector2[] points, List<int> indices, int u, int v, int w, int nv) {
        const float EPS = 1e-5f;
        int a = indices[u];
        int b = indices[v];
        int c = indices[w];

        Vector2 A = points[a];
        Vector2 B = points[b];
        Vector2 C = points[c];

        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;

        for (int p = 0; p < nv; p++) {
            if (p == u || p == v || p == w) continue;
            int idx = indices[p];
            Vector2 P = points[idx];
            if (InsideTriangle(A, B, C, P)) return false;
        }
        return true;
    }

    static bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
        float ax = C.x - B.x; float ay = C.y - B.y;
        float bx = A.x - C.x; float by = A.y - C.y;
        float cx = B.x - A.x; float cy = B.y - A.y;

        float apx = P.x - A.x; float apy = P.y - A.y;
        float bpx = P.x - B.x; float bpy = P.y - B.y;
        float cpx = P.x - C.x; float cpy = P.y - C.y;

        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;

        return (aCROSSbp >= 0f) && (bCROSScp >= 0f) && (cCROSSap >= 0f);
    }

    /// <summary>
    /// 检查给定世界坐标点是否位于任意阻止建造的墙体内部/重叠区域。
    /// 依赖 Layer "MapWall"；若未配置该 Layer，则始终返回 false（不阻挡）。
    /// </summary>
    public static bool IsPointBlocked(Vector2 worldPosition) {
        if (wallLayerMask == -1)
            wallLayerMask = LayerMask.GetMask("MapWall");

        // 如果项目里还没创建 "MapWall" 这个 Layer，则不进行阻挡判断
        if (wallLayerMask == 0)
            return false;

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition, wallLayerMask);
        foreach (var col in hits) {
            if (col == null) continue;
            var wall = col.GetComponent<MapWall>();
            if (wall != null && wall.blocksBuilding)
                return true;
        }

        return false;
    }

    void OnDrawGizmos() {
        if (!drawGizmos)
            return;

        var poly = GetComponent<PolygonCollider2D>();
        if (poly == null)
            return;

        Gizmos.color = gizmoColor;

        // PolygonCollider2D 的点在局部坐标下，需要转换到世界空间
        var points = poly.points;
        if (points == null || points.Length == 0)
            return;

        Vector3 offset = transform.TransformPoint(poly.offset);
        for (int i = 0; i < points.Length; i++) {
            Vector3 a = offset + (Vector3)points[i];
            Vector3 b = offset + (Vector3)points[(i + 1) % points.Length];
            Gizmos.DrawLine(a, b);
        }
    }
}
