using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "PlayerDeck", menuName = "CardGame/PlayerDeck")]
public class PlayerDeck : ScriptableObject
{
    [Header("Owned Cards (Persistent)")]
    [SerializeField] private List<CardData> ownedCards = new List<CardData>(); 

    [Header("Available Cards (For Trials)")]
    private List<CardData> availableCards = new List<CardData>(); 

    public void AddCard(CardData data)
    {
        if (data != null) ownedCards.Add(data.Clone());
    }

    public void AddCards(List<CardData> dataList)
    {
        foreach (var data in dataList) {
            if (data != null) ownedCards.Add(data.Clone());
        }
    }

    public void ClearDeck() => ownedCards.Clear();

    public List<CardData> GetAllCards() => new List<CardData>(ownedCards);

    public int Count => ownedCards.Count;

    public CardData GetCardAt(int index) =>
        (index >= 0 && index < ownedCards.Count) ? ownedCards[index] : null;

    /// <summary> Must be called at the START of every trial. Copies owned cards into available cards and shuffles. </summary>
    public void ResetAvailableCards()
    {
        availableCards = new List<CardData>(ownedCards);
        Shuffle(availableCards);
    }

    /// <summary> Draw sequentially from available cards. Removes drawn cards from available pool. </summary>
    public List<CardData> DrawFromAvailable(int count)
    {
        var drawCount = Mathf.Min(count, availableCards.Count);
        var drawn = availableCards.Take(drawCount).ToList();
        availableCards.RemoveRange(0, drawCount);
        return drawn.Select(c => c.Clone()).ToList();
    }

    /// <summary> Draw randomly from available cards. Removes drawn cards from available pool. </summary>
    public List<CardData> GetRandomDraw(int count)
    {
        var drawCount = Mathf.Min(count, availableCards.Count);
        if (drawCount <= 0) return new List<CardData>(); 

        var drawnIndexes = new List<int>(); 
        var drawnCards = new List<CardData>(); 

        // Randomly pick indexes
        for (int i = 0; i < drawCount; i++)
        {
            int rnd = Random.Range(0, availableCards.Count);
            drawnIndexes.Add(rnd);
            drawnCards.Add(availableCards[rnd]);
            availableCards.RemoveAt(rnd);
        }

        // Return cloned drawn cards
        return drawnCards.Select(c => c.Clone()).ToList();
    }

    private void Shuffle(List<CardData> list)
    {
        for (int i = 0; i < list.Count; i++) {
            int rnd = Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }
}
