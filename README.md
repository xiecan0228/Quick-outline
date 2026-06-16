# Quick Outline Pro for Unity 6.3 LTS

Quick Outline Pro is a lightweight runtime outline/highlight package for Unity 6.3 LTS. It targets Built-in Render Pipeline, URP, HDRP, Mobile, WebGL, and Playable Ads without compute shaders or post-processing dependencies.

## Features

- Unity 6.3 LTS package metadata (`unity: 6000.3`).
- Built-in, URP, and HDRP outline shaders.
- Mobile, WebGL, and playable-ad friendly inverted-hull rendering.
- C# one-click control through `QuickOutlinePro`.
- Hover highlight and click highlight helpers.
- HDR/glowing outline color, dynamic color, dynamic width, and glow intensity.
- Multi-material renderer support by appending one outline material without replacing originals.
- Optional mesh-combine outline proxy for multi-part models.

## Quick start

1. Copy this repository into a Unity project or install it as a local package.
2. Add `QuickOutlinePro` to any object with a `MeshRenderer` or `SkinnedMeshRenderer`.
3. Choose `Always`, `Hover`, `Click`, or `HoverAndClick` mode.
4. Adjust outline color, width, and glow intensity.
5. For centralized pointer selection, add `QuickOutlineProInputSelector` to the camera.

## C# control

```csharp
using QuickOutlinePro;
using UnityEngine;

public sealed class OutlineExample : MonoBehaviour
{
    [SerializeField] private QuickOutlinePro.QuickOutlinePro outline;

    public void Select()
    {
        outline.SetHighlighted(true);
        outline.SetColor(Color.yellow);
        outline.SetWidth(0.04f);
        outline.SetGlow(2.5f);
    }
}
```

## Render pipeline notes

`QuickOutlinePro` detects the active render pipeline automatically and loads one of these shaders:

- `QuickOutlinePro/BuiltIn/Outline`
- `QuickOutlinePro/URP/Outline`
- `QuickOutlinePro/HDRP/Outline`

The implementation uses normal extrusion, front-face culling, transparent blending, and no post-processing pass, which keeps it compatible with constrained builds such as WebGL and playable ads.
