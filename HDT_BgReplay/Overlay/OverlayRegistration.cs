using System.Collections.Generic;
using System.Windows;
using Hearthstone_Deck_Tracker.Utility.Extensions;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgReplay.Overlay
{
    internal static class OverlayRegistration
    {
        public static void RegisterClickable(FrameworkElement element)
        {
            OverlayExtensions.SetIsOverlayHitTestVisible(element, true);
            AddElement(Core.OverlayWindow, "_clickableElements", element);
            AddElement(Core.OverlayWindow, "_hoverableElements", element);
        }

        public static void UnregisterClickable(FrameworkElement element)
        {
            RemoveElement(Core.OverlayWindow, "_clickableElements", element);
            RemoveElement(Core.OverlayWindow, "_hoverableElements", element);
        }

        private static void AddElement(object overlayWindow, string fieldName, FrameworkElement element)
        {
            var collection = Utils.GetFieldValue(overlayWindow, fieldName) as ICollection<FrameworkElement>;
            if (collection == null || collection.Contains(element))
                return;
            collection.Add(element);
        }

        private static void RemoveElement(object overlayWindow, string fieldName, FrameworkElement element)
        {
            var collection = Utils.GetFieldValue(overlayWindow, fieldName) as ICollection<FrameworkElement>;
            collection?.Remove(element);
        }
    }
}
