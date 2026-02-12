using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDeck", menuName = "CardGame/Deck")]
public class Deck : ScriptableObject
{
    [Tooltip("All 52 card sprites in suit-order")]
    public Sprite[] cardSprites;

    private List<int> availableCardIDs = new List<int>();

    // Called at start of round
    public void ShuffleDeck()
    {
        availableCardIDs.Clear();
        for (int i = 0; i < cardSprites.Length; i++)
            availableCardIDs.Add(i);

        // Fisher–Yates Shuffle
        for (int i = 0; i < availableCardIDs.Count; i++)
        {
            int rnd = Random.Range(i, availableCardIDs.Count);
            (availableCardIDs[i], availableCardIDs[rnd]) = (availableCardIDs[rnd], availableCardIDs[i]);
        }
    }

    // Draw the next available card
    public int DrawCardID()
    {
        if (availableCardIDs.Count == 0)
        {
            Debug.LogWarning("Deck is empty. No more cards to draw.");
            return -1;
        }

        int nextID = availableCardIDs[0];
        availableCardIDs.RemoveAt(0);
        return nextID;
    }

    public Sprite GetCardSprite(int cardID)
    {
        if (cardID >= 0 && cardID < cardSprites.Length)
            return cardSprites[cardID];
        else
            return null;
    }

    public int CardsLeft => availableCardIDs.Count;

    public void ResetDeck()
    {
        ShuffleDeck(); // or keep as separate if you want
    }
}
