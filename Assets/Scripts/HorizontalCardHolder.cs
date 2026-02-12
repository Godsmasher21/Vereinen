using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HorizontalCardHolder : MonoBehaviour
{
    public List<Transform> slots;
    public bool allowHoverScale = true;
    private List<Card> cards = new List<Card>();
    private Card selectedCard;
    [HideInInspector] public Card currentlyDragging;

    public void AddCard(Card card)
    {
        cards.Add(card);

        card.BeginDragEvent.AddListener(OnBeginDrag);
        card.EndDragEvent.AddListener(OnEndDrag);
        card.PointerEnterEvent.AddListener(OnPointerEnter);
        card.PointerExitEvent.AddListener(OnPointerExit);

        ReorderCardVisuals();
    }

    private void OnBeginDrag(Card card)
    {
        selectedCard = card;
    }

    private void OnEndDrag(Card card)
    {
        selectedCard = null;
    }

    private void OnPointerEnter(Card card) { }
    private void OnPointerExit(Card card) { }

    private void Update()
    {

        if (selectedCard == null) return;

        int fromIndex = GetSlotIndexOfCard(selectedCard);
        if (fromIndex == -1) return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == selectedCard) continue;

            float selectedX = selectedCard.transform.position.x;
            float otherX = cards[i].transform.position.x;

            if (selectedX < otherX && fromIndex > i)
            {
                Swap(fromIndex, i);
                break;
            }
            else if (selectedX > otherX && fromIndex < i)
            {
                Swap(fromIndex, i);
                break;
            }
        }
    }

    private void Swap(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0 || fromIndex >= cards.Count || toIndex >= cards.Count)
            return;

        Card draggedCard = cards[fromIndex];
        Card otherCard = cards[toIndex];

        Transform draggedCardSlot = draggedCard.originalParent;
        Transform otherCardSlot = otherCard.transform.parent;

        otherCard.transform.SetParent(draggedCardSlot);
        otherCard.transform.localPosition = Vector3.zero;

        draggedCard.originalParent = otherCardSlot;

        cards[fromIndex] = otherCard;
        cards[toIndex] = draggedCard;

        draggedCard.transform.SetAsLastSibling();

        ReorderCardVisuals();

        Debug.Log($"Swapped {draggedCard.name} ↔ {otherCard.name}");
    }

    private int GetSlotIndexOfCard(Card card)
    {
        for (int i = 0; i < cards.Count; i++)
            if (cards[i] == card)
                return i;
        return -1;
    }

    public void UpdateLayout()
    {
        for (int i = 0; i < cards.Count; i++)
            cards[i].transform.SetSiblingIndex(i);
    }

    public void ClearCards()
    {
        cards.Clear();
    }

    public void ReorderCardVisuals()
    {
        int index = 0;

        foreach (var card in cards)
        {
            if (card == null || card.cardVisual == null) continue;
            if (card == currentlyDragging) continue;

            if (card.secondaryVisual != null)
                card.secondaryVisual.transform.SetSiblingIndex(index++);

            card.cardVisual.transform.SetSiblingIndex(index++);
        }

        if (currentlyDragging?.cardVisual != null)
            currentlyDragging.cardVisual.transform.SetAsLastSibling();
    }

    public void TryFuseSelectedCards()
    {
        var selectedCards = cards.Where(c => c.IsSelected()).ToList();
        Debug.Log($"[Fusion] Selected card count: {selectedCards.Count}");

        if (selectedCards.Count != 2)
        {
            Debug.LogWarning("Fusion requires exactly 2 selected cards.");
            return;
        }

        var cardA = selectedCards[0];
        var cardB = selectedCards[1];

        Debug.Log($"Trying to fuse: {cardA.name} ({cardA.data.cardID}) + {cardB.name} ({cardB.data.cardID})");

        // 🧠 Use FusionEvaluator to determine fusion outcome
        var fusionResult = FusionEvaluator.Evaluate(cardA.data, cardB.data);
        var fusionType = fusionResult.Type;
        var fusionFPS = fusionResult.BaseFPS;
        var dominantSuit = fusionResult.DominantSuit;
        cardA.fusionResult = fusionResult;
        Debug.Log($"[Fusion Result] Type: {fusionType}, FPS: {fusionFPS}, Dominant Suit: {dominantSuit}");

        // ✅ Set fusion data on Card A
        cardA.SetFusionData(cardA.data.cardID, cardB.data.cardID, fusionResult.Type, fusionResult.DominantSuit);
        cardA.cardVisual.SetFollowOffset(Vector3.zero);

        // ✅ Update visual representation (optional logic — keeping the stronger card visually)
        int fusionResultID = Mathf.Max(cardA.data.cardID, cardB.data.cardID);
        var fusedCardData = new CardData
        {
            cardID = cardA.data.cardID,
            isFused = true,
            fusedID1 = cardA.data.cardID,
            fusedID2 = cardB.data.cardID,
            suit = dominantSuit,
            baseFPS = fusionFPS,
            fusionType = fusionType
        };

        cardA.SetCardData(fusedCardData);

        // ✅ Reassign B’s visual to A as secondary
        if (cardB.cardVisual != null)
        {
            cardB.cardVisual.transform.SetParent(cardA.cardVisual.transform.parent);
            cardB.cardVisual.ReassignParent(cardA);
            cardA.secondaryVisual = cardB.cardVisual;

            cardB.cardVisual.SetFollowOffset(new Vector3(-0.5f, 0f, 0f));
            cardB.cardVisual.transform.SetSiblingIndex(
                Mathf.Max(0, cardA.cardVisual.transform.GetSiblingIndex() - 1)
            );
            cardB.cardVisual.isSecondaryVisual = true;
        }

        // ✅ Remove Card B (now merged into A)
        cards.Remove(cardB);
        Destroy(cardB.gameObject);

        foreach (var card in cards)
        {
            card.Deselect();
        }
        
        Debug.Log($"[Deck] Fused card added: {fusionType} | FPS: {fusionFPS} | ID: {fusionResultID}");

        ReorderCardVisuals();
    }

}
