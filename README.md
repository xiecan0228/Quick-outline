# Quick Outline Pro for Unity 6.3 LTS

Quick Outline Pro is a lightweight Fresnel rim-light highlight package for Unity 6.3 LTS. It targets Built-in Render Pipeline, URP, HDRP, Mobile, WebGL, and Playable Ads without compute shaders or post-processing dependencies.

## Features

- Unity 6.3 LTS package metadata (`unity: 6000.3`).
- Single cross-pipeline Fresnel edge-light shader instead of inverted-hull mesh extrusion.
- Mobile, WebGL, and playable-ad friendly transparent surface overlay rendering.
- C# one-click control through `QuickOutlinePro`.
- Hover highlight and click highlight helpers.
- HDR/glowing Fresnel rim color, dynamic rim width, and dynamic glow intensity.
- Multi-material and multi-submesh support through hidden overlay renderers, so original materials are not replaced.
- Optional mesh-combine Fresnel proxy for multi-part models.

## Quick start

1. Copy this repository into a Unity project or install it as a local package.
2. Add `QuickOutlinePro` to any object with a `MeshRenderer` or `SkinnedMeshRenderer`.
3. Choose `Always`, `Hover`, `Click`, or `HoverAndClick` mode.
4. Adjust Fresnel rim color, rim width, and glow intensity.
5. For centralized pointer selection, add `QuickOutlineProInputSelector` to the camera.

## C# control

```csharp
using QOP = QuickOutlinePro.QuickOutlinePro;
using UnityEngine;

public sealed class OutlineExample : MonoBehaviour
{
    [SerializeField] private QOP outline;

    public void Select()
    {
        outline.SetHighlighted(true);
        outline.SetColor(Color.yellow);
        outline.SetWidth(0.55f);
        outline.SetGlow(2.5f);
    }
}
```

## Render pipeline notes

The runtime material uses `QuickOutlinePro/FresnelRim`. The shader draws the original model surface again as a transparent additive Fresnel rim pass, so it behaves like a view-dependent edge glow around the object rather than a pushed-out shell or inserted plane.

The implementation uses vertex normals, view direction, transparent additive blending, and no post-processing pass, which keeps it compatible with constrained builds such as WebGL and playable ads.
