using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DailyCaravan
{
    // Keep the native card3 artwork, including its black stepped edge. The
    // native highlighted/selected sprites replace the frame and face colors.
    internal sealed class CaravanCardEdge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private static readonly AccessTools.FieldRef<UICardItem, Image> Frame =
            AccessTools.FieldRefAccess<UICardItem, Image>("card");
        private static readonly AccessTools.FieldRef<UICardItem, Sprite> Rest =
            AccessTools.FieldRefAccess<UICardItem, Sprite>("spriteCardDefault");

        private UICardItem card;
        private Image frame;
        private Button button;
        private CaravanCardRim rim;
        private bool hovered, pressed, navigationFocus;

        internal static void Attach(UICardItem card)
        {
            var frame = Frame(card);
            var sprite = Rest(card);
            var button = card.GetCardButton();
            if (!frame || !sprite || !button || sprite.name != "card3" ||
                sprite.rect.size != new Vector2(75f, 82f) ||
                (frame.type != Image.Type.Simple && frame.type != Image.Type.Sliced)) return;

            var edge = button.gameObject.AddComponent<CaravanCardEdge>();
            edge.card = card;
            edge.frame = frame;
            edge.button = button;
            var layer = new GameObject("Caravan Card Focus Edge", typeof(RectTransform), typeof(CaravanCardRim));
            var rect = (RectTransform)layer.transform;
            var art = frame.rectTransform;
            rect.SetParent(art.parent, false);
            rect.SetSiblingIndex(art.GetSiblingIndex());
            rect.anchorMin = art.anchorMin;
            rect.anchorMax = art.anchorMax;
            rect.pivot = art.pivot;
            rect.anchoredPosition3D = art.anchoredPosition3D;
            rect.sizeDelta = art.sizeDelta;
            edge.rim = layer.GetComponent<CaravanCardRim>();
            edge.rim.Frame = frame;
            edge.rim.raycastTarget = false;
            edge.rim.enabled = false;
            edge.navigationFocus = EventSystem.current?.currentSelectedGameObject == button.gameObject;
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = pressed = false;
        public void OnPointerDown(PointerEventData eventData) => pressed = eventData.button == PointerEventData.InputButton.Left;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnSelect(BaseEventData eventData) => navigationFocus = !(eventData is PointerEventData);
        public void OnDeselect(BaseEventData eventData) => navigationFocus = false;

        private void LateUpdate()
        {
            if (!card || !frame || !rim) return;
            if (frame.sprite != Rest(card)) frame.sprite = Rest(card);
            bool focused = navigationFocus && EventSystem.current?.currentSelectedGameObject == button.gameObject;
            var gamepad = GamepadManager.Instance;
            bool pointer = (hovered || pressed) && (!gamepad || !gamepad.IsGamepadInUse());
            bool show = (pointer || focused) && button.isActiveAndEnabled && button.IsInteractable() &&
                CaravanPatches.CanGrant(card, out _);
            if (show)
            {
                // The reveal animation can flatten the native image when AddItem returns.
                // Use its live transform so the edge follows the card and UI scaling.
                var art = frame.rectTransform;
                rim.rectTransform.localRotation = art.localRotation;
                rim.rectTransform.localScale = art.localScale;
            }
            if (rim.enabled != show) rim.enabled = show;
        }

        private void OnDisable()
        {
            hovered = pressed = navigationFocus = false;
            if (rim) rim.enabled = false;
        }
    }

    internal sealed class CaravanCardRim : MaskableGraphic
    {
        internal Image Frame;
        private static readonly int[] Corner = { 9, 7, 6, 5, 5 };
        private const int FrameHeight = 80;

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (!Frame || !Frame.sprite) return;
            var rect = Frame.GetPixelAdjustedRect();
            var border = Frame.type == Image.Type.Sliced ? Frame.sprite.border : Vector4.zero;
            float pixel = 1f / (Frame.pixelsPerUnit * Frame.pixelsPerUnitMultiplier);
            // Trace only the one-pixel halo, leaving the face, badge and native
            // frame decoration intact. Round the corner outline in pixel steps.
            for (int row = -1; row <= FrameHeight; row++)
            {
                int left = Left(row);
                int outer = Mathf.Min(left - 1, Left(row - 1), Left(row + 1));
                if (left == 38)
                {
                    AddStrip(outer, 76 - outer, row);
                    continue;
                }
                AddStrip(outer, left, row);
                AddStrip(76 - left, 76 - outer, row);
            }

            void AddStrip(int from, int to, int row)
            {
                if (from >= to) return;
                float x0 = Map(from, 75f, rect.xMin, rect.width, border.x, border.z, pixel);
                float x1 = Map(to, 75f, rect.xMin, rect.width, border.x, border.z, pixel);
                float y0 = Map(81 - row, 82f, rect.yMin, rect.height, border.y, border.w, pixel);
                float y1 = Map(82 - row, 82f, rect.yMin, rect.height, border.y, border.w, pixel);
                int index = vertices.currentVertCount;
                vertices.AddVert(new Vector3(x0, y0), Color.white, Vector2.zero);
                vertices.AddVert(new Vector3(x0, y1), Color.white, Vector2.zero);
                vertices.AddVert(new Vector3(x1, y1), Color.white, Vector2.zero);
                vertices.AddVert(new Vector3(x1, y0), Color.white, Vector2.zero);
                vertices.AddTriangle(index, index + 1, index + 2);
                vertices.AddTriangle(index + 2, index + 3, index);
            }
        }

        private static int Left(int row)
        {
            if (row < 0 || row >= FrameHeight) return 38;
            int corner = Mathf.Min(row, FrameHeight - 1 - row);
            return corner < Corner.Length ? Corner[corner] : 4;
        }

        private static float Map(float coordinate, float source, float start, float size, float low, float high, float pixel)
        {
            float borders = low + high;
            if (borders <= 0f) return start + size * coordinate / source;
            float edgePixel = Mathf.Min(pixel, size / borders);
            if (coordinate <= low) return start + coordinate * edgePixel;
            if (coordinate >= source - high) return start + size - (source - coordinate) * edgePixel;
            return start + low * edgePixel + (coordinate - low) / (source - borders) * (size - borders * edgePixel);
        }
    }
}
