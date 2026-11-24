using System;
using System.Linq;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DailyCaravan
{
    // The native dictionary slides in from the right. At large UI scales its first
    // row can cover the kingdom badge on the last card. Keep the native horizontal tween.
    [HarmonyPatch(typeof(UICard), nameof(UICard.DisplayKeywords))]
    internal sealed class CaravanKeywordLayout : MonoBehaviour
    {
        private static readonly AccessTools.FieldRef<UIOverlayMerchant, Action<int, ResourceAmount[]>> Callback =
            AccessTools.FieldRefAccess<UIOverlayMerchant, Action<int, ResourceAmount[]>>("m_Callback");
        private static readonly AccessTools.FieldRef<UICardItem, Image> Kingdom =
            AccessTools.FieldRefAccess<UICardItem, Image>("kingdom");
        private RectTransform panel, canvas, resourceCounter;
        private RectTransform[] obstacles;
        private readonly Vector3[] corners = new Vector3[4];
        private float shift;

        private static void Postfix(UICard __instance, UIEffectKeywordDictionary ___m_KeywordDictionary)
        {
            if (!(__instance is UICardItem) || !___m_KeywordDictionary || !___m_KeywordDictionary.isActiveAndEnabled) return;
            var merchant = __instance.GetComponentInParent<UIOverlayMerchant>();
            if (!merchant || !(Callback(merchant)?.Target is GuestCaravan guest) || !DailyCaravanPlugin.Instance.Owns(guest)) return;
            var layout = ___m_KeywordDictionary.GetComponent<CaravanKeywordLayout>() ??
                ___m_KeywordDictionary.gameObject.AddComponent<CaravanKeywordLayout>();
            layout.panel = (RectTransform)layout.transform;
            layout.canvas = layout.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
            layout.resourceCounter = UIManager.Instance.GetResourceAmount(ResourceType.Gold)?.transform as RectTransform;
            layout.obstacles = merchant.GetComponentsInChildren<UICardItem>()
                .SelectMany(card => new[] { card.GetCardButton().transform as RectTransform, Kingdom(card)?.transform.parent as RectTransform })
                .Where(rect => rect != null).ToArray();
        }

        private Rect Bounds(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            var a = panel.parent.InverseTransformPoint(corners[0]);
            var b = panel.parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        private void LateUpdate()
        {
            if (obstacles == null || !canvas) return;
            const float padding = 4f;
            float required = 0f, available = float.PositiveInfinity;
            var viewport = Bounds(canvas);
            // Some merchant layouts hide the resource counters. Reserve the upper HUD band there too.
            float ceiling = viewport.yMax - viewport.height * .1f;
            if (resourceCounter && resourceCounter.gameObject.activeInHierarchy)
                ceiling = Mathf.Min(ceiling, Bounds(resourceCounter).yMin - padding);
            foreach (RectTransform entry in panel)
            {
                var row = Bounds(entry);
                row.y -= shift;
                available = Mathf.Min(available, ceiling - row.yMax);
                foreach (var obstacle in obstacles)
                {
                    if (!obstacle || !obstacle.gameObject.activeInHierarchy) continue;
                    var card = Bounds(obstacle);
                    if (row.Overlaps(card)) required = Mathf.Max(required, card.yMax + padding - row.yMin);
                }
            }
            SetShift(required <= available ? required : 0f);
        }

        private void SetShift(float value)
        {
            if (Mathf.Approximately(value, shift)) return;
            panel.anchoredPosition += Vector2.up * (value - shift);
            shift = value;
        }

        private void OnDisable()
        {
            SetShift(0f);
            obstacles = null;
        }
    }
}
