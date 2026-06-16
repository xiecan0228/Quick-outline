using UnityEngine;

namespace QuickOutlinePro
{
    [AddComponentMenu("Rendering/Quick Outline Pro Input Selector")]
    public sealed class QuickOutlineProInputSelector : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask layerMask = ~0;
        [SerializeField] private bool clickToggles = true;
        private QuickOutlinePro currentHover;
        private QuickOutlinePro currentClick;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            UpdateHover();
            if (Input.GetMouseButtonDown(0)) UpdateClick();
        }

        private void UpdateHover()
        {
            QuickOutlinePro hit = RaycastOutline();
            if (hit == currentHover) return;
            if (currentHover != null && currentHover.Mode == QuickOutlinePro.HighlightMode.Hover) currentHover.SetHoverState(false);
            currentHover = hit;
            if (currentHover != null)
            {
                currentHover.Mode = QuickOutlinePro.HighlightMode.Hover;
                currentHover.Visible = true;
                currentHover.SetHoverState(true);
            }
        }

        private void UpdateClick()
        {
            QuickOutlinePro hit = RaycastOutline();
            if (!clickToggles && currentClick != null) currentClick.SetClickState(false);
            if (hit == null) return;
            if (clickToggles && hit == currentClick)
            {
                hit.SetClickState(false);
                currentClick = null;
                return;
            }
            currentClick = hit;
            currentClick.Mode = QuickOutlinePro.HighlightMode.Click;
            currentClick.Visible = true;
            currentClick.SetClickState(true);
        }

        private QuickOutlinePro RaycastOutline()
        {
            if (targetCamera == null) return null;
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out RaycastHit hit, 10000f, layerMask, QueryTriggerInteraction.Ignore)
                ? hit.collider.GetComponentInParent<QuickOutlinePro>()
                : null;
        }
    }
}
