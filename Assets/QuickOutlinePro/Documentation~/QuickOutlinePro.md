# Quick Outline Pro Documentation

Add `QuickOutlinePro` to a model, pick a highlight mode, and tune `Fresnel Rim Color`, `Fresnel Rim Width`, and `Glow Intensity`. The component creates hidden overlay renderers that reuse the source mesh with a transparent Fresnel rim material, so original materials stay untouched.

Use `Fresnel Rim Width` to control how far the edge light rolls toward the center of the model. Use `Glow Intensity` to control additive brightness.

Enable `Combine Meshes` when a character or prop is made of many child meshes and should receive a unified Fresnel overlay proxy. Use `Rebuild()` after changing child meshes at runtime.

For hover and click interaction, either use the component's built-in `OnMouseEnter`, `OnMouseExit`, and `OnMouseDown` callbacks with colliders, or add `QuickOutlineProInputSelector` to a camera for raycast-based selection. The selector calls `SetHoverState` and `SetClickState` directly, so hover no longer requires a click.
Add `QuickOutlinePro` to a model, pick a highlight mode, and tune `Outline Color`, `Outline Width`, and `Glow Intensity`. The component appends a runtime outline material after the model's original material list, so multi-material objects keep their existing surfaces.

Enable `Combine Meshes` when a character or prop is made of many child meshes and should receive a unified outline proxy. Use `Rebuild()` after changing child meshes at runtime.

For hover and click interaction, either use the component's built-in `OnMouseEnter`, `OnMouseExit`, and `OnMouseDown` callbacks with colliders, or add `QuickOutlineProInputSelector` to a camera for raycast-based selection.
