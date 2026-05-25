using System;

namespace HDT_BgReplay
{
    internal static class CardNameResolver
    {
        public static string GetName(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return "(unknown)";

            try
            {
                var card = HearthDb.Cards.All.TryGetValue(cardId, out var c) ? c : null;
                if (card != null && !string.IsNullOrWhiteSpace(card.Name))
                    return card.Name;
            }
            catch
            {
                // HearthDb not available
            }

            return cardId;
        }
    }
}
