using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuickOutlinePro
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/Quick Outline Pro")]
    public sealed class QuickOutlinePro : MonoBehaviour
    {
        public enum HighlightMode { Off, Always, Hover, Click, HoverAndClick }
        public enum PipelineMode { Auto, BuiltIn, URP, HDRP }

        [Header("Behaviour")]
        [SerializeField] private HighlightMode mode = HighlightMode.Always;
        [SerializeField] private PipelineMode pipeline = PipelineMode.Auto;
        [SerializeField] private bool includeChildren = true;
        [SerializeField] private bool combineMeshes = false;
        [SerializeField] private bool requireColliderForInput = true;

        [Header("Style")]
        [ColorUsage(true, true)] [SerializeField] private Color outlineColor = new Color(0.0f, 0.65f, 1.0f, 1.0f);
        [SerializeField, Range(0.0f, 0.25f)] private float outlineWidth = 0.025f;
        [SerializeField, Range(0.0f, 8.0f)] private float glowIntensity = 1.5f;
        [SerializeField] private bool visible = true;

        private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int GlowId = Shader.PropertyToID("_GlowIntensity");

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private Material outlineMaterial;
        private Renderer combinedRenderer;
        private bool hovered;
        private bool clicked;
        private bool applied;

        public HighlightMode Mode { get => mode; set { mode = value; RefreshVisibility(); } }
        public Color OutlineColor { get => outlineColor; set { outlineColor = value; UpdateMaterialProperties(); } }
        public float OutlineWidth { get => outlineWidth; set { outlineWidth = Mathf.Max(0f, value); UpdateMaterialProperties(); } }
        public float GlowIntensity { get => glowIntensity; set { glowIntensity = Mathf.Max(0f, value); UpdateMaterialProperties(); } }
        public bool Visible { get => visible; set { visible = value; RefreshVisibility(); } }

        private void Awake()
        {
            CacheRenderers();
            EnsureInputCollider();
            CreateOutlineMaterial();
            RefreshVisibility();
        }

        private void OnEnable() => RefreshVisibility();
        private void OnDisable() => RemoveOutline();
        private void OnDestroy() => Cleanup();
        private void OnMouseEnter() { hovered = true; RefreshVisibility(); }
        private void OnMouseExit() { hovered = false; RefreshVisibility(); }
        private void OnMouseDown() { clicked = !clicked; RefreshVisibility(); }

        public void SetHighlighted(bool highlighted)
        {
            visible = highlighted;
            mode = highlighted ? HighlightMode.Always : HighlightMode.Off;
            RefreshVisibility();
        }

        public void SetColor(Color color) => OutlineColor = color;
        public void SetWidth(float width) => OutlineWidth = width;
        public void SetGlow(float intensity) => GlowIntensity = intensity;
        public void Rebuild() { RemoveOutline(); CacheRenderers(); CreateOutlineMaterial(); RefreshVisibility(); }

        private void RefreshVisibility()
        {
            if (!isActiveAndEnabled) return;
            bool shouldShow = visible && ShouldShowForMode();
            if (shouldShow) ApplyOutline(); else RemoveOutline();
        }

        private bool ShouldShowForMode()
        {
            switch (mode)
            {
                case HighlightMode.Always: return true;
                case HighlightMode.Hover: return hovered;
                case HighlightMode.Click: return clicked;
                case HighlightMode.HoverAndClick: return hovered || clicked;
                default: return false;
            }
        }

        private void CacheRenderers()
        {
            renderers.Clear();
            originalMaterials.Clear();
            if (combineMeshes) BuildCombinedMesh();
            Renderer[] found = includeChildren ? GetComponentsInChildren<Renderer>(true) : GetComponents<Renderer>();
            foreach (Renderer renderer in found)
            {
                if (renderer == null || renderer == combinedRenderer || !IsSupportedRenderer(renderer)) continue;
                renderers.Add(renderer);
                originalMaterials[renderer] = renderer.sharedMaterials;
            }
            if (combinedRenderer != null)
            {
                renderers.Add(combinedRenderer);
                originalMaterials[combinedRenderer] = combinedRenderer.sharedMaterials;
            }
        }

        private static bool IsSupportedRenderer(Renderer renderer) => renderer is MeshRenderer || renderer is SkinnedMeshRenderer;

        private void ApplyOutline()
        {
            if (applied || outlineMaterial == null) return;
            UpdateMaterialProperties();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !originalMaterials.TryGetValue(renderer, out Material[] source)) continue;
                Material[] materials = new Material[source.Length + 1];
                for (int i = 0; i < source.Length; i++) materials[i] = source[i];
                materials[materials.Length - 1] = outlineMaterial;
                renderer.sharedMaterials = materials;
            }
            applied = true;
        }

        private void RemoveOutline()
        {
            if (!applied) return;
            foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
            {
                if (entry.Key != null) entry.Key.sharedMaterials = entry.Value;
            }
            applied = false;
        }

        private void CreateOutlineMaterial()
        {
            if (outlineMaterial != null) DestroyImmediate(outlineMaterial);
            Shader shader = Shader.Find(ResolveShaderName());
            if (shader == null) shader = Shader.Find("QuickOutlinePro/BuiltIn/Outline");
            outlineMaterial = new Material(shader) { name = "Quick Outline Pro Runtime", hideFlags = HideFlags.HideAndDontSave };
            UpdateMaterialProperties();
        }

        private string ResolveShaderName()
        {
            PipelineMode selected = pipeline == PipelineMode.Auto ? DetectPipeline() : pipeline;
            if (selected == PipelineMode.URP) return "QuickOutlinePro/URP/Outline";
            if (selected == PipelineMode.HDRP) return "QuickOutlinePro/HDRP/Outline";
            return "QuickOutlinePro/BuiltIn/Outline";
        }

        private static PipelineMode DetectPipeline()
        {
            RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null) return PipelineMode.BuiltIn;
            string type = asset.GetType().Name.ToLowerInvariant();
            if (type.Contains("hd")) return PipelineMode.HDRP;
            if (type.Contains("universal") || type.Contains("urp")) return PipelineMode.URP;
            return PipelineMode.BuiltIn;
        }

        private void UpdateMaterialProperties()
        {
            if (outlineMaterial == null) return;
            outlineMaterial.SetColor(ColorId, outlineColor * Mathf.Max(1f, glowIntensity));
            outlineMaterial.SetFloat(WidthId, outlineWidth);
            outlineMaterial.SetFloat(GlowId, glowIntensity);
        }

        private void EnsureInputCollider()
        {
            if (!requireColliderForInput || GetComponent<Collider>() != null) return;
            if (GetComponentInChildren<Renderer>() != null) gameObject.AddComponent<BoxCollider>();
        }

        private void BuildCombinedMesh()
        {
            MeshFilter[] filters = includeChildren ? GetComponentsInChildren<MeshFilter>(true) : GetComponents<MeshFilter>();
            if (filters.Length == 0) return;
            List<CombineInstance> combine = new List<CombineInstance>(filters.Length);
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh == null) continue;
                combine.Add(new CombineInstance
                {
                    mesh = filters[i].sharedMesh,
                    transform = worldToLocal * filters[i].transform.localToWorldMatrix
                });
            }
            if (combine.Count == 0) return;
            GameObject holder = new GameObject("QuickOutlinePro_CombinedMesh") { hideFlags = HideFlags.HideAndDontSave };
            holder.transform.SetParent(transform, false);
            Mesh mesh = new Mesh { name = "QuickOutlinePro Combined Mesh" };
            mesh.CombineMeshes(combine.ToArray(), true, true, false);
            holder.AddComponent<MeshFilter>().sharedMesh = mesh;
            combinedRenderer = holder.AddComponent<MeshRenderer>();
            combinedRenderer.shadowCastingMode = ShadowCastingMode.Off;
            combinedRenderer.receiveShadows = false;
            combinedRenderer.enabled = true;
        }

        private void Cleanup()
        {
            RemoveOutline();
            if (outlineMaterial != null) DestroyImmediate(outlineMaterial);
            if (combinedRenderer != null) DestroyImmediate(combinedRenderer.gameObject);
        }
    }
}
