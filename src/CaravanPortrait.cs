using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyCaravan
{
    [HarmonyPatch(typeof(UIOverlayMerchant), nameof(UIOverlayMerchant.Init))]
    internal static class CaravanPortrait
    {
        private const string Header = "HeaderContainer/Header";
        private const string Portrait = "Daily Caravan Portrait";

        // The merchant screen is shared with other visitors and the commission delivery.
        private static void Prefix(UIOverlayMerchant __instance)
        {
            __instance.transform.Find("Panel/ScrollArea/Viewport/Selection")?.GetComponent<CaravanCardSpacing>()?.ResetSpacing();
            var previous = __instance.transform.Find(Header + "/" + Portrait);
            if (previous) previous.gameObject.SetActive(false);
        }

        internal static void Show(UIOverlayMerchant screen)
        {
            var header = screen.transform.Find(Header)?.GetComponent<TextMeshProUGUI>();
            var sprite = DialogueManager.Instance.GetActor(Actors.Caravan)?.avatar;
            if (!header || !sprite) return;
            header.ForceMeshUpdate();
            var bounds = header.textBounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;

            var image = header.transform.Find(Portrait)?.GetComponent<Image>();
            if (!image)
            {
                var obj = new GameObject(Portrait, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                obj.transform.SetParent(header.transform, false);
                obj.GetComponent<LayoutElement>().ignoreLayout = true;
                image = obj.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            image.sprite = sprite;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = header.rectTransform.pivot;
            rect.pivot = new Vector2(1f, .5f);
            float height = bounds.size.y * 1.6f;
            rect.sizeDelta = new Vector2(height * sprite.rect.width / sprite.rect.height, height);
            rect.anchoredPosition = new Vector2(bounds.min.x - 4f, bounds.center.y);
            image.gameObject.SetActive(true);
        }
    }
}
