namespace HDT_BgPickAdvisor.Detection
{
    /// <summary>Phase gating without turn numbers — trinkets only after hero pick is finished.</summary>
    internal static class BgPickPhaseRules
    {
        public static bool ShouldShowTrinketOverlay(
            bool isBattlegrounds,
            bool isInMenu,
            bool isHeroPickPhase,
            bool isHeroPickingDone,
            int trinketOfferCount) =>
            isBattlegrounds
            && !isInMenu
            && !isHeroPickPhase
            && isHeroPickingDone
            && trinketOfferCount >= 2;
    }
}
