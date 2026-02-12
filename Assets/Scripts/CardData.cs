using System;

[Serializable]
public class CardData
{
    // ------------------------
    // ✅ BASE CARD INFO
    // ------------------------
    public int cardID;           // The unique identifier for the card
    public bool isFused = false;

    // FUSION METADATA
    public int fusedID1;
    public int fusedID2;
    public int baseFPS;
    // Will be overridden by the Fusion Evaluator
    public FusionType fusionType;
    public Suit suit;

    // ------------------------
    // ✅ ENUMS
    // ------------------------
    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum FusionType
    {
        SumFusion,
        Pair,
        Flush,
        Straight,
        FaceFusion,
        FaceSequence,
        FacePair,
        StraightFlush,
        RoyalFlush,
        StraightRoyalFlush
    }

    // ------------------------
    // ✅ UTILITY METHODS
    // ------------------------

    public CardData Clone()
    {
        return new CardData
        {
            cardID = this.cardID,
            isFused = this.isFused,
            fusedID1 = this.fusedID1,
            fusedID2 = this.fusedID2,
            fusionType = this.fusionType,
            suit = this.suit,
            baseFPS = this.baseFPS,
        };
    }
}
