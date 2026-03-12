using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SelectableOutline : MonoBehaviour {
    [SerializeField] Color outlineColor = new Color(1f, 1f, 0.2f, 1f);
    [SerializeField, Range(0.0f, 0.1f)] float outlineSize = 0.03f;

    SpriteRenderer spriteRenderer;
    Material baseMaterial;
    Material outlineMaterial;

    static Shader outlineShader;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            baseMaterial = spriteRenderer.sharedMaterial;
    }

    void OnDestroy() {
        if (outlineMaterial != null) {
            Destroy(outlineMaterial);
            outlineMaterial = null;
        }
    }

    public void SetSelected(bool selected) {
        if (spriteRenderer == null)
            return;

        if (!selected) {
            if (baseMaterial != null)
                spriteRenderer.material = baseMaterial;
            return;
        }

        if (outlineShader == null) {
            outlineShader = Shader.Find("Custom/SpriteOutline");
            if (outlineShader == null) {
                Debug.LogWarning("[SelectableOutline] Shader 'Custom/SpriteOutline' not found. Outline will be skipped.");
                return;
            }
        }

        if (outlineMaterial == null) {
            outlineMaterial = new Material(outlineShader) {
                name = "SelectableOutline_Material"
            };
        }

        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineSize", outlineSize);

        spriteRenderer.material = outlineMaterial;
    }
}
