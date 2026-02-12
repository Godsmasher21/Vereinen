using UnityEngine;

public static class FusionEvaluator
{
    public static FusionResult Evaluate(CardData a, CardData b)
    {
        int rankA = a.cardID % 13;
        int suitA = a.cardID / 13;

        int rankB = b.cardID % 13;
        int suitB = b.cardID / 13;

        // ⚔️ BASE FLAGS
        bool isFlush = suitA == suitB;

        bool isAceLowSequence = (rankA == 12 && rankB == 0) || (rankA == 0 && rankB == 12);
        bool isSequence = Mathf.Abs(rankA - rankB) == 1 || isAceLowSequence;

        bool isFaceCard = IsFaceRank(rankA) && IsFaceRank(rankB);
        bool isPair = rankA == rankB;

        // ⚔️ BASE RANK-DERIVED FPS
        int baseRankFPS = GetRankDerivedFPS(rankA, rankB);

        // ⚔️ FUSION HIERARCHY
        if (isSequence && isFlush && isFaceCard) return NewResult(CardData.FusionType.StraightRoyalFlush, baseRankFPS + 8, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isFlush && isFaceCard) return NewResult(CardData.FusionType.RoyalFlush, baseRankFPS + 7, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isSequence && isFlush) return NewResult(CardData.FusionType.StraightFlush, baseRankFPS + 6, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isPair && isFaceCard) return NewResult(CardData.FusionType.FacePair, baseRankFPS + 5, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isSequence && isFaceCard) return NewResult(CardData.FusionType.FaceSequence, baseRankFPS + 4, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isFaceCard) return NewResult(CardData.FusionType.FaceFusion, baseRankFPS + 3, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isSequence) return NewResult(CardData.FusionType.Straight, baseRankFPS + 3, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isFlush) return NewResult(CardData.FusionType.Flush, baseRankFPS + 2, GetDominantSuit(suitA, suitB, rankA, rankB));
        if (isPair) return NewResult(CardData.FusionType.Pair, baseRankFPS + 2, GetDominantSuit(suitA, suitB, rankA, rankB));

        // ⚔️ FALLBACK
        return NewResult(CardData.FusionType.SumFusion, baseRankFPS, GetDominantSuit(suitA, suitB, rankA, rankB));
    }

    // =========================
    // ⚔️ HELPER METHODS
    // =========================
    private static bool IsFaceRank(int rank) => rank >= 9 && rank <= 11;

    private static int GetRankFPS(int rank)
    {
        // Rank mapping:
        // 0 -> 2
        // 1 -> 3
        // …
        // 8 -> 10
        // 9 -> J = 10
        // 10 -> Q = 10
        // 11 -> K = 10
        // 12 -> A = 11
        if (rank == 12) return 11;        // Ace
        if (rank >= 9 && rank <= 11) return 10; // J, Q, K
        return rank + 2;                 // Number cards
    }

    private static int GetRankDerivedFPS(int rankA, int rankB)
    {
        int fpsA = GetRankFPS(rankA);
        int fpsB = GetRankFPS(rankB);
        return (fpsA + fpsB) / 2;
    }

    private static CardData.Suit GetDominantSuit(int suitA, int suitB, int rankA, int rankB)
    {
        if (suitA == suitB) return (CardData.Suit)suitA;

        // Dominant suit = suit of the higher ranked card
        return (rankA > rankB) ? (CardData.Suit)suitA : (CardData.Suit)suitB;
    }

    private static FusionResult NewResult(CardData.FusionType fusionType, int baseFPS, CardData.Suit dominantSuit)
    {
        return new FusionResult(fusionType, baseFPS, dominantSuit);
    }
}
