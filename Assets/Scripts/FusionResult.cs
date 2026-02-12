using System;

[Serializable]
public struct FusionResult
{
    public CardData.FusionType Type;
    public int BaseFPS;
    public CardData.Suit DominantSuit;

    public FusionResult(CardData.FusionType type, int baseFPS, CardData.Suit dominantSuit)
    {
        Type = type;
        BaseFPS = baseFPS;
        DominantSuit = dominantSuit;
    }
}
