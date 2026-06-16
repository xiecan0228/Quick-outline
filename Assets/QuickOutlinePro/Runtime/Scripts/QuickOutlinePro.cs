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

        [Header("Behaviour")]
        [SerializeField] private HighlightMode mode = HighlightMode.Always;
        [SerializeField] private bool includeChildren = true;
        [SerializeField] private bool combineMeshes = false;
        [SerializeField] private bool requireColliderForInput = true;

        [Header("Fresnel Rim Light")]
        [ColorUsage(true, true)] [SerializeField] private Color outlineColor = new Color(0.0f, 0.65f, 1.0f, 1.0f);
        [Tooltip("Controls Fresnel rim thickness. Larger values make the edge light wider.")]
        [SerializeField, Range(0.0f, 1.0f)] private float outlineWidth = 0.35f;
        [SerializeField, Range(0.0f, 8.0f)] private float glowIntensity = 1.5f;
        [SerializeField] private bool visible = true;

        private static readonly int ColorId = Shader.PropertyToID("_RimColor");
        private static readonly int WidthId = Shader.PropertyToID("_RimWidth");
        private static readonly int GlowId = Shader.PropertyToID("_GlowIntensity");

        private readonly List<Renderer> sourceRenderers = new List<Renderer>();
        private readonly List<Renderer> overlayRenderers = new List<Renderer>();
        private Material fresnelMaterial;
        private GameObject overlayRoot;
        private bool hovered;
        private bool clicked;

        public HighlightMode Mode { get => mode; set { mode = value; RefreshVisibility(); } }
        public Color OutlineColor { get => outlineColor; set { outlineColor = value; UpdateMaterialProperties(); } }
        public float OutlineWidth { get => outlineWidth; set { outlineWidth = Mathf.Clamp01(value); UpdateMaterialProperties(); } }
        public float GlowIntensity { get => glowIntensity; set { glowIntensity = Mathf.Max(0f, value); UpdateMaterialProperties(); } }
        public bool Visible { get => visible; set { visible = value; RefreshVisibility(); } }

        private void Awake()
        {
            Rebuild();
            EnsureInputCollider();
        }

        private void OnEnable() => RefreshVisibility();
        private void OnDisable() => SetOverlayEnabled(false);
        private void OnDestroy() => Cleanup();
        private void OnMouseEnter() { SetHoverState(true); }
        private void OnMouseExit() { SetHoverState(false); }
        private void OnMouseDown() { SetClickState(!clicked); }

        public void SetHighlighted(bool highlighted)
        {
            visible = highlighted;
            mode = highlighted ? HighlightMode.Always : HighlightMode.Off;
            RefreshVisibility();
        }

        public void SetHoverState(bool isHovered)
        {
            hovered = isHovered;
            RefreshVisibility();
        }

        public void SetClickState(bool isClicked)
        {
            clicked = isClicked;
            RefreshVisibility();
        }

        public void SetColor(Color color) => OutlineColor = color;
        public void SetWidth(float width) => OutlineWidth = width;
        public void SetGlow(float intensity) => GlowIntensity = intensity;

        public void Rebuild()
        {
            CleanupOverlayObjects();
            CreateFresnelMaterial();
            CacheSourceRenderers();
            if (combineMeshes) BuildCombinedOverlay(); else BuildRendererOverlays();
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (!isActiveAndEnabled) return;
            SetOverlayEnabled(visible && ShouldShowForMode());
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

        private void CacheSourceRenderers()
        {
            sourceRenderers.Clear();
            Renderer[] found = includeChildren ? GetComponentsInChildren<Renderer>(true) : GetComponents<Renderer>();
            foreach (Renderer renderer in found)
            {
                if (renderer == null || renderer.transform.IsChildOf(overlayRoot.transform) || !IsSupportedRenderer(renderer)) continue;
                sourceRenderers.Add(renderer);
            }
        }

        private static bool IsSupportedRenderer(Renderer renderer) => renderer is MeshRenderer || renderer is SkinnedMeshRenderer;

        private void BuildRendererOverlays()
        {
            foreach (Renderer source in sourceRenderers)
            {
                if (source is MeshRenderer meshRenderer) CreateMeshOverlay(meshRenderer);
                else if (source is SkinnedMeshRenderer skinnedRenderer) CreateSkinnedOverlay(skinnedRenderer);
            }
        }

        private void CreateMeshOverlay(MeshRenderer source)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null) return;
            GameObject overlay = CreateOverlayObject(source.transform, source.name + "_FresnelRim");
            MeshFilter filter = overlay.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;
            MeshRenderer renderer = overlay.AddComponent<MeshRenderer>();
            CopyRendererSettings(source, renderer);
            renderer.sharedMaterials = CreateOverlayMaterials(sourceFilter.sharedMesh.subMeshCount);
            overlayRenderers.Add(renderer);
        }

        private void CreateSkinnedOverlay(SkinnedMeshRenderer source)
        {
            if (source.sharedMesh == null) return;
            GameObject overlay = CreateOverlayObject(source.transform, source.name + "_FresnelRim");
            SkinnedMeshRenderer renderer = overlay.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = source.sharedMesh;
            renderer.rootBone = source.rootBone;
            renderer.bones = source.bones;
            renderer.localBounds = source.localBounds;
            renderer.updateWhenOffscreen = source.updateWhenOffscreen;
            renderer.quality = source.quality;
            CopyRendererSettings(source, renderer);
            renderer.sharedMaterials = CreateOverlayMaterials(source.sharedMesh.subMeshCount);
            overlayRenderers.Add(renderer);
        }

        private GameObject CreateOverlayObject(Transform source, string objectName)
        {
            GameObject overlay = new GameObject(objectName) { hideFlags = HideFlags.DontSave };
            overlay.transform.SetParent(source, false);
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one;
            overlay.layer = source.gameObject.layer;
            return overlay;
        }

        private Material[] CreateOverlayMaterials(int subMeshCount)
        {
            int count = Mathf.Max(1, subMeshCount);
            Material[] materials = new Material[count];
            for (int i = 0; i < count; i++) materials[i] = fresnelMaterial;
            return materials;
        }

        private static void CopyRendererSettings(Renderer source, Renderer target)
        {
            target.shadowCastingMode = ShadowCastingMode.Off;
            target.receiveShadows = false;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
            target.probeAnchor = source.probeAnchor;
            target.enabled = source.enabled;
        }

        private void SetOverlayEnabled(bool enabled)
        {
            foreach (Renderer renderer in overlayRenderers)
            {
                if (renderer != null) renderer.enabled = enabled;
            }
        }

        private void CreateFresnelMaterial()
        {
            if (fresnelMaterial != null) DestroyImmediate(fresnelMaterial);
            Shader shader = Shader.Find("QuickOutlinePro/FresnelRim");
            fresnelMaterial = new Material(shader) { name = "Quick Outline Pro Fresnel Rim", hideFlags = HideFlags.HideAndDontSave };
            UpdateMaterialProperties();
        }

        private void UpdateMaterialProperties()
        {
            if (fresnelMaterial == null) return;
            fresnelMaterial.SetColor(ColorId, outlineColor);
            fresnelMaterial.SetFloat(WidthId, outlineWidth);
            fresnelMaterial.SetFloat(GlowId, glowIntensity);
        }

        private void EnsureInputCollider()
        {
            if (!requireColliderForInput || GetComponent<Collider>() != null) return;
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Renderer renderer in sourceRenderers)
            {
                if (renderer == null) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!hasBounds) return;
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.center = transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private void BuildCombinedOverlay()
        {
            MeshFilter[] filters = includeChildren ? GetComponentsInChildren<MeshFilter>(true) : GetComponents<MeshFilter>();
            List<CombineInstance> combine = new List<CombineInstance>(filters.Length);
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh == null || filters[i].transform.IsChildOf(overlayRoot.transform)) continue;
                combine.Add(new CombineInstance { mesh = filters[i].sharedMesh, transform = worldToLocal * filters[i].transform.localToWorldMatrix });
            }
            if (combine.Count == 0) return;
            GameObject overlay = CreateOverlayObject(transform, "QuickOutlinePro_CombinedFresnelRim");
            Mesh mesh = new Mesh { name = "QuickOutlinePro Combined Fresnel Mesh" };
            mesh.CombineMeshes(combine.ToArray(), true, true, false);
            overlay.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = overlay.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterials = CreateOverlayMaterials(mesh.subMeshCount);
            overlayRenderers.Add(renderer);
        }

        private void CleanupOverlayObjects()
        {
            overlayRenderers.Clear();
            if (overlayRoot != null) DestroyImmediate(overlayRoot);
            overlayRoot = new GameObject("QuickOutlinePro_FresnelOverlays") { hideFlags = HideFlags.DontSave };
            overlayRoot.transform.SetParent(transform, false);
        }

        private void Cleanup()
        {
            CleanupOverlayObjects();
            if (overlayRoot != null) DestroyImmediate(overlayRoot);
            if (fresnelMaterial != null) DestroyImmediate(fresnelMaterial);
        }
    }
}
